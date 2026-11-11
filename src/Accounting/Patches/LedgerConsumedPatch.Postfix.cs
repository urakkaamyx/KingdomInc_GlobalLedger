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
    static partial class LedgerConsumedPatch
    {
        static void Postfix(FlowIO __instance, InventoryItem __result)
        {
            if (LedgerObserver.current == null || __result?.Type == null || !(__instance is FlowInput) || __instance.Parent != LedgerObserver.current.Element) return;
            var element = LedgerObserver.current.Element;
            int index = __instance.ParentIndex;
            if (element is TrainingGrounds && index == 0 || element is QuestBoard && index == 0 || element is Arena && __result.Type is HumanItemType)
            {
                if (!LedgerObserver.current.VisitorsBefore.Any(v => ReferenceEquals(v.item, __result)))
                    LedgerObserver.current.VisitorsBefore.Add((__result, __result.Type));
                return;
            }
            if (IsConsumedInput(element, index, __result))
                LedgerObserver.AddCount(LedgerObserver.current.Consumed, __result.Type.Identifier, 1);
        }

    }
}
