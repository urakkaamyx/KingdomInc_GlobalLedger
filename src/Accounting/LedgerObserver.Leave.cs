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
        static Exception Leave(LedgerFrame __state, Exception __exception)
        {
            try { End(__state); }
            catch (Exception error) { BepInEx.Logging.Logger.CreateLogSource("Global Ledger").LogWarning(error); }
            return __exception;
        }

    }
}
