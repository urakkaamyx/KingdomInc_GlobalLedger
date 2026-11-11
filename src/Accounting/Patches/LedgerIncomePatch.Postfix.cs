using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Assets.Source.Adventure;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;

namespace KingdomGlobalLedger
{
    static partial class LedgerIncomePatch
    {
        static void Postfix(GamePlayer __instance, Currency c, long amt, bool isIncome)
        {
            if (!isIncome || amt <= 0) return;
            if (LedgerObserver.current != null && LedgerObserver.current.Player == __instance) LedgerObserver.current.Income[(int)c] += amt;
            else if (LedgerObserver.current == null) LedgerWarProgressPatch.RecordIncome(c, amt);
        }

    }
}
