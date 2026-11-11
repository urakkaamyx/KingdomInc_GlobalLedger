using System;
using System.Linq;
using Assets.Behaviour.UI;
using Assets.Source.Player;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomGlobalLedger
{
    [BepInPlugin(Id, "Global Ledger", Version)]
    [BepInDependency("urakka.kingdominc.foundation", "0.11.0")]
    public sealed partial class GlobalLedgerPlugin : BaseUnityPlugin
    {
        public const string Id = "urakka.kingdominc.globalledger", Version = "0.1.11";
        internal static GlobalLedgerPlugin Instance;
        internal readonly LedgerBook Book = new LedgerBook(() => Time.realtimeSinceStartupAsDouble);
        internal LedgerPanel Panel;
        internal LedgerNavigation Navigation;
        internal FoundationModContext Context;
        internal ConfigEntry<bool> EnableRollingAverage;
        internal ConfigEntry<int> RollingAverageEntries;
        Harmony hooks;
        GamePlayer player;

    }
}
