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
        internal Image Background(RectTransform root, Color color, bool framed = false)
        {
            var image = root.gameObject.AddComponent<Image>(); image.color = color;
            if (framed && frame) { image.sprite = frame; image.type = Image.Type.Sliced; image.color = Color.white; }
            return image;
        }

    }
}
