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
    static partial class LedgerSpendingPatch
    {
        static void Postfix(GamePlayer __instance, Currency c, long amt, bool __result)
        { if (__result && amt > 0 && LedgerObserver.current != null && LedgerObserver.current.Player == __instance) LedgerObserver.current.Spending[(int)c] += amt; }

    }
}
