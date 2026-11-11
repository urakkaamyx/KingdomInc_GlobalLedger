using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Player;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerBookTotals
    {
        internal bool ItemActive(string itemId) => ItemProduced(itemId) != 0 || ItemConsumed(itemId) != 0;

    }
}
