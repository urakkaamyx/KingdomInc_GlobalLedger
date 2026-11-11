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
        internal static void Install(Harmony harmony)
        {
            var prefix = new HarmonyMethod(typeof(LedgerObserver), nameof(Enter));
            var finalizer = new HarmonyMethod(typeof(LedgerObserver), nameof(Leave));
            var names = new HashSet<string> { "Update", "DoManualCraft", "DoCollect", "QueueCraft", "SoldierDied", "UpdateQuestProgress" };
            foreach (var type in typeof(FlowElement).Assembly.GetTypes().Where(t => typeof(FlowElement).IsAssignableFrom(t)))
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => names.Contains(m.Name) && !m.IsAbstract && !m.ContainsGenericParameters))
                    harmony.Patch(method, prefix: prefix, finalizer: finalizer);
            harmony.CreateClassProcessor(typeof(LedgerIncomePatch)).Patch();
            harmony.CreateClassProcessor(typeof(LedgerSpendingPatch)).Patch();
            harmony.CreateClassProcessor(typeof(LedgerConsumedPatch)).Patch();
            harmony.CreateClassProcessor(typeof(LedgerWarProgressPatch)).Patch();
        }

    }
}
