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
    internal sealed class LedgerUi
    {
        static readonly string[] TierFirstItems = {
            "iron_ore", "linen_cloth", "conscript", "wood_log", "healing_potion",
            "mercurial_flux", "cattle", "ancient_gem", "soothing_herbs", "widget",
            "prayer_beads", "fragmented_nightmare"
        };
        static Sprite silverCoin, copperCoin;
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
        internal static Sprite GoldCoin() => Currency.Gold.GetIcon();
        internal static Sprite SilverCoin() { ResolveCoins(); return silverCoin; }
        internal static Sprite CopperCoin() { ResolveCoins(); return copperCoin; }
        static void ResolveCoins()
        {
            if (silverCoin && copperCoin) return;
            foreach (var display in Resources.FindObjectsOfTypeAll<CurrencyDisplay>())
            {
                if (!silverCoin) silverCoin = AccessTools.Field(typeof(CurrencyDisplay), "_silverIcon")?.GetValue(display) as Sprite;
                if (!copperCoin) copperCoin = AccessTools.Field(typeof(CurrencyDisplay), "_copperIcon")?.GetValue(display) as Sprite;
                if (silverCoin && copperCoin) return;
            }
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
            button.onClick.AddListener(() => {
                if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
                Assets.Behaviour.UI.UISounds.Button();
                clicked();
            });
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
        internal static (string Sign, long Gold, long Silver, long Copper) SplitGold(long value)
        {
            string sign = value > 0 ? "+" : value < 0 ? "-" : "";
            long abs = Math.Abs(value);
            return (sign, abs / 10000, (abs / 100) % 100, abs % 100);
        }
        internal static string TimeUnitSuffix(LedgerPanel.TimeUnit unit)
        {
            switch (unit)
            {
                case LedgerPanel.TimeUnit.Sec: return "/sec";
                case LedgerPanel.TimeUnit.Hour: return "/hour";
                default: return "/min";
            }
        }
        internal static double ScaleRate(long value, LedgerPanel.TimeUnit unit)
        {
            switch (unit)
            {
                case LedgerPanel.TimeUnit.Sec: return value / 60.0;
                case LedgerPanel.TimeUnit.Hour: return value * 60.0;
                default: return value;
            }
        }
        internal static (string Sign, long Gold, long Silver, long Copper) SplitGoldScaled(long value, LedgerPanel.TimeUnit unit)
        {
            long scaled = unit == LedgerPanel.TimeUnit.Sec ? (long)Math.Round(value / 60.0) : unit == LedgerPanel.TimeUnit.Hour ? value * 60 : value;
            return SplitGold(scaled);
        }
        internal static string Rate(long value, Currency currency, LedgerPanel.TimeUnit unit = LedgerPanel.TimeUnit.Min)
        {
            if (currency == Currency.Gold)
            {
                var (sign, gold, silver, copper) = SplitGoldScaled(value, unit);
                string suffix = unit == LedgerPanel.TimeUnit.Min ? "" : " " + TimeUnitSuffix(unit);
                return sign + gold.ToString("#,0", CultureInfo.InvariantCulture) + "g " + silver + "s " + copper + "c" + suffix;
            }
            double amount = Math.Round(ScaleRate(value, unit), 2, MidpointRounding.AwayFromZero);
            return (amount > 0 ? "+" : "") + amount.ToString("0.00", CultureInfo.InvariantCulture) + TimeUnitSuffix(unit);
        }
        internal static string ItemRate(long value, LedgerPanel.TimeUnit unit = LedgerPanel.TimeUnit.Min)
        {
            double amount = Math.Round(ScaleRate(value, unit), 2, MidpointRounding.AwayFromZero);
            return (amount > 0 ? "+" : "") + amount.ToString("0.00", CultureInfo.InvariantCulture) + TimeUnitSuffix(unit);
        }
    }
}
