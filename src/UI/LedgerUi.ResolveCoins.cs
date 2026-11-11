using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Behaviour.UI.Shared;
using Assets.Source.Items;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerUi
    {
        static void ResolveCoins()
        {
            if (silverCoin && copperCoin) return;
            foreach (var display in Resources.FindObjectsOfTypeAll<CurrencyDisplay>())
            {
                if (!silverCoin) silverCoin = AccessTools.Field(typeof(CurrencyDisplay), "_silverIcon")?.GetValue(display) as Sprite;
                if (!copperCoin) copperCoin = AccessTools.Field(typeof(CurrencyDisplay), "_copperIcon")?.GetValue(display) as Sprite;
                if (silverCoin && copperCoin) return;
            }
        }

    }
}
