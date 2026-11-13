using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomGlobalLedger
{
    static class LedgerEscapeDismissPatch
    {
        internal static bool Prefix(Component __instance)
        {
            var panel = GlobalLedgerPlugin.Instance?.Panel;
            if (!panel || !panel.gameObject.activeSelf || !__instance.GetComponent<Canvas>().enabled || FoundationApi.UiAccess.IsAlertVisible || FoundationApi.UiAccess.HasPopup)
                return true;
            panel.ClosePopup();
            return false;
        }
    }
}
