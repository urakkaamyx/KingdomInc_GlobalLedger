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
    static partial class LedgerHudVisibilityPatch
    {
        static void Postfix(GameUI __instance)
        {
            var panel = GlobalLedgerPlugin.Instance?.Panel;
            if (panel && panel.gameObject.activeSelf && !__instance.GetComponent<Canvas>().enabled) panel.ClosePopup();
        }

    }
}
