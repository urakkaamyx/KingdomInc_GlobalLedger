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
        internal static (string Sign, long Gold, long Silver, long Copper) SplitGold(long value)
        {
            string sign = value > 0 ? "+" : value < 0 ? "-" : "";
            long abs = Math.Abs(value);
            return (sign, abs / 10000, (abs / 100) % 100, abs % 100);
        }

    }
}
