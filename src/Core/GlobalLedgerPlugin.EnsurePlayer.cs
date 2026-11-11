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
    public sealed partial class GlobalLedgerPlugin : BaseUnityPlugin
    {
        internal void EnsurePlayer()
        {
            if (ReferenceEquals(player, FoundationApi.World.Player)) return;
            player = FoundationApi.World.Player;
            Book.Reset();
            LedgerObserver.Reset();
            if (Navigation) Navigation.Clear();
            if (Panel) { Panel.ResetTree(); if (Panel.gameObject.activeSelf) Panel.ClosePopup(); }
        }

    }
}
