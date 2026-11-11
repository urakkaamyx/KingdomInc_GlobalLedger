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
    internal static partial class LedgerObserver
    {
        internal static LedgerBookSource Describe(FlowElement element)
        {
            string label = element is ResourceElement resource ? resource.DefaultLabel : element is RecipeElement recipe ? recipe.DefaultLabel : element.Label;
            string typeName = Translation.Translate(label);
            if (string.IsNullOrEmpty(typeName) || typeName.StartsWith("@")) typeName = Regex.Replace(element.ElementType, "([a-z])([A-Z])", "$1 $2");
            var flow = element.ParentFlow;
            string detail = element is RecipeElement crafting && crafting.Recipe != null ? Translation.Translate(crafting.Recipe.DisplayName) : Translation.Translate(element.Label);
            string name = (LedgerLocations.CleanLabel(flow?.Label) ?? "Workspace") + " / " + detail + " [" + element.Position.x + ", " + element.Position.y + "]";
            return new LedgerBookSource(element.Guid, element.ElementType, typeName, name);
        }

    }
}
