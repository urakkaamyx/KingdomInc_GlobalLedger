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
        internal static string Rate(long value, Currency currency, LedgerPanel.TimeUnit unit = LedgerPanel.TimeUnit.Min, int divisor = 1)
        {
            if (currency == Currency.Gold)
            {
                var (sign, gold, silver, copper) = SplitGoldScaled(value, unit, divisor);
                string suffix = unit == LedgerPanel.TimeUnit.Min ? "" : " " + TimeUnitSuffix(unit);
                return sign + gold.ToString("#,0", CultureInfo.InvariantCulture) + "g " + silver + "s " + copper + "c" + suffix;
            }
            double amount = Math.Round(ScaleRate(value, unit, divisor), 2, MidpointRounding.AwayFromZero);
            return (amount > 0 ? "+" : "") + amount.ToString("0.00", CultureInfo.InvariantCulture) + TimeUnitSuffix(unit);
        }

    }
}
