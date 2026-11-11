using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Player;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerBook
    {
        internal IReadOnlyList<LedgerBookTotals> Snapshot(int windowSeconds = 60)
        {
            if (windowSeconds <= 0) windowSeconds = 1;
            long now = (long)Math.Floor(clock());
            long cutoff = now - windowSeconds;
            Trim(now);
            var result = new Dictionary<string, LedgerBookTotals>();
            foreach (var bucket in buckets)
            {
                if (bucket.Key <= cutoff) continue;
                foreach (var pair in bucket.Value)
                {
                    if (!result.TryGetValue(pair.Key, out var totals)) result[pair.Key] = totals = new LedgerBookTotals(pair.Value.Source);
                    totals.Source = pair.Value.Source;
                    for (int c = 0; c < CurrencyCount; c++) { totals.Earned[c] += pair.Value.Earned[c]; totals.Spent[c] += pair.Value.Spent[c]; }
                    foreach (var item in pair.Value.ItemEarned) { totals.ItemEarned.TryGetValue(item.Key, out var old); totals.ItemEarned[item.Key] = old + item.Value; }
                    foreach (var item in pair.Value.ItemSpent) { totals.ItemSpent.TryGetValue(item.Key, out var old); totals.ItemSpent[item.Key] = old + item.Value; }
                }
            }
            return result.Values.ToArray();
        }

    }
}
