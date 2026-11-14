using System;
using System.Globalization;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerRateCell
    {
        readonly RectTransform root, coins;
        readonly TMP_Text amount;
        readonly TMP_Text[] coinText = new TMP_Text[3];
        readonly Image[] coinIcon = new Image[3];

        internal LedgerRateCell(LedgerUi ui, Transform parent, string name)
        {
            root = LedgerUi.Rect(parent, name);
            root.gameObject.AddComponent<RectMask2D>();
            amount = ui.Text(root, "Amount", "", 17);
            amount.alignment = TextAlignmentOptions.MidlineRight;
            amount.enableAutoSizing = true;
            amount.fontSizeMin = 12;
            amount.fontSizeMax = 17;
            coins = LedgerUi.Rect(root, "Coins");
            for (int i = 0; i < coinText.Length; i++)
            {
                coinText[i] = ui.Text(coins, "Amount" + i, "", 17);
                coinText[i].alignment = TextAlignmentOptions.MidlineRight;
                coinIcon[i] = LedgerUi.Rect(coins, "Coin" + i).gameObject.AddComponent<Image>();
                coinIcon[i].preserveAspect = true;
                coinIcon[i].raycastTarget = false;
            }
        }

        internal void Bind(LedgerRowData row, long value, bool net, LedgerPanel owner, float x)
        {
            bool visible = !row.IsHeader && (row.GameCurrency.HasValue || row.Item != null);
            root.gameObject.SetActive(visible);
            if (!visible) return;
            LedgerUi.Place(root, x, 0, LedgerPanel.CellWidth, LedgerPanel.RowHeight - 1);
            Color color = net ? value > 0 ? LedgerUi.Gain : value < 0 ? LedgerUi.Loss : LedgerUi.Muted : LedgerUi.Paper;
            bool gold = row.GameCurrency == GameCurrency.Gold;
            coins.gameObject.SetActive(gold);
            amount.gameObject.SetActive(!gold);
            if (gold) BindGold((long)Math.Round(owner.Rate(value)), net, color);
            else
            {
                amount.text = LedgerUi.FormatAmount(owner.Rate(value), net);
                amount.color = color;
                LedgerUi.Fill(amount.rectTransform, 0, 0, 4, 0);
            }
        }

        void BindGold(long scaled, bool net, Color color)
        {
            var parts = LedgerUi.SplitGoldCompact(scaled);
            long[] values = { parts.Gold, parts.Silver, parts.Copper };
            Sprite[] sprites = { LedgerUi.GoldCoin(), LedgerUi.SilverCoin(), LedgerUi.CopperCoin() };
            bool[] shown = { parts.Gold != 0, parts.Silver != 0 || parts.Gold != 0, parts.Gold == 0 };
            int first = shown[0] ? 0 : shown[1] ? 1 : 2;
            float total = 0;
            for (int i = 0; i < coinText.Length; i++)
            {
                coinText[i].gameObject.SetActive(shown[i]);
                coinIcon[i].gameObject.SetActive(shown[i]);
                if (!shown[i]) continue;
                coinText[i].text = (i == first ? scaled < 0 ? "-" : net && scaled > 0 ? "+" : "" : "") + values[i].ToString("#,0", CultureInfo.InvariantCulture);
                coinText[i].color = color;
                coinIcon[i].sprite = sprites[i];
                total += coinText[i].GetPreferredValues().x + 24;
            }
            float scale = Mathf.Min(1, (LedgerPanel.CellWidth - 4) / Mathf.Max(1, total));
            LedgerUi.Place(coins, LedgerPanel.CellWidth - 4 - total * scale, (LedgerPanel.RowHeight - 1) * .5f * (1 - scale), total, LedgerPanel.RowHeight - 1);
            coins.localScale = Vector3.one * scale;
            float cursor = 0;
            for (int i = 0; i < coinText.Length; i++)
            {
                if (!shown[i]) continue;
                float width = coinText[i].GetPreferredValues().x;
                LedgerUi.Place(coinText[i].rectTransform, cursor, 0, width, LedgerPanel.RowHeight - 1);
                LedgerUi.Place(coinIcon[i].rectTransform, cursor + width + 2, 10, 16, 16);
                cursor += width + 24;
            }
        }
    }
}
