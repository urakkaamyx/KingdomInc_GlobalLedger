using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomGlobalLedger
{
    static class LedgerHudVisibilityPatch
    {
        internal static void Postfix(Component __instance)
        {
            var panel = GlobalLedgerPlugin.Instance?.Panel;
            if (panel && panel.gameObject.activeSelf && !__instance.GetComponent<Canvas>().enabled)
                panel.ClosePopup();
        }
    }
}
