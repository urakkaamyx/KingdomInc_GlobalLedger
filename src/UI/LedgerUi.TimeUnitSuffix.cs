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
        internal static string TimeUnitSuffix(LedgerPanel.TimeUnit unit)
        {
            switch (unit)
            {
                case LedgerPanel.TimeUnit.Sec: return "/sec";
                case LedgerPanel.TimeUnit.Hour: return "/hour";
                default: return "/min";
            }
        }

    }
}
