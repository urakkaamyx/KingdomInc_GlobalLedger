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
        internal TMP_Text Text(Transform parent, string name, string value, float size = 19)
        {
            var rect = Rect(parent, name); Fill(rect);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font.font; text.fontSharedMaterial = font.fontSharedMaterial; text.color = Paper;
            text.fontSize = size; text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis; text.alignment = TextAlignmentOptions.MidlineLeft;
            text.richText = false; text.raycastTarget = false; text.text = value; return text;
        }

    }
}
