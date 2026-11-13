using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerLocations
    {
        internal static string CleanLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            string cleaned = RichTextTags.Replace(value, "").Trim();
            return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
        }

        internal static Sprite ContainerIcon()
        {
            if (!cachedContainerIcon)
                cachedContainerIcon = FoundationApi.Decorations.ContainerIcon;
            return cachedContainerIcon;
        }

        static readonly Regex RichTextTags = new Regex(@"</?[a-zA-Z][^>]*>", RegexOptions.Compiled);
        static Sprite cachedContainerIcon;
        readonly Dictionary<WorkspaceObject, string> containerIds = new Dictionary<WorkspaceObject, string>();
        int nextId;
        static Sprite DefaultIcon(WorkspaceObject container)
        {
            if (container?.World)
            {
                // Native frames don't expose an icon; use their own background sprite.
                var image = FoundationApi.Decorations.Background(container.World);
                if (image && image.sprite)
                    return image.sprite;
            }

            return FoundationApi.GameInfo.Icon("Icons2_45");
        }

        internal static IEnumerable<GameWorkspace> GetAllFlows(GameKingdom player)
        {
            if (player?.Workspaces == null)
                yield break;
            var visited = new HashSet<GameWorkspace>();
            foreach (var flow in player.Workspaces)
            {
                if (flow == null || !visited.Add(flow))
                    continue;
                yield return flow;
                foreach (var sub in GetSubFlows(flow, visited))
                    yield return sub;
            }
        }

        private static IEnumerable<GameWorkspace> GetSubFlows(GameWorkspace flow, HashSet<GameWorkspace> visited)
        {
            if (flow?.Buildings == null)
                yield break;
            for (int i = 0; i < flow.Buildings.Count; i++)
            {
                var el = flow.Buildings[i];
                if (el == null)
                    continue;
                var inner = FoundationApi.WorkspaceAccess.ExtensionWorkspace(el, "InnerFlow");
                if (inner != null && visited.Add(inner))
                {
                    yield return inner;
                    foreach (var sub in GetSubFlows(inner, visited))
                        yield return sub;
                }
            }
        }

        internal static bool Overlap(WorkspaceObject container, Vector2Int position, Vector2Int size)
        {
            return container.Position.x < (long)position.x + size.x && (long)container.Position.x + container.Size.x > position.x && container.Position.y > (long)position.y - size.y && (long)container.Position.y - container.Size.y < position.y;
        }

        internal void Reset()
        {
            containerIds.Clear();
            nextId = 0;
        }

        internal IReadOnlyList<LedgerLocationEntry> Resolve(IReadOnlyList<LedgerBookTotals> totals)
        {
            var player = FoundationApi.Kingdom;
            if (player == null)
                return Array.Empty<LedgerLocationEntry>();
            var allFlows = GetAllFlows(player).ToList();
            var live = new Dictionary<string, LedgerTarget>();
            foreach (var flow in allFlows)
                foreach (var element in flow.Buildings)
                    live[element.Guid] = new LedgerTarget(flow, element);
            // Pre-scan all live containers in a fixed spatial order across workspaces:
            // flow index -> position Y descending -> position X ascending -> decoration index.
            var containerList = new List<(GameWorkspace Flow, WorkspaceObject Deco, string BaseName)>();
            for (int f = 0; f < allFlows.Count; f++)
            {
                var flow = allFlows[f];
                if (flow?.Decorations == null)
                    continue;
                for (int d = 0; d < flow.Decorations.Count; d++)
                {
                    var deco = flow.Decorations[d];
                    if (deco != null && deco.IsContainer)
                    {
                        var labels = flow.Decorations.Where(l => l.Label != null && CleanLabel(l.Label) != null && Overlap(deco, l.Position, Vector2Int.RoundToInt(l.Size)));
                        var label = labels.OrderBy(l => (l.Position - deco.Position).sqrMagnitude).FirstOrDefault();
                        string baseName = (label != null ? CleanLabel(label.Label) : null) ?? "Container [" + deco.Position.x + ", " + deco.Position.y + "]";
                        containerList.Add((flow, deco, baseName));
                    }
                }
            }

            var sortedContainers = containerList.OrderBy(c => allFlows.IndexOf(c.Flow)).ThenByDescending(c => c.Deco.Position.y).ThenBy(c => c.Deco.Position.x).ThenBy(c => c.Flow.Decorations.ToList().IndexOf(c.Deco)).ToList();
            var containerNames = new Dictionary<WorkspaceObject, string>();
            foreach (var group in sortedContainers.GroupBy(c => c.BaseName, StringComparer.Ordinal))
            {
                var items = group.ToList();
                if (items.Count > 1)
                {
                    for (int i = 0; i < items.Count; i++)
                        containerNames[items[i].Deco] = items[i].BaseName + "_" + (i + 1);
                }
                else
                {
                    containerNames[items[0].Deco] = items[0].BaseName;
                }
            }

            foreach (var item in sortedContainers)
            {
                if (!containerIds.TryGetValue(item.Deco, out _))
                    containerIds[item.Deco] = "container/" + ++nextId;
            }

            var result = new List<LedgerLocationEntry>();
            foreach (var item in totals)
            {
                var entry = new LedgerLocationEntry
                {
                    Totals = item,
                    BuildingName = item.Source.Name
                };
                if (!live.TryGetValue(item.Source.Id, out var target))
                {
                    if (item.Source.TypeId == "War")
                    {
                        entry.GroupKey = "war";
                        entry.GroupName = "War Progress";
                        var war = FoundationApi.GameInfo.FindWar(item.Source.Id.StartsWith("war_") ? item.Source.Id.Substring(4) : item.Source.Id);
                        entry.GroupIcon = entry.BuildingIcon = war?.Icon ?? DefaultIcon(null);
                        result.Add(entry);
                        continue;
                    }

                    entry.GroupKey = "removed";
                    entry.GroupName = "Removed buildings";
                    entry.GroupIcon = DefaultIcon(null);
                    result.Add(entry);
                    continue;
                }

                var flow = target.Flow;
                var building = target.Building;
                entry.BuildingTarget = target;
                entry.BuildingName = FoundationApi.GameInfo.Translate(building.Label) + " [" + building.Position.x + ", " + building.Position.y + "]";
                entry.BuildingIcon = building.Icon;
                // A machine overlapping nested/overlapping frames belongs once to the
                // smallest frame. This matches native overlap-based container grouping
                // while avoiding double-counted net totals in overlapping containers.
                var buildingSize = building.World ? Vector2Int.RoundToInt(FoundationApi.WorkspaceVisuals.Size(building.World)) : new Vector2Int(Math.Max(128, Mathf.RoundToInt(building.Size.x)), Math.Max(128, Mathf.RoundToInt(building.Size.y)));
                var container = flow.Decorations.Where(d => d.IsContainer && Overlap(d, building.Position, buildingSize)).OrderBy(d => (long)d.Size.x * d.Size.y).FirstOrDefault();
                if (container == null)
                {
                    int flowIdx = allFlows.IndexOf(flow);
                    entry.GroupKey = "uncontained/" + (flowIdx >= 0 ? flowIdx.ToString() : flow.GetHashCode().ToString());
                    entry.GroupName = (CleanLabel(flow.Label) ?? "Workspace") + " / Uncontained";
                    entry.GroupIcon = DefaultIcon(null);
                }
                else
                {
                    entry.GroupKey = containerIds[container];
                    entry.GroupName = containerNames.TryGetValue(container, out var name) ? name : ("Container [" + container.Position.x + ", " + container.Position.y + "]");
                    var sticker = flow.Decorations.Where(d => d.HasSticker && Overlap(container, d.Position, Vector2Int.RoundToInt(d.Size))).OrderBy(d => (d.Position - container.Position).sqrMagnitude).FirstOrDefault();
                    entry.GroupIcon = sticker?.StickerSprite ?? building.Icon ?? DefaultIcon(container);
                    entry.GroupTarget = new LedgerTarget(flow, container: container);
                }

                result.Add(entry);
            }

            // Retain keys for existing frames, but don't retain removed native graphs.
            foreach (var stale in containerIds.Keys.Where(d => !allFlows.Any(f => f.Decorations.Contains(d))).ToArray())
                containerIds.Remove(stale);
            return result;
        }
    }
}
