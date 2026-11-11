using KingdomFoundation.Api;
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
    internal sealed partial class LedgerLocations
    {
        internal IReadOnlyList<LedgerLocationEntry> Resolve(IReadOnlyList<LedgerBookTotals> totals)
        {
            var player = FoundationApi.World.Player;
            if (player == null) return Array.Empty<LedgerLocationEntry>();
            var allFlows = GetAllFlows(player).ToList();
            var live = new Dictionary<string, LedgerTarget>();
            foreach (var flow in allFlows)
                foreach (var element in flow.Elements) live[element.Guid] = new LedgerTarget(flow, element);

            // Pre-scan all live containers in a fixed spatial order across workspaces:
            // flow index -> position Y descending -> position X ascending -> decoration index.
            var containerList = new List<(FlowDiagram Flow, FlowDeco Deco, string BaseName)>();
            for (int f = 0; f < allFlows.Count; f++)
            {
                var flow = allFlows[f];
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
                .OrderBy(c => allFlows.IndexOf(c.Flow))
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

            var result = new List<LedgerLocationEntry>();
            foreach (var item in totals)
            {
                var entry = new LedgerLocationEntry { Totals = item, BuildingName = item.Source.Name };
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
                    int flowIdx = allFlows.IndexOf(flow);
                    entry.GroupKey = "uncontained/" + (flowIdx >= 0 ? flowIdx.ToString() : flow.GetHashCode().ToString());
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
            foreach (var stale in containerIds.Keys.Where(d => !allFlows.Any(f => f.Decorations.Contains(d))).ToArray()) containerIds.Remove(stale);
            return result;
        }

    }
}
