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
        internal void Bind(LedgerRowData value, float width)
        {
            data = value; Root.name = value.Key; Root.gameObject.SetActive(true);
            if (value.IsHeader)
            {
                expander.gameObject.SetActive(false);
                ViewButton.gameObject.SetActive(false);
                hover.Bind(null);
                label.text = value.Label;
                label.color = LedgerUi.Gold;
                label.fontStyle = FontStyles.Bold;
                shade.color = new Color(.28f, .16f, .10f, .85f);
                amount.gameObject.SetActive(false);
                goldGroup.gameObject.SetActive(false);
                prefixIcon.enabled = false;
                separator.text = "";
                icon.enabled = false;
                Layout(width);
                return;
            }
            label.fontStyle = FontStyles.Normal;
            arrow.text = value.Expandable ? (value.Open ? "v" : ">") : "";
            expander.gameObject.SetActive(value.Expandable);
            ViewButton.gameObject.SetActive(value.Target != null);
            hover.Bind(value.Target);
            label.text = value.IconOnly ? "" : value.Label;
            label.color = value.Depth == 0 ? LedgerUi.Gold : value.Currency.HasValue || value.Item != null || value.Expandable ? LedgerUi.Paper : LedgerUi.Muted;
            shade.color = value.Depth == 0 ? new Color(.35f, .22f, .13f, .7f) : new Color(0, 0, 0, index % 2 == 0 ? .13f : .03f);
            Color rateColor = value.Amount > 0 ? LedgerUi.Gain : value.Amount < 0 ? LedgerUi.Loss : LedgerUi.Muted;
            bool isGold = value.Currency == Currency.Gold;
            amount.text = value.Currency.HasValue ? LedgerUi.Rate(value.Amount, value.Currency.Value, owner.Unit, owner.Divisor) : value.Item != null ? LedgerUi.ItemRate(value.Amount, owner.Unit, owner.Divisor) : "";
            amount.color = rateColor;
            amount.gameObject.SetActive(!isGold && (value.Currency.HasValue || value.Item != null));
            goldGroup.gameObject.SetActive(isGold);
            if (isGold)
            {
                var (sign, g, s, c) = LedgerUi.SplitGoldScaled(value.Amount, owner.Unit, owner.Divisor);
                goldText.text = sign + g.ToString("#,0", CultureInfo.InvariantCulture);
                silverText.text = s.ToString(CultureInfo.InvariantCulture);
                copperText.text = c.ToString(CultureInfo.InvariantCulture);
                goldText.color = silverText.color = copperText.color = rateColor;
                goldUnit.text = LedgerUi.TimeUnitSuffix(owner.Unit);
                goldCoin.sprite = LedgerUi.GoldCoin(); goldCoin.enabled = goldCoin.sprite;
                silverCoin.sprite = LedgerUi.SilverCoin(); silverCoin.enabled = silverCoin.sprite;
                copperCoin.sprite = LedgerUi.CopperCoin(); copperCoin.enabled = copperCoin.sprite;
            }
            prefixIcon.enabled = value.PrefixIcon;
            if (prefixIcon.enabled) prefixIcon.sprite = value.PrefixIcon;
            separator.text = value.PrefixIcon && value.Icon ? "|" : "";
            icon.enabled = value.Icon;
            if (icon.enabled) icon.sprite = value.Icon;
            Layout(width);
        }

    }
}
