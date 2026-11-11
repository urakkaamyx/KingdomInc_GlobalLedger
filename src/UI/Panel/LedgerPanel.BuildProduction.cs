using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.Shared;
using Assets.Behaviour.UI.TechTree;
using Assets.Source.Items;
using Assets.Source.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    public sealed partial class LedgerPanel : MonoBehaviour
    {
        void BuildProduction()
        {
            if (GamePlayer.Current == null) return;
            var data = locations.Resolve(book.Snapshot(windowSeconds));
            foreach (var item in LedgerUi.OrderedUnlockedItems())
            {
                string id = item.Identifier;
                string root = "item/" + id;
                var active = data.Where(t => t.Totals.ItemActive(id)).ToArray();
                if (!showAll && active.Length == 0) continue;
                AddItem(root, LedgerUi.ItemName(item) + " - Net Production", 0, item, active.Sum(t => t.Totals.ItemNet(id)), true);
                if (!expanded.Contains(root)) continue;
                if (active.Length == 0) { VisibleRows.Add(new LedgerRowData(root + "/empty", "No observed activity in the recent window.", 1)); continue; }
                foreach (var group in active.GroupBy(t => t.GroupKey).OrderBy(g => g.First().GroupName, StringComparer.CurrentCulture).ThenBy(g => g.Key, StringComparer.Ordinal))
                {
                    string groupKey = root + "/" + group.Key;
                    var container = group.First();
                    var containerRow = AddItem(groupKey, container.GroupName, 1, item, group.Sum(t => t.Totals.ItemNet(id)), true);
                    containerRow.PrefixIcon = container.GroupTarget != null ? LedgerLocations.ContainerIcon() : null;
                    containerRow.Icon = container.GroupIcon; containerRow.Target = container.GroupTarget;
                    if (!expanded.Contains(groupKey)) continue;
                    foreach (var entry in group.OrderBy(t => t.BuildingName, StringComparer.CurrentCulture).ThenBy(t => t.Totals.Source.Id, StringComparer.Ordinal))
                    {
                        var building = entry.Totals;
                        string buildingKey = groupKey + "/" + building.Source.Id;
                        var buildingRow = AddItem(buildingKey, entry.BuildingName, 2, item, building.ItemNet(id), true);
                        buildingRow.Icon = entry.BuildingIcon; buildingRow.Target = entry.BuildingTarget;
                        if (!expanded.Contains(buildingKey)) continue;
                        string costs = buildingKey + "/costs";
                        VisibleRows.Add(new LedgerRowData(costs, "Operating Costs", 3) { Expandable = true, Open = expanded.Contains(costs) });
                        if (!expanded.Contains(costs)) continue;
                        bool any = false;
                        foreach (var costItem in LedgerUi.OrderedUnlockedItems())
                        {
                            long spentItem = building.ItemConsumed(costItem.Identifier);
                            if (spentItem != 0)
                            {
                                var cost = AddItem(costs + "/item/" + costItem.Identifier, LedgerUi.ItemName(costItem), 4, costItem, -spentItem, false);
                                cost.Icon = costItem.GetIcon(); cost.IconOnly = true; any = true;
                            }
                        }
                        foreach (Currency costCurrency in Enum.GetValues(typeof(Currency)))
                            if (building.Spent[(int)costCurrency] != 0 && costCurrency.IsUnlocked())
                            {
                                var cost = Add(costs + "/currency/" + (int)costCurrency, LedgerUi.CurrencyName(costCurrency), 4, costCurrency, -building.Spent[(int)costCurrency], false);
                                cost.Icon = costCurrency.GetIcon(); cost.IconOnly = true; any = true;
                            }
                        if (!any) VisibleRows.Add(new LedgerRowData(costs + "/empty", "No observed operating costs", 4));
                    }
                }
            }
            if (VisibleRows.Count == 0)
                VisibleRows.Add(new LedgerRowData("production/empty", "No observed production in the last minute. Click Show All to list unlocked items.", 0));
        }

    }
}
