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
        internal static void AddCount(Dictionary<string, long> map, string key, long delta)
        {
            if (string.IsNullOrEmpty(key) || key == "dummy_passthrough") return;
            map.TryGetValue(key, out var existing);
            map[key] = existing + delta;
        }

    }
}
