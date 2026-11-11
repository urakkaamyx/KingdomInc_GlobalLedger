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
        internal static string ItemName(ItemType item)
        {
            if (item == null) return "";
            string translated = Translation.Translate(item.GetDisplayName());
            return string.IsNullOrEmpty(translated) || translated.StartsWith("@") ? item.Identifier : translated;
        }

    }
}
