using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Player;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerBookTotals
    {
        internal LedgerBookSource Source;
        internal readonly long[] Earned = new long[LedgerBook.CurrencyCount], Spent = new long[LedgerBook.CurrencyCount];
        internal readonly Dictionary<string, long> ItemEarned = new Dictionary<string, long>(), ItemSpent = new Dictionary<string, long>();

    }
}
