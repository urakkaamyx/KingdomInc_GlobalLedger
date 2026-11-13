using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace KingdomGlobalLedger
{
    static class LedgerSpendingPatch
    {
        internal static void Postfix(GameKingdom __instance, GameCurrency c, long amt, bool __result)
        {
            if (__result && amt > 0 && LedgerObserver.current != null && LedgerObserver.current.Player == __instance)
                LedgerObserver.current.Spending[(int)c] += amt;
        }
    }
}
