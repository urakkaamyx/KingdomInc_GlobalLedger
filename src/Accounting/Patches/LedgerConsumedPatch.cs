using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace KingdomGlobalLedger
{
    static class LedgerConsumedPatch
    {
        static bool IsConsumedInput(WorkspaceObject element, int inputIndex, GameInventoryItem item)
        {
            if (element.Production.IsRecipe)
                return inputIndex != 0 || !element.Production.HasEnhancementResult;
            switch (element.TypeId)
            {
                case "Shop":
                case "LuxuryShop":
                case "Restaurant":
                case "Disenchanter":
                case "RequisitionOffice":
                case "NightmareCollector":
                case "Incinerator":
                case "Dungeon":
                case "WarEffort":
                    return true;
                case "Outfitter":
                case "Inn":
                case "TrainingGrounds":
                    return inputIndex == 1;
                case "Arena":
                    return item.Type?.IsHuman != true;
                default:
                    return false;
            }
        }

        internal static void Postfix(WorkspacePort __instance, GameInventoryItem __result)
        {
            if (LedgerObserver.current == null || __result?.Type == null || !__instance.IsInput || __instance.Parent != LedgerObserver.current.Element)
                return;
            var element = LedgerObserver.current.Element;
            int index = __instance.ParentIndex;
            if (element.TypeId == "TrainingGrounds" && index == 0 || element.TypeId == "QuestBoard" && index == 0 || element.TypeId == "Arena" && __result.Type.IsHuman)
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
