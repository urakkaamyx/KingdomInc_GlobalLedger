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
        internal static double ScaleRate(long value, LedgerPanel.TimeUnit unit, int divisor = 1)
        {
            if (divisor <= 0) divisor = 1;
            switch (unit)
            {
                case LedgerPanel.TimeUnit.Sec:
                    return divisor > 1 ? (double)value / divisor : value / 60.0;
                case LedgerPanel.TimeUnit.Hour:
                    return divisor > 1 ? ((double)value / divisor) * 60.0 : value * 60.0;
                default:
                    return divisor > 1 ? (double)value / divisor : value;
            }
        }

    }
}
