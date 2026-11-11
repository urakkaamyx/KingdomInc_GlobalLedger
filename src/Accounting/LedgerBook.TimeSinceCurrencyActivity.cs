using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Player;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerBook
    {
        internal double TimeSinceCurrencyActivity(int currency) => lastCurrencyActivity.TryGetValue(currency, out var time) ? Math.Max(0, clock() - time) : double.MaxValue;

    }
}
