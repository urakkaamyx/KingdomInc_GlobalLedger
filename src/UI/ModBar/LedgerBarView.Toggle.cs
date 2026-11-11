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
        void Toggle()
        {
            plugin.EnsurePlayer();
            if (GamePlayer.Current == null || !GameUI.Instance) return;
            if (!panel)
            {
                panel = LedgerPanel.Create(plugin.Book, button);
                plugin.Panel = panel;
                panel.OnPopupClosed += Refresh;
            }
            panel.TogglePopup();
            Refresh();
        }

    }
}
