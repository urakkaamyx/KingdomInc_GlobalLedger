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
    [HarmonyPatch(typeof(GameUI), nameof(GameUI.ProcessEscape))]
    static partial class LedgerEscapeDismissPatch
    {

    }
}
