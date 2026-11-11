using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Player;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerBook
    {
        internal void Reset()
        {
            buckets.Clear();
            lastItemActivity.Clear();
            lastCurrencyActivity.Clear();
            started = clock();
        }

    }
}
