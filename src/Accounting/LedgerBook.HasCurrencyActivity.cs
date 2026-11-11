using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Player;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerBook
    {
        internal bool HasCurrencyActivity(int currency) => lastCurrencyActivity.ContainsKey(currency);

    }
}
