using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Assets.Source.Adventure;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;

namespace KingdomGlobalLedger
{
    internal static partial class LedgerObserver
    {
        // Balance in till + wallet income makes collecting a transfer, never a second sale.
        internal static void End(LedgerFrame frame)
        {
            if (frame == null || current != frame) return;
            current = frame.Parent;
            try
            {
                if (frame.Player != GamePlayer.Current || GlobalLedgerPlugin.Instance == null) return;
                long gold = 0, mana = 0, crowns = 0, req = 0;
                switch (frame.Element)
                {
                    case Shop shop: gold = shop.CollectedGold; break;
                    case Disenchanter disenchanter: mana = disenchanter.CollectedMana; break;
                    case Restaurant restaurant: gold = restaurant.CollectedGold; crowns = restaurant.CollectedCrowns; break;
                    case RequisitionOffice office: req = office.CollectedRequisition; break;
                }
                frame.Income[0] += gold - frame.Tray[0];
                frame.Income[1] += req - frame.Tray[1];
                frame.Income[4] += mana - frame.Tray[4];
                frame.Income[5] += crowns - frame.Tray[5];
                SnapshotItemsAfter(frame.Element, frame);
                LedgerBookSource source = null;
                for (int c = 0; c < 9; c++)
                {
                    long earned = Math.Max(0, frame.Income[c]);
                    if (earned == 0 && frame.Spending[c] == 0) continue;
                    if (source == null) source = Describe(frame.Element);
                    GlobalLedgerPlugin.Instance.Book.Record(source, (Currency)c, earned, frame.Spending[c]);
                }
                foreach (var itemId in frame.Produced.Keys.Union(frame.Consumed.Keys))
                {
                    frame.Produced.TryGetValue(itemId, out var produced);
                    frame.Consumed.TryGetValue(itemId, out var consumed);
                    if (produced == 0 && consumed == 0) continue;
                    if (source == null) source = Describe(frame.Element);
                    GlobalLedgerPlugin.Instance.Book.RecordItem(source, itemId, produced, consumed);
                }
            }
            finally { frame.Element = null; frame.Parent = null; frame.Player = null; frame.OutputsBefore.Clear(); frame.VisitorsBefore.Clear(); pool.Push(frame); }
        }

    }
}
