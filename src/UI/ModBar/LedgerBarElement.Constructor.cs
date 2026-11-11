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
    internal sealed partial class LedgerBarElement : ModBarElement
    {
        internal LedgerBarElement(GlobalLedgerPlugin plugin) : base(GlobalLedgerPlugin.Id, "open", 220, 0, ModBarAlignment.Right) { this.plugin = plugin; }

    }
}
