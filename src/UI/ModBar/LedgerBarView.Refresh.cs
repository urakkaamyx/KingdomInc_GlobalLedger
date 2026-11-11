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
        public void Refresh()
        {
            if (button) button.GetComponentInChildren<TMPro.TMP_Text>().text = panel && panel.gameObject.activeSelf ? "Global Ledger ^" : "Global Ledger v";
        }

    }
}
