using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Player;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerBook
    {
        internal const int CurrencyCount = 9;
        readonly Func<double> clock;
        readonly SortedDictionary<long, Dictionary<string, LedgerBookTotals>> buckets = new SortedDictionary<long, Dictionary<string, LedgerBookTotals>>();
        readonly Dictionary<string, double> lastItemActivity = new Dictionary<string, double>();
        readonly Dictionary<int, double> lastCurrencyActivity = new Dictionary<int, double>();
        double started;
        internal double Elapsed => Math.Max(0, clock() - started);
        internal const int MaxRetentionSeconds = 1800;

    }
}
