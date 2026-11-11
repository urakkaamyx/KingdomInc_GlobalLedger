using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Player;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerBook
    {
        internal void Record(LedgerBookSource source, Currency currency, long earned, long spent)
        {
            if (earned == 0 && spent == 0) return;
            lastCurrencyActivity[(int)currency] = clock();
            long second = (long)Math.Floor(clock());
            Trim(second);
            if (!buckets.TryGetValue(second, out var entries)) buckets[second] = entries = new Dictionary<string, LedgerBookTotals>();
            if (!entries.TryGetValue(source.Id, out var totals)) entries[source.Id] = totals = new LedgerBookTotals(source);
            totals.Earned[(int)currency] += earned;
            totals.Spent[(int)currency] += spent;
        }

    }
}
