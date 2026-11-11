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
    static partial class LedgerEscapeDismissPatch
    {
        static bool Prefix(GameUI __instance)
        {
            var panel = GlobalLedgerPlugin.Instance?.Panel;
            if (!panel || !panel.gameObject.activeSelf || !__instance.GetComponent<Canvas>().enabled || UIAlertWindow.Active || __instance.IsActive(GameUIType.Popup))
                return true;
            panel.ClosePopup();
            return false;
        }

    }
}
