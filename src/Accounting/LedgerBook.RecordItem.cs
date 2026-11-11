using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Player;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerBook
    {
        internal void RecordItem(LedgerBookSource source, string itemId, long produced, long consumed)
        {
            if ((produced == 0 && consumed == 0) || string.IsNullOrEmpty(itemId)) return;
            lastItemActivity[itemId] = clock();
            long second = (long)Math.Floor(clock());
            Trim(second);
            if (!buckets.TryGetValue(second, out var entries)) buckets[second] = entries = new Dictionary<string, LedgerBookTotals>();
            if (!entries.TryGetValue(source.Id, out var totals)) entries[source.Id] = totals = new LedgerBookTotals(source);
            if (produced != 0) { totals.ItemEarned.TryGetValue(itemId, out var old); totals.ItemEarned[itemId] = old + produced; }
            if (consumed != 0) { totals.ItemSpent.TryGetValue(itemId, out var old); totals.ItemSpent[itemId] = old + consumed; }
        }

    }
}
