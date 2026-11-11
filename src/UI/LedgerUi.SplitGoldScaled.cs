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
        internal static (string Sign, long Gold, long Silver, long Copper) SplitGoldScaled(long value, LedgerPanel.TimeUnit unit, int divisor = 1)
        {
            if (divisor <= 0) divisor = 1;
            long scaled;
            switch (unit)
            {
                case LedgerPanel.TimeUnit.Sec:
                    scaled = divisor > 1 ? (long)Math.Round((double)value / divisor) : (long)Math.Round(value / 60.0);
                    break;
                case LedgerPanel.TimeUnit.Hour:
                    scaled = divisor > 1 ? (long)Math.Round(((double)value / divisor) * 60.0) : value * 60;
                    break;
                default:
                    scaled = divisor > 1 ? (long)Math.Round((double)value / divisor) : value;
                    break;
            }
            return SplitGold(scaled);
        }

    }
}
