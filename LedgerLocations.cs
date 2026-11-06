using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Assets.Source.Adventure;
using Assets.Source.Flow;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerTarget
    {
        internal readonly FlowDiagram Flow;
        internal readonly FlowElement Building;
        internal readonly FlowDeco Container;
        internal LedgerTarget(FlowDiagram flow, FlowElement building = null, FlowDeco container = null)
        { Flow = flow; Building = building; Container = container; }
        internal bool Exists => GamePlayer.Current != null && GamePlayer.Current.Flows.Contains(Flow)
            && (Building != null ? Flow.Elements.Contains(Building) : Container != null && Flow.Decorations.Contains(Container));
        internal RectTransform View => Building != null ? (Building.World ? (RectTransform)Building.World.transform : null)
            : Container?.World ? (RectTransform)Container.World.transform : null;
        internal static bool Same(LedgerTarget a, LedgerTarget b) => ReferenceEquals(a, b) || a != null && b != null
            && ReferenceEquals(a.Flow, b.Flow) && ReferenceEquals(a.Building, b.Building) && ReferenceEquals(a.Container, b.Container);
    }

    // Resolve the current graph every refresh. Rewiring, moving, relabeling and removing
    // a container changes grouping without changing the immutable accounting amounts.
    internal sealed class LedgerLocations
    {
        static readonly Regex RichTextTags = new Regex(@"</?[a-zA-Z][^>]*>", RegexOptions.Compiled);
        static Sprite cachedContainerIcon;
        internal static Sprite ContainerIcon()
        {
            if (cachedContainerIcon) return cachedContainerIcon;
            if (Assets.Behaviour.UI.GameUI.Instance)
            {
                var decoMenu = AccessTools.Field(typeof(Assets.Behaviour.UI.GameUI), "_decoMenu").GetValue(Assets.Behaviour.UI.GameUI.Instance) as Component;
                if (decoMenu)
                {
                    var isLabel = AccessTools.Field(typeof(Assets.Behaviour.UI.Deco.DecoBuyMenuRow), "_isLabel");
                    var isSticker = AccessTools.Field(typeof(Assets.Behaviour.UI.Deco.DecoBuyMenuRow), "_isSticker");
                    var costField = AccessTools.Field(typeof(Assets.Behaviour.UI.Deco.DecoBuyMenuRow), "_cost");
                    foreach (var row in decoMenu.GetComponentsInChildren<Assets.Behaviour.UI.Deco.DecoBuyMenuRow>(true))
                    {
                        if ((bool)isLabel.GetValue(row) || (bool)isSticker.GetValue(row)) continue;
                        var cost = costField.GetValue(row) as Component;
                        var icon = row.GetComponentsInChildren<Image>(true)
                            .FirstOrDefault(i => i.gameObject != row.gameObject && i.sprite != null && i.type != Image.Type.Sliced && (cost == null || !i.transform.IsChildOf(cost.transform)));
                        if (icon != null) return cachedContainerIcon = icon.sprite;
                    }
                }
            }
            return SpriteLibrary.Get("Icons_31");
        }
        readonly Dictionary<FlowDeco, string> containerIds = new Dictionary<FlowDeco, string>();
        int nextId;
        internal void Reset() { containerIds.Clear(); nextId = 0; }
        internal static string CleanLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string cleaned = RichTextTags.Replace(value, "").Trim();
            return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
        }
        internal sealed class Entry
        {
            internal LedgerBook.Totals Totals;
            internal string GroupKey, GroupName, BuildingName;
            internal Sprite GroupIcon, BuildingIcon;
            internal LedgerTarget GroupTarget, BuildingTarget;
        }
        internal IReadOnlyList<Entry> Resolve(IReadOnlyList<LedgerBook.Totals> totals)
        {
            var player = GamePlayer.Current;
            if (player == null) return Array.Empty<Entry>();
            var live = new Dictionary<string, LedgerTarget>();
            foreach (var flow in player.Flows)
                foreach (var element in flow.Elements) live[element.Guid] = new LedgerTarget(flow, element);

            // Pre-scan all live containers in a fixed spatial order across workspaces:
            // flow index -> position Y descending -> position X ascending -> decoration index.
            var containerList = new List<(FlowDiagram Flow, FlowDeco Deco, string BaseName)>();
            for (int f = 0; f < player.Flows.Count; f++)
            {
                var flow = player.Flows[f];
                if (flow?.Decorations == null) continue;
                for (int d = 0; d < flow.Decorations.Count; d++)
                {
                    var deco = flow.Decorations[d];
                    if (deco != null && deco.IsContainer)
                    {
                        var labels = flow.Decorations.Where(l => l.Label != null && CleanLabel(l.Label) != null && Overlap(deco, l.Position, l.Size));
                        var label = labels.OrderBy(l => (l.Position - deco.Position).sqrMagnitude).FirstOrDefault();
                        string baseName = (label != null ? CleanLabel(label.Label) : null) ?? "Container [" + deco.Position.x + ", " + deco.Position.y + "]";
                        containerList.Add((flow, deco, baseName));
                    }
                }
            }

            var sortedContainers = containerList
                .OrderBy(c => player.Flows.IndexOf(c.Flow))
                .ThenByDescending(c => c.Deco.Position.y)
                .ThenBy(c => c.Deco.Position.x)
                .ThenBy(c => c.Flow.Decorations.IndexOf(c.Deco))
                .ToList();

            var containerNames = new Dictionary<FlowDeco, string>();
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

            var result = new List<Entry>();
            foreach (var item in totals)
            {
                var entry = new Entry { Totals = item, BuildingName = item.Source.Name };
                if (!live.TryGetValue(item.Source.Id, out var target))
                {
                    if (item.Source.TypeId == "War")
                    {
                        entry.GroupKey = "war";
                        entry.GroupName = "War Progress";
                        var war = WarType.Get(item.Source.Id.StartsWith("war_") ? item.Source.Id.Substring(4) : item.Source.Id);
                        entry.GroupIcon = entry.BuildingIcon = war?.Icon ?? DefaultIcon(null);
                        result.Add(entry);
                        continue;
                    }
                    entry.GroupKey = "removed"; entry.GroupName = "Removed buildings";
                    entry.GroupIcon = DefaultIcon(null); result.Add(entry); continue;
                }
                var flow = target.Flow; var building = target.Building;
                entry.BuildingTarget = target;
                entry.BuildingName = Translation.Translate(building.Label) + " [" + building.Position.x + ", " + building.Position.y + "]";
                entry.BuildingIcon = building.Icon;
                // A machine overlapping nested/overlapping frames belongs once to the
                // smallest frame. This matches native overlap-based container grouping
                // while avoiding double-counted net totals in overlapping containers.
                var buildingSize = building.World ? Vector2Int.RoundToInt(building.World.Size) : new Vector2Int(Math.Max(128, building.Size.x), Math.Max(128, building.Size.y));
                var container = flow.Decorations.Where(d => d.IsContainer && Overlap(d, building.Position, buildingSize))
                    .OrderBy(d => (long)d.Size.x * d.Size.y).FirstOrDefault();
                if (container == null)
                {
                    entry.GroupKey = "uncontained/" + player.Flows.IndexOf(flow);
                    entry.GroupName = (CleanLabel(flow.Label) ?? "Workspace") + " / Uncontained";
                    entry.GroupIcon = DefaultIcon(null);
                }
                else
                {
                    entry.GroupKey = containerIds[container];
                    entry.GroupName = containerNames.TryGetValue(container, out var name) ? name : ("Container [" + container.Position.x + ", " + container.Position.y + "]");
                    var sticker = flow.Decorations.Where(d => d.Sticker != null && Overlap(container, d.Position, d.Size))
                        .OrderBy(d => (d.Position - container.Position).sqrMagnitude).FirstOrDefault();
                    entry.GroupIcon = sticker?.Sticker.GetSprite() ?? building.Icon ?? DefaultIcon(container);
                    entry.GroupTarget = new LedgerTarget(flow, container: container);
                }
                result.Add(entry);
            }
            // Retain keys for existing frames, but don't retain removed native graphs.
            foreach (var stale in containerIds.Keys.Where(d => !player.Flows.Any(f => f.Decorations.Contains(d))).ToArray()) containerIds.Remove(stale);
            return result;
        }
        internal static bool Overlap(FlowDeco container, Vector2Int position, Vector2Int size)
        {
            return container.Position.x < (long)position.x + size.x && (long)container.Position.x + container.Size.x > position.x
                && container.Position.y > (long)position.y - size.y && (long)container.Position.y - container.Size.y < position.y;
        }
        static Sprite DefaultIcon(FlowDeco container)
        {
            if (container?.World)
            {
                // Native frames don't expose an icon; use their own background sprite.
                var image = AccessTools.Field(typeof(Assets.Behaviour.Flow.WorldFlowDeco), "_background").GetValue(container.World) as Image;
                if (image && image.sprite) return image.sprite;
            }
            return SpriteLibrary.Get("Icons2_45");
        }
    }
}
