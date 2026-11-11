using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.Shared;
using Assets.Behaviour.UI.TechTree;
using Assets.Source.Items;
using Assets.Source.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerTreeRow
    {
        static void PlaceCoinPair(TMP_Text text, Image coin, ref float cursor, float rightGap)
        {
            cursor -= rightGap;
            LedgerUi.Place(coin.rectTransform, cursor - 16, 9.5f, 16, 16);
            cursor -= 18;
            float textW = Mathf.Ceil(text.GetPreferredValues(text.text).x) + 2;
            LedgerUi.Place(text.rectTransform, cursor - textW, 0, textW, 35);
            cursor -= textW;
        }

    }
}
