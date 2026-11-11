using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Player;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerBookTotals
    {
        internal bool Active(int currency) => Earned[currency] != 0 || Spent[currency] != 0;

    }
}
