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
        public override IModBarView CreateView(IModBarContext context) => new LedgerBarView(plugin, context);

    }
}
