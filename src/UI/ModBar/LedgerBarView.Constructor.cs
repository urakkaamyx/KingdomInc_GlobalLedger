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
    internal sealed partial class LedgerBarView : IModBarView
    {
        internal LedgerBarView(GlobalLedgerPlugin plugin, IModBarContext context)
        {
            this.plugin = plugin;
            button = context.CreateButton("Global Ledger v", Toggle);
        }

    }
}
