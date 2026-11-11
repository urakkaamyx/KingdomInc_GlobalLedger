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
    public sealed partial class GlobalLedgerPlugin : BaseUnityPlugin
    {
        void RegisterModConfig()
        {
            var section = Context.Settings(
                "Global Ledger",
                "Tracks live currency earnings, operating expenses, and item production across your entire kingdom.");
            section.ClearElements();
            section.AddToggle(EnableRollingAverage, "Rolling average");
            section.AddText("Checking this box enables rolling averages, making the calculations less jumpy but not as accurate in real time.");
            section.AddSlider(RollingAverageEntries, 2, 30, "Entry Count");
        }

    }
}
