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
        internal void Layout(float width)
        {
            if (data == null) return;
            LedgerUi.Place(Root, 0, index * 36, width, 35);
            if (data.IsHeader)
            {
                expander.gameObject.SetActive(false);
                ViewButton.gameObject.SetActive(false);
                amount.gameObject.SetActive(false);
                goldGroup.gameObject.SetActive(false);
                prefixIcon.enabled = false;
                separator.text = "";
                icon.enabled = false;
                LedgerUi.Place(label.rectTransform, 24, 0, width - 48, 35);
                return;
            }
            float left = 12 + data.Depth * 24;
            LedgerUi.Place((RectTransform)expander.transform, left - 3, 0, 24, 35);
            float textLeft;
            if (data.PrefixIcon)
            {
                LedgerUi.Place((RectTransform)prefixIcon.transform, left + 22, 8, 20, 20);
                LedgerUi.Place(separator.rectTransform, left + 43, 0, 10, 35);
                LedgerUi.Place((RectTransform)icon.transform, left + 54, 8, 20, 20);
                textLeft = left + (data.Icon ? 80 : 48);
            }
            else
            {
                LedgerUi.Place((RectTransform)icon.transform, left + 22, 8, 20, 20);
                textLeft = left + (data.Icon ? 48 : 24);
            }
            float rightReserve = data.Currency.HasValue || data.Item != null ? 300 : data.Target != null ? 74 : 16;
            LedgerUi.Place(label.rectTransform, textLeft, 0, Mathf.Max(24, width - textLeft - rightReserve), 35);
            LedgerUi.Place(amount.rectTransform, Mathf.Max(0, width - 250), 0, 180, 35);
            LedgerUi.Place(goldGroup, Mathf.Max(0, width - 300), 0, 240, 35);
            if (data.Currency == Currency.Gold)
            {
                float cursor = 240;
                float unitW = Mathf.Ceil(goldUnit.GetPreferredValues(goldUnit.text).x) + 4;
                LedgerUi.Place(goldUnit.rectTransform, cursor - unitW, 0, unitW, 35);
                cursor -= unitW;
                PlaceCoinPair(copperText, copperCoin, ref cursor, 0);
                PlaceCoinPair(silverText, silverCoin, ref cursor, 7);
                PlaceCoinPair(goldText, goldCoin, ref cursor, 7);
            }
            LedgerUi.Place((RectTransform)ViewButton.transform, Mathf.Max(0, width - 66), 4, 58, 27);
        }

    }
}
