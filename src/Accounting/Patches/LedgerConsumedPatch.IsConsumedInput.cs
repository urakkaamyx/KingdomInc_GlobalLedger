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
        static bool IsConsumedInput(FlowElement element, int inputIndex, InventoryItem item)
        {
            switch (element)
            {
                case RecipeElement recipe:
                    if (inputIndex == 0 && recipe.Recipe?.Results != null && recipe.Recipe.Results.Any(r => r.Enhancement != null))
                        return false;
                    return true;
                case Shop _:
                case LuxuryShop _:
                case Restaurant _:
                case Disenchanter _:
                case RequisitionOffice _:
                case NightmareCollector _:
                case Incinerator _:
                case Dungeon _:
                case WarEffort _:
                    return true;
                case Outfitter _:
                case Inn _:
                case TrainingGrounds _:
                    return inputIndex == 1;
                case Arena _:
                    return !(item.Type is HumanItemType);
                default:
                    return false;
            }
        }

    }
}
