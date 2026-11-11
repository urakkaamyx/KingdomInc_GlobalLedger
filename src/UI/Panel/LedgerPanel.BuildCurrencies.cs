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
        void BuildCurrencies()
        {
            if (GamePlayer.Current == null) return;
            var data = locations.Resolve(book.Snapshot(windowSeconds));
            foreach (Currency currency in Enum.GetValues(typeof(Currency)))
            {
                if (!currency.IsUnlocked()) continue;
                int c = (int)currency;
                string root = "currency/" + c;
                var active = data.Where(t => t.Totals.Active(c)).ToArray();
                Add(root, LedgerUi.CurrencyName(currency) + " - Net Earnings", 0, currency, active.Sum(t => t.Totals.Net(c)), true);
                if (!expanded.Contains(root)) continue;
                if (active.Length == 0) { VisibleRows.Add(new LedgerRowData(root + "/empty", "No observed activity in the recent window.", 1)); continue; }
                foreach (var group in active.GroupBy(t => t.GroupKey).OrderBy(g => g.First().GroupName, StringComparer.CurrentCulture).ThenBy(g => g.Key, StringComparer.Ordinal))
                {
                    string groupKey = root + "/" + group.Key;
                    var container = group.First();
                    var containerRow = Add(groupKey, container.GroupName, 1, currency, group.Sum(t => t.Totals.Net(c)), true);
                    containerRow.PrefixIcon = container.GroupTarget != null ? LedgerLocations.ContainerIcon() : null;
                    containerRow.Icon = container.GroupIcon; containerRow.Target = container.GroupTarget;
                    if (!expanded.Contains(groupKey)) continue;
                    foreach (var entry in group.OrderBy(t => t.BuildingName, StringComparer.CurrentCulture).ThenBy(t => t.Totals.Source.Id, StringComparer.Ordinal))
                    {
                        var building = entry.Totals;
                        string buildingKey = groupKey + "/" + building.Source.Id;
                        var buildingRow = Add(buildingKey, entry.BuildingName, 2, currency, building.Net(c), true);
                        buildingRow.Icon = entry.BuildingIcon; buildingRow.Target = entry.BuildingTarget;
                        if (!expanded.Contains(buildingKey)) continue;
                        string costs = buildingKey + "/costs";
                        VisibleRows.Add(new LedgerRowData(costs, "Operating Costs", 3) { Expandable = true, Open = expanded.Contains(costs) });
                        if (!expanded.Contains(costs)) continue;
                        bool any = false;
                        foreach (Currency costCurrency in Enum.GetValues(typeof(Currency)))
                            if (building.Spent[(int)costCurrency] != 0 && costCurrency.IsUnlocked())
                            {
                                var cost = Add(costs + "/" + (int)costCurrency, LedgerUi.CurrencyName(costCurrency), 4, costCurrency, -building.Spent[(int)costCurrency], false);
                                cost.Icon = costCurrency.GetIcon(); cost.IconOnly = true; any = true;
                            }
                        if (!any) VisibleRows.Add(new LedgerRowData(costs + "/empty", "No observed operating costs", 4));
                    }
                }
            }
        }

    }
}
