using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerBook
    {
        internal LedgerBook(Func<double> clock)
        {
            this.clock = clock;
            Reset();
        }

        internal const int CurrencyCount = 9;
        readonly Func<double> clock;
        readonly SortedDictionary<long, Dictionary<string, LedgerBookTotals>> buckets = new SortedDictionary<long, Dictionary<string, LedgerBookTotals>>();
        readonly Dictionary<string, double> lastItemActivity = new Dictionary<string, double>();
        readonly Dictionary<int, double> lastCurrencyActivity = new Dictionary<int, double>();
        double started;
        internal double Elapsed => Math.Max(0, clock() - started);

        internal const int MaxRetentionSeconds = 1800;
        internal bool HasCurrencyActivity(int currency) => lastCurrencyActivity.ContainsKey(currency);
        internal bool HasItemActivity(string itemId) => !string.IsNullOrEmpty(itemId) && lastItemActivity.ContainsKey(itemId);
        internal void Record(LedgerBookSource source, GameCurrency currency, long earned, long spent)
        {
            if (earned == 0 && spent == 0)
                return;
            lastCurrencyActivity[(int)currency] = clock();
            long second = (long)Math.Floor(clock());
            Trim(second);
            if (!buckets.TryGetValue(second, out var entries))
                buckets[second] = entries = new Dictionary<string, LedgerBookTotals>();
            if (!entries.TryGetValue(source.Id, out var totals))
                entries[source.Id] = totals = new LedgerBookTotals(source);
            totals.Earned[(int)currency] += earned;
            totals.Spent[(int)currency] += spent;
        }

        internal void RecordItem(LedgerBookSource source, string itemId, long produced, long consumed)
        {
            if ((produced == 0 && consumed == 0) || string.IsNullOrEmpty(itemId))
                return;
            lastItemActivity[itemId] = clock();
            long second = (long)Math.Floor(clock());
            Trim(second);
            if (!buckets.TryGetValue(second, out var entries))
                buckets[second] = entries = new Dictionary<string, LedgerBookTotals>();
            if (!entries.TryGetValue(source.Id, out var totals))
                entries[source.Id] = totals = new LedgerBookTotals(source);
            if (produced != 0)
            {
                totals.ItemEarned.TryGetValue(itemId, out var old);
                totals.ItemEarned[itemId] = old + produced;
            }

            if (consumed != 0)
            {
                totals.ItemSpent.TryGetValue(itemId, out var old);
                totals.ItemSpent[itemId] = old + consumed;
            }
        }

        internal void Reset()
        {
            buckets.Clear();
            lastItemActivity.Clear();
            lastCurrencyActivity.Clear();
            started = clock();
        }

        internal IReadOnlyList<LedgerBookTotals> Snapshot(int windowSeconds = 60)
        {
            if (windowSeconds <= 0)
                windowSeconds = 1;
            long now = (long)Math.Floor(clock());
            long cutoff = now - windowSeconds;
            Trim(now);
            var result = new Dictionary<string, LedgerBookTotals>();
            foreach (var bucket in buckets)
            {
                if (bucket.Key <= cutoff)
                    continue;
                foreach (var pair in bucket.Value)
                {
                    if (!result.TryGetValue(pair.Key, out var totals))
                        result[pair.Key] = totals = new LedgerBookTotals(pair.Value.Source);
                    totals.Source = pair.Value.Source;
                    for (int c = 0; c < CurrencyCount; c++)
                    {
                        totals.Earned[c] += pair.Value.Earned[c];
                        totals.Spent[c] += pair.Value.Spent[c];
                    }

                    foreach (var item in pair.Value.ItemEarned)
                    {
                        totals.ItemEarned.TryGetValue(item.Key, out var old);
                        totals.ItemEarned[item.Key] = old + item.Value;
                    }

                    foreach (var item in pair.Value.ItemSpent)
                    {
                        totals.ItemSpent.TryGetValue(item.Key, out var old);
                        totals.ItemSpent[item.Key] = old + item.Value;
                    }
                }
            }

            return result.Values.ToArray();
        }

        internal double TimeSinceCurrencyActivity(int currency) => lastCurrencyActivity.TryGetValue(currency, out var time) ? Math.Max(0, clock() - time) : double.MaxValue;
        internal double TimeSinceItemActivity(string itemId) => !string.IsNullOrEmpty(itemId) && lastItemActivity.TryGetValue(itemId, out var time) ? Math.Max(0, clock() - time) : double.MaxValue;
        // 30 minutes
        void Trim(long second)
        {
            foreach (var key in buckets.Keys.TakeWhile(key => key <= second - MaxRetentionSeconds).ToArray())
                buckets.Remove(key);
        }
    }
}
