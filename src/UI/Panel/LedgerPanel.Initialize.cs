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
    public sealed partial class LedgerPanel : MonoBehaviour
    {
        void Initialize(LedgerBook book, Button anchor, RectTransform root)
        {
            this.book = book; this.anchor = anchor; overlay = root; ui = new LedgerUi(anchor);
            panel = LedgerUi.Rect(root, "GlobalLedgerPanel"); ui.Background(panel, new Color(.16f, .09f, .09f), true);
            panel.gameObject.AddComponent<UIBlocksZoom>();
            var title = ui.Text(panel, "Title", "GLOBAL LEDGER", 24); LedgerUi.Place(title.rectTransform, 24, 13, 500, 34); title.color = LedgerUi.Gold;
            var close = ui.Button(panel, "Close", "X", ClosePopup);
            close.GetComponent<RectTransform>().anchorMin = close.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
            close.GetComponent<RectTransform>().pivot = new Vector2(1, 1); close.GetComponent<RectTransform>().anchoredPosition = new Vector2(-16, -14); close.GetComponent<RectTransform>().sizeDelta = new Vector2(36, 32);

            rollingButton = ui.Button(panel, "RollingToggle", "Rolling Avg", ToggleRollingAverage, 14);
            var rollingRect = (RectTransform)rollingButton.transform;
            rollingRect.anchorMin = rollingRect.anchorMax = new Vector2(1, 1);
            rollingRect.pivot = new Vector2(1, 1);
            rollingRect.anchoredPosition = new Vector2(-180, -14);
            rollingRect.sizeDelta = new Vector2(92, 32);

            basicButton = ui.Button(panel, "BasicMode", "Basic", () => SetViewMode(ViewMode.Basic));
            var basicRect = (RectTransform)basicButton.transform;
            basicRect.anchorMin = basicRect.anchorMax = new Vector2(1, 1);
            basicRect.pivot = new Vector2(1, 1);
            basicRect.anchoredPosition = new Vector2(-118, -14);
            basicRect.sizeDelta = new Vector2(56, 32);

            advButton = ui.Button(panel, "AdvMode", "Adv", () => SetViewMode(ViewMode.Adv));
            var advRect = (RectTransform)advButton.transform;
            advRect.anchorMin = advRect.anchorMax = new Vector2(1, 1);
            advRect.pivot = new Vector2(1, 1);
            advRect.anchoredPosition = new Vector2(-58, -14);
            advRect.sizeDelta = new Vector2(54, 32);

            secButton = ui.Button(panel, "UnitSec", "sec", () => SetTimeUnit(TimeUnit.Sec));
            minButton = ui.Button(panel, "UnitMin", "min", () => SetTimeUnit(TimeUnit.Min));
            hourButton = ui.Button(panel, "UnitHour", "hour", () => SetTimeUnit(TimeUnit.Hour));

            currencies = ui.Button(panel, "CurrenciesTab", "Currencies", () => SelectTab(0)); LedgerUi.Place((RectTransform)currencies.transform, 24, 56, 155, 38);
            production = ui.Button(panel, "ProductionTab", "Production", () => SelectTab(1)); LedgerUi.Place((RectTransform)production.transform, 187, 56, 155, 38);
            showAllButton = ui.Button(panel, "ShowAll", "Show All", ToggleShowAll); LedgerUi.Place((RectTransform)showAllButton.transform, 518, 56, 155, 38);
            collapseAll = ui.Button(panel, "CollapseAll", "Collapse All", CollapseAll); LedgerUi.Place((RectTransform)collapseAll.transform, 681, 56, 155, 38);
            status = ui.Text(panel, "Window", "", 15); LedgerUi.Place(status.rectTransform, 24, 101, 740, 25); status.color = LedgerUi.Muted;
            viewport = LedgerUi.Rect(panel, "TreeViewport"); LedgerUi.Fill(viewport, 20, 136, 24, 62);
            ui.Background(viewport, new Color(0, 0, 0, .15f)); viewport.gameObject.AddComponent<RectMask2D>();
            content = LedgerUi.Rect(viewport, "TreeContent");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0, 1);
            content.sizeDelta = Vector2.zero;
            scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.content = content; scroll.viewport = viewport;
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 32;
            viewport.offsetMax = new Vector2(-40, -136);
            var track = LedgerUi.Rect(panel, "ScrollTrack");
            track.anchorMin = new Vector2(1, 0); track.anchorMax = Vector2.one; track.pivot = new Vector2(1, .5f);
            track.offsetMin = new Vector2(-33, 62); track.offsetMax = new Vector2(-23, -136);
            ui.Background(track, new Color(0, 0, 0, .28f));
            var handle = LedgerUi.Rect(track, "ScrollHandle"); LedgerUi.Fill(handle);
            var handleImage = ui.Background(handle, new Color(.65f, .48f, .29f, .85f));
            var scrollbar = track.gameObject.AddComponent<Scrollbar>(); scrollbar.handleRect = handle; scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop; scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
            scroll.verticalScrollbar = scrollbar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            footer = ui.Text(panel, "Footnote", "", 13); LedgerUi.Fill(footer.rectTransform, 24, 0, 24, 15);
            footer.rectTransform.anchorMin = footer.rectTransform.anchorMax = new Vector2(0, 0);
            footer.rectTransform.pivot = Vector2.zero;
            footer.rectTransform.sizeDelta = new Vector2(740, 38); footer.rectTransform.anchoredPosition = new Vector2(24, 14);
            footer.textWrappingMode = TextWrappingModes.Normal; footer.color = LedgerUi.Muted;
            Fit();
        }

    }
}
