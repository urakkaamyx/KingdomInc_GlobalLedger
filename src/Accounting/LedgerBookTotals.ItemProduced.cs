using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Player;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerBookTotals
    {
        internal long ItemProduced(string itemId) => ItemEarned.TryGetValue(itemId, out var value) ? value : 0;

    }
}
