using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Source.Items;
using Assets.Source.Player;
using Assets.Source.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerUi
    {
        static readonly string[] TierFirstItems = {
            "iron_ore", "linen_cloth", "conscript", "wood_log", "healing_potion",
            "mercurial_flux", "cattle", "ancient_gem", "soothing_herbs", "widget",
            "prayer_beads", "fragmented_nightmare"
        };
        internal static readonly Color Paper = new Color(1, .94f, .79f), Muted = new Color(.72f, .67f, .57f),
            Gold = new Color(.94f, .73f, .38f), Gain = new Color(.62f, .85f, .57f), Loss = new Color(.96f, .52f, .44f);
        readonly TMP_Text font;
        readonly Image buttonStyle;
        readonly Sprite frame;
        internal LedgerUi(Button nativeButton)
        {
            font = nativeButton.GetComponentInChildren<TMP_Text>(true);
            buttonStyle = nativeButton.GetComponent<Image>();
            frame = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(s => s.name == "DefaultBorderDark");
        }
        internal static RectTransform Rect(Transform parent, string name)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false); return rect;
        }
        internal static void Fill(RectTransform rect, float left = 0, float top = 0, float right = 0, float bottom = 0)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top); }
        internal static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }
        internal Image Background(RectTransform root, Color color, bool framed = false)
        {
            var image = root.gameObject.AddComponent<Image>(); image.color = color;
            if (framed && frame) { image.sprite = frame; image.type = Image.Type.Sliced; image.color = Color.white; }
            return image;
        }
        internal TMP_Text Text(Transform parent, string name, string value, float size = 19)
        {
            var rect = Rect(parent, name); Fill(rect);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font.font; text.fontSharedMaterial = font.fontSharedMaterial; text.color = Paper;
            text.fontSize = size; text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis; text.alignment = TextAlignmentOptions.MidlineLeft;
            text.richText = false; text.raycastTarget = false; text.text = value; return text;
        }
        internal Button Button(Transform parent, string name, string value, Action clicked, float fontSize = 19)
        {
            var rect = Rect(parent, name);
            var image = Background(rect, buttonStyle.color);
            image.sprite = buttonStyle.sprite; image.type = buttonStyle.type; image.pixelsPerUnitMultiplier = buttonStyle.pixelsPerUnitMultiplier;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => { Assets.Behaviour.UI.UISounds.Button(); clicked(); });
            var label = Text(rect, "Label", value, fontSize); label.alignment = TextAlignmentOptions.Center; return button;
        }
        internal static string CurrencyName(Currency currency)
        {
            switch (currency) { case Currency.Medal: return "Medals"; case Currency.Ring: return "Rings"; case Currency.Crown: return "Crowns"; case Currency.Ducat: return "Ducats"; default: return currency.ToString(); }
        }
        internal static string ItemName(ItemType item)
        {
            if (item == null) return "";
            string translated = Translation.Translate(item.GetDisplayName());
            return string.IsNullOrEmpty(translated) || translated.StartsWith("@") ? item.Identifier : translated;
        }
        internal static int ItemTier(ItemType item)
        {
            if (item == null) return int.MaxValue;
            int tier = 1;
            for (int i = 1; i < TierFirstItems.Length; i++)
            {
                var boundary = ItemType.All.FirstOrDefault(t => t.Identifier == TierFirstItems[i]);
                if (boundary != null && item.Ordinal >= boundary.Ordinal) tier = i + 1;
            }
            return tier;
        }
        internal static IEnumerable<ItemType> OrderedUnlockedItems()
        {
            return ItemType.Unlocked
                .Where(i => i.Identifier != "dummy_passthrough")
                .OrderBy(ItemTier)
                .ThenBy(ItemName, StringComparer.CurrentCultureIgnoreCase);
        }
        internal static string Rate(long value, Currency currency)
        {
            decimal amount = currency == Currency.Gold ? (decimal)value / 10000 : value;
            amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
            return (amount > 0 ? "+" : "") + amount.ToString("0.00", CultureInfo.InvariantCulture)
                + (currency == Currency.Gold ? " g/min" : "/min");
        }
        internal static string ItemRate(long value)
        {
            decimal amount = decimal.Round((decimal)value, 2, MidpointRounding.AwayFromZero);
            return (amount > 0 ? "+" : "") + amount.ToString("0.00", CultureInfo.InvariantCulture) + "/min";
        }
    }
}
