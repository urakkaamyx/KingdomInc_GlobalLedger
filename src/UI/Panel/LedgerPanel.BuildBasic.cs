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
        void BuildBasic()
        {
            if (GamePlayer.Current == null) return;
            var currentTotals = book.Snapshot(windowSeconds);

            // Section 1: CURRENCIES
            VisibleRows.Add(new LedgerRowData("basic/header/currencies", "CURRENCY NET COLLECTION", 0) { IsHeader = true });
            int currencyCount = 0;
            foreach (Currency currency in Enum.GetValues(typeof(Currency)))
            {
                if (!currency.IsUnlocked()) continue;
                int c = (int)currency;
                bool hasActivity = book.HasCurrencyActivity(c);
                bool isActiveInWindow = currentTotals.Any(t => t.Active(c));
                double timeSinceActivity = book.TimeSinceCurrencyActivity(c);

                if (!showAll)
                {
                    if (currency != Currency.Gold && !hasActivity) continue;
                    if (!isActiveInWindow && timeSinceActivity > windowSeconds + 60.0 && currency != Currency.Gold) continue;
                }

                long net = currentTotals.Where(t => t.Active(c)).Sum(t => t.Net(c));
                var row = new LedgerRowData("basic/currency/" + c, LedgerUi.CurrencyName(currency), 1)
                {
                    Currency = currency,
                    Amount = net,
                    Expandable = false,
                    Icon = currency.GetIcon()
                };
                VisibleRows.Add(row);
                currencyCount++;
            }
            if (currencyCount == 0 && !showAll)
            {
                VisibleRows.Add(new LedgerRowData("basic/currency/empty", "No observed currency activity in the recent window. Click Show All to list unlocked currencies.", 1));
            }

            // Section 2: PRODUCTION
            VisibleRows.Add(new LedgerRowData("basic/header/production", "ITEM NET PRODUCTION", 0) { IsHeader = true });
            var items = LedgerUi.OrderedUnlockedItems();
            int count = 0;
            foreach (var item in items)
            {
                string id = item.Identifier;
                bool hasActivity = book.HasItemActivity(id);
                bool isActiveInWindow = currentTotals.Any(t => t.ItemActive(id));
                double timeSinceActivity = book.TimeSinceItemActivity(id);

                if (!showAll)
                {
                    // 1. If something hasnt been made DO NOT SHOW IT
                    if (!hasActivity) continue;

                    // 2. If somethings qty hasnt been greater than 0 within the selected time,
                    // and it has been more than a rolling minute (60s) since it was last made, DO NOT SHOW IT.
                    // 3. If something has been made, and value keeps changing: SHOW IT (isActiveInWindow == true).
                    // 4. If something has been made recently, but drops to 0: SHOW IT FOR A ROLLING MINUTE (timeSinceActivity <= windowSeconds + 60.0).
                    if (!isActiveInWindow && timeSinceActivity > windowSeconds + 60.0) continue;
                }

                long net = currentTotals.Where(t => t.ItemActive(id)).Sum(t => t.ItemNet(id));
                var row = new LedgerRowData("basic/item/" + id, LedgerUi.ItemName(item), 1)
                {
                    Item = item,
                    Amount = net,
                    Expandable = false,
                    Icon = item.GetIcon()
                };
                VisibleRows.Add(row);
                count++;
            }
            if (count == 0 && !showAll)
            {
                VisibleRows.Add(new LedgerRowData("basic/item/empty", "No observed production in the recent window. Click Show All to list unlocked items.", 1));
            }
        }

    }
}
