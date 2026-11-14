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
        readonly SortedSet<long> bucketSeconds = new SortedSet<long>();
        readonly SortedDictionary<long, Dictionary<string, LedgerBookTotals>> buckets = new SortedDictionary<long, Dictionary<string, LedgerBookTotals>>();
        readonly Dictionary<string, double> lastItemActivity = new Dictionary<string, double>();
        readonly Dictionary<int, double> lastCurrencyActivity = new Dictionary<int, double>();
        double started;
        long lastTrim = long.MinValue;
        internal long BatchWrites { get; private set; }
        internal double Elapsed => Math.Max(0, clock() - started);

        internal const int MaxRetentionSeconds = 24 * 60 * 60;
        internal bool HasCurrencyActivity(int currency) => lastCurrencyActivity.ContainsKey(currency);
        internal bool HasItemActivity(string itemId) => !string.IsNullOrEmpty(itemId) && lastItemActivity.ContainsKey(itemId);
        /// <summary>Commit one pooled operation batch with one clock read and bucket lookup.</summary>
        internal bool RecordBatch(LedgerFrame frame)
        {
            bool active = frame.Produced.Count != 0 || frame.Consumed.Count != 0;
            for (int c = 0; c < CurrencyCount; c++)
                active |= frame.Income[c] > 0 || frame.Spending[c] > 0;
            if (!active)
                return false;
            var source = LedgerObserver.Describe(frame.Element);
            double time = clock();
            long second = (long)Math.Floor(time);
            Trim(second);
            if (!buckets.TryGetValue(second, out var entries))
            {
                bucketSeconds.Add(second);
                buckets[second] = entries = new Dictionary<string, LedgerBookTotals>();
            }
            if (!entries.TryGetValue(source.Id, out var totals))
                entries[source.Id] = totals = new LedgerBookTotals(source);
            totals.Source = source;
            for (int c = 0; c < CurrencyCount; c++)
            {
                long earned = Math.Max(0, frame.Income[c]);
                long spent = Math.Max(0, frame.Spending[c]);
                if (earned == 0 && spent == 0)
                    continue;
                lastCurrencyActivity[c] = time;
                totals.Earned[c] += earned;
                totals.Spent[c] += spent;
            }
            Accumulate(frame.Produced, totals.ItemEarned, time);
            Accumulate(frame.Consumed, totals.ItemSpent, time);
            BatchWrites++;
            return true;
        }

        void Accumulate(Dictionary<string, long> values, Dictionary<string, long> target, double time)
        {
            foreach (var pair in values)
            {
                if (pair.Value == 0)
                    continue;
                target.TryGetValue(pair.Key, out var previous);
                target[pair.Key] = previous + pair.Value;
                lastItemActivity[pair.Key] = time;
            }
        }

        internal void Record(LedgerBookSource source, GameCurrency currency, long earned, long spent)
        {
            if (earned == 0 && spent == 0)
                return;
            lastCurrencyActivity[(int)currency] = clock();
            long second = (long)Math.Floor(clock());
            Trim(second);
            if (!buckets.TryGetValue(second, out var entries))
            {
                bucketSeconds.Add(second);
                buckets[second] = entries = new Dictionary<string, LedgerBookTotals>();
            }
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
            {
                bucketSeconds.Add(second);
                buckets[second] = entries = new Dictionary<string, LedgerBookTotals>();
            }
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
            bucketSeconds.Clear();
            lastItemActivity.Clear();
            lastCurrencyActivity.Clear();
            lastTrim = long.MinValue;
            BatchWrites = 0;
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
            foreach (long second in bucketSeconds.GetViewBetween(cutoff + 1, now))
            {
                foreach (var pair in buckets[second])
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
        // Retain the longest selectable window: 24 hours.
        void Trim(long second)
        {
            if (second == lastTrim)
                return;
            lastTrim = second;
            foreach (var key in bucketSeconds.TakeWhile(key => key <= second - MaxRetentionSeconds).ToArray())
            {
                buckets.Remove(key);
                bucketSeconds.Remove(key);
            }
        }
    }
}
