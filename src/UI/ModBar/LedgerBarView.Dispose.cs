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
        public void Dispose()
        {
            if (panel)
            {
                panel.OnPopupClosed -= Refresh;
                if (GameUI.Instance && panel.gameObject.activeSelf) panel.ClosePopup(false);
                UnityEngine.Object.Destroy(panel.gameObject);
            }
            if (plugin.Panel == panel) plugin.Panel = null;
        }

    }
}
