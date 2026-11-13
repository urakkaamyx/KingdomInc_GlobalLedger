using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerBookTotals
    {
        internal bool Active(int currency) => Earned[currency] != 0 || Spent[currency] != 0;
        internal LedgerBookTotals(LedgerBookSource source)
        {
            Source = source;
        }

        internal LedgerBookSource Source;
        internal readonly long[] Earned = new long[LedgerBook.CurrencyCount], Spent = new long[LedgerBook.CurrencyCount];
        internal readonly Dictionary<string, long> ItemEarned = new Dictionary<string, long>(), ItemSpent = new Dictionary<string, long>();
        internal bool ItemActive(string itemId) => ItemProduced(itemId) != 0 || ItemConsumed(itemId) != 0;
        internal long ItemConsumed(string itemId) => ItemSpent.TryGetValue(itemId, out var value) ? value : 0;
        internal long ItemNet(string itemId) => ItemProduced(itemId) - ItemConsumed(itemId);
        internal long ItemProduced(string itemId) => ItemEarned.TryGetValue(itemId, out var value) ? value : 0;
        internal long Net(int currency) => Earned[currency] - Spent[currency];
    }
}
