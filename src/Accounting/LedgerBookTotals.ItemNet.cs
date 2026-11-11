using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Player;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerBookTotals
    {
        internal long ItemNet(string itemId) => ItemProduced(itemId) - ItemConsumed(itemId);

    }
}
