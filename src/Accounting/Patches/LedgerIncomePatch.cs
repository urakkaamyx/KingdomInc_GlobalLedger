using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace KingdomGlobalLedger
{
    static class LedgerIncomePatch
    {
        internal static void Postfix(GameKingdom __instance, GameCurrency c, long amt, bool isIncome)
        {
            if (!isIncome || amt <= 0)
                return;
            if (LedgerObserver.current != null && LedgerObserver.current.Player == __instance)
                LedgerObserver.current.Income[(int)c] += amt;
            else if (LedgerObserver.current == null)
                LedgerWarProgressPatch.RecordIncome(c, amt);
        }
    }
}
