using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Assets.Source.Adventure;
using Assets.Source.Flow;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerLocations
    {
        internal static string CleanLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string cleaned = RichTextTags.Replace(value, "").Trim();
            return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
        }

    }
}
