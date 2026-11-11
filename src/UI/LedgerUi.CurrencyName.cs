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
        internal static string CurrencyName(Currency currency)
        {
            switch (currency) { case Currency.Medal: return "Medals"; case Currency.Ring: return "Rings"; case Currency.Crown: return "Crowns"; case Currency.Ducat: return "Ducats"; default: return currency.ToString(); }
        }

    }
}
