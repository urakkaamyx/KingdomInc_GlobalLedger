using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerTreeRow
    {
        internal void Bind(LedgerRowData value, float width)
        {
            data = value;
            Root.name = value.Key;
            Root.gameObject.SetActive(true);
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
            label.color = value.Depth == 0 ? LedgerUi.Gold : value.GameCurrency.HasValue || value.Item != null || value.Expandable ? LedgerUi.Paper : LedgerUi.Muted;
            shade.color = value.Depth == 0 ? new Color(.35f, .22f, .13f, .7f) : new Color(0, 0, 0, index % 2 == 0 ? .13f : .03f);
            Color rateColor = value.Amount > 0 ? LedgerUi.Gain : value.Amount < 0 ? LedgerUi.Loss : LedgerUi.Muted;
            bool isGold = value.GameCurrency == GameCurrency.Gold;
            amount.text = value.GameCurrency.HasValue ? LedgerUi.Rate(value.Amount, value.GameCurrency.Value, owner.Unit, owner.Divisor) : value.Item != null ? LedgerUi.ItemRate(value.Amount, owner.Unit, owner.Divisor) : "";
            amount.color = rateColor;
            amount.gameObject.SetActive(!isGold && (value.GameCurrency.HasValue || value.Item != null));
            goldGroup.gameObject.SetActive(isGold);
            if (isGold)
            {
                var(sign, g, s, c) = LedgerUi.SplitGoldScaled(value.Amount, owner.Unit, owner.Divisor);
                goldText.text = sign + g.ToString("#,0", CultureInfo.InvariantCulture);
                silverText.text = s.ToString(CultureInfo.InvariantCulture);
                copperText.text = c.ToString(CultureInfo.InvariantCulture);
                goldText.color = silverText.color = copperText.color = rateColor;
                goldUnit.text = LedgerUi.TimeUnitSuffix(owner.Unit);
                goldCoin.sprite = LedgerUi.GoldCoin();
                goldCoin.enabled = goldCoin.sprite;
                silverCoin.sprite = LedgerUi.SilverCoin();
                silverCoin.enabled = silverCoin.sprite;
                copperCoin.sprite = LedgerUi.CopperCoin();
                copperCoin.enabled = copperCoin.sprite;
            }

            prefixIcon.enabled = value.PrefixIcon;
            if (prefixIcon.enabled)
                prefixIcon.sprite = value.PrefixIcon;
            separator.text = value.PrefixIcon && value.Icon ? "|" : "";
            icon.enabled = value.Icon;
            if (icon.enabled)
                icon.sprite = value.Icon;
            Layout(width);
        }

        static Image CoinIcon(Transform parent, string name)
        {
            var image = LedgerUi.Rect(parent, name).gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        static TMP_Text CoinText(LedgerUi ui, Transform parent, string name)
        {
            var text = ui.Text(parent, name, "", 18);
            text.alignment = TextAlignmentOptions.MidlineRight;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        internal LedgerTreeRow(LedgerPanel owner, LedgerUi ui, Transform parent, int index)
        {
            this.owner = owner;
            this.index = index;
            Root = LedgerUi.Rect(parent, "TreeRow" + index);
            shade = ui.Background(Root, Color.clear);
            Button = Root.gameObject.AddComponent<Button>();
            Button.targetGraphic = shade;
            Button.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
            Button.onClick.AddListener(() =>
            {
                if (EventSystem.current)
                    EventSystem.current.SetSelectedGameObject(null);
                if (data != null)
                {
                    FoundationApi.UiAccess.PlaySound(GameUiSound.Button);
                    owner.Activate(data);
                }
            });
            hover = Root.gameObject.AddComponent<LedgerRowHover>();
            var arrowRect = LedgerUi.Rect(Root, "Expander");
            var arrowHit = ui.Background(arrowRect, Color.clear);
            expander = arrowRect.gameObject.AddComponent<Button>();
            expander.targetGraphic = arrowHit;
            expander.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
            expander.onClick.AddListener(() =>
            {
                if (EventSystem.current)
                    EventSystem.current.SetSelectedGameObject(null);
                if (data?.Expandable == true)
                {
                    FoundationApi.UiAccess.PlaySound(GameUiSound.Button);
                    owner.ToggleRow(data.Key);
                }
            });
            arrow = ui.Text(arrowRect, "Arrow", "", 17);
            prefixIcon = LedgerUi.Rect(Root, "ContainerIcon").gameObject.AddComponent<Image>();
            prefixIcon.preserveAspect = true;
            prefixIcon.raycastTarget = false;
            separator = ui.Text(Root, "IconSeparator", "", 17);
            separator.alignment = TextAlignmentOptions.Center;
            separator.color = LedgerUi.Muted;
            icon = LedgerUi.Rect(Root, "CurrencyIcon").gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            label = ui.Text(Root, "Name", "");
            amount = ui.Text(Root, "Rate", "");
            amount.alignment = TextAlignmentOptions.MidlineRight;
            amount.enableAutoSizing = true;
            amount.fontSizeMin = 13;
            amount.fontSizeMax = 19;
            goldGroup = LedgerUi.Rect(Root, "GoldRate");
            goldText = CoinText(ui, goldGroup, "GoldAmount");
            goldCoin = CoinIcon(goldGroup, "GoldCoin");
            silverText = CoinText(ui, goldGroup, "SilverAmount");
            silverCoin = CoinIcon(goldGroup, "SilverCoin");
            copperText = CoinText(ui, goldGroup, "CopperAmount");
            copperCoin = CoinIcon(goldGroup, "CopperCoin");
            goldUnit = ui.Text(goldGroup, "GoldUnit", "", 13);
            goldUnit.color = LedgerUi.Muted;
            goldUnit.alignment = TextAlignmentOptions.MidlineLeft;
            ViewButton = ui.Button(Root, "View", "View", () =>
            {
                if (data?.Target != null)
                    owner.View(data);
            }, 14);
        }

        internal readonly RectTransform Root;
        internal readonly Button Button, ViewButton;
        readonly TMP_Text label, amount, arrow, separator, goldText, silverText, copperText, goldUnit;
        readonly RectTransform goldGroup;
        readonly Image goldCoin, silverCoin, copperCoin;
        readonly Button expander;
        readonly LedgerRowHover hover;
        readonly Image prefixIcon, icon, shade;
        readonly int index;
        readonly LedgerPanel owner;
        LedgerRowData data;
        internal void Layout(float width)
        {
            if (data == null)
                return;
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

            float rightReserve = data.GameCurrency.HasValue || data.Item != null ? 300 : data.Target != null ? 74 : 16;
            LedgerUi.Place(label.rectTransform, textLeft, 0, Mathf.Max(24, width - textLeft - rightReserve), 35);
            LedgerUi.Place(amount.rectTransform, Mathf.Max(0, width - 250), 0, 180, 35);
            LedgerUi.Place(goldGroup, Mathf.Max(0, width - 300), 0, 240, 35);
            if (data.GameCurrency == GameCurrency.Gold)
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
