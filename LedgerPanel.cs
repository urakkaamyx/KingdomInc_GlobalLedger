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
    public sealed class LedgerPanel : MonoBehaviour
    {
        public enum ViewMode { Basic, Adv }
        public enum TimeUnit { Sec, Min, Hour }

        public Action OnPopupClosed;
        LedgerBook book;
        LedgerUi ui;
        Button anchor;
        RectTransform overlay, panel, content, viewport;
        TMP_Text status, footer;
        Button currencies, production, showAllButton, collapseAll;
        Button basicButton, advButton, secButton, minButton, hourButton, rollingButton;
        ScrollRect scroll;
        readonly Vector3[] anchorCorners = new Vector3[4];
        readonly HashSet<string> expanded = new HashSet<string>();
        readonly List<TreeRow> rows = new List<TreeRow>();
        readonly LedgerLocations locations = new LedgerLocations();
        internal readonly List<RowData> VisibleRows = new List<RowData>();
        float nextRefresh, width, height;
        int tab;
        bool showAll;
        ViewMode viewMode = ViewMode.Basic;
        TimeUnit timeUnit = TimeUnit.Min;
        int divisor = 1;
        int windowSeconds = 60;
        internal int SelectedTab => tab;
        internal bool ShowingAll => showAll;
        internal ViewMode Mode => viewMode;
        internal TimeUnit Unit => timeUnit;
        internal int Divisor => divisor;
        internal int WindowSeconds => windowSeconds;
        internal string StatusText => status != null ? status.text : "";
        internal RectTransform Frame => panel;
        internal ScrollRect Scroller => scroll;
        internal Button CurrenciesButton => currencies;
        internal Button ProductionButton => production;
        internal Button ShowAllButton => showAllButton;
        internal Button CollapseAllButton => collapseAll;
        internal Button BasicButton => basicButton;
        internal Button AdvButton => advButton;
        internal Button SecButton => secButton;
        internal Button MinButton => minButton;
        internal Button HourButton => hourButton;
        internal Button RollingButton => rollingButton;

        internal static LedgerPanel Create(LedgerBook book, Button anchor)
        {
            // Foundation owns the bar slot. This popup has its own native popup lifecycle
            // outside the clipped slot and is destroyed with the Foundation view.
            var canvas = anchor.GetComponentInParent<Canvas>().rootCanvas;
            var root = LedgerUi.Rect(canvas.transform, "GlobalLedgerDropdown");
            LedgerUi.Fill(root); root.gameObject.SetActive(false);
            var result = root.gameObject.AddComponent<LedgerPanel>();
            try { result.Initialize(book, anchor, root); return result; }
            catch { Destroy(root.gameObject); throw; }
        }
        public void OpenPopup()
        {
            if (gameObject.activeSelf) return;
            if (UISounds.Instance) UISounds.WindowOpen();
            gameObject.SetActive(true);
            RefreshNow();
        }
        public void ClosePopup() => ClosePopup(true);
        public void ClosePopup(bool playSound)
        {
            if (!gameObject.activeSelf) return;
            if (playSound && UISounds.Instance) UISounds.WindowClose();
            gameObject.SetActive(false);
            OnPopupClosed?.Invoke();
        }
        public void TogglePopup()
        {
            if (gameObject.activeSelf) ClosePopup();
            else OpenPopup();
        }
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
        void OnEnable() { if (book != null) { transform.SetAsLastSibling(); Fit(); RefreshNow(); } }
        void Update()
        {
            var bar = anchor ? anchor.transform.parent?.parent?.parent as RectTransform : null;
            if (!anchor || !anchor.gameObject.activeInHierarchy || !anchor.GetComponentInParent<Canvas>().enabled || (bar && bar.anchoredPosition.y > 0.5f))
            { if (GameUI.Instance) ClosePopup(); return; }
            if (transform.GetSiblingIndex() != transform.parent.childCount - 1) transform.SetAsLastSibling();
            Fit();
            if (Time.unscaledTime >= nextRefresh) RefreshNow();
        }
        void Fit()
        {
            if (!anchor) return;
            ((RectTransform)anchor.transform).GetWorldCorners(anchorCorners);
            Vector2 point = overlay.InverseTransformPoint(anchorCorners[0]);
            float fullWidth = overlay.rect.width, fullHeight = overlay.rect.height;
            width = Mathf.Min(860, fullWidth - 20);
            float x = Mathf.Clamp(point.x - overlay.rect.xMin, 10, Mathf.Max(10, fullWidth - width - 10));
            float top = overlay.rect.yMax - point.y + 8;
            height = Mathf.Min(670, fullHeight - top - 16);
            // At extreme UI scales fit the complete popup into the visible canvas.
            if (height < 280) { top = Mathf.Min(top, Mathf.Max(10, fullHeight - 296)); height = Mathf.Min(670, fullHeight - top - 16); }
            LedgerUi.Place(panel, x, top, width, height);
            footer.rectTransform.sizeDelta = new Vector2(width - 48, 38);
            status.rectTransform.sizeDelta = new Vector2(width - 48, 25);
            float tabWidth = Mathf.Min(155, (width - 72) / 4);
            bool isAdv = viewMode == ViewMode.Adv;
            currencies.gameObject.SetActive(isAdv);
            production.gameObject.SetActive(isAdv);
            collapseAll.gameObject.SetActive(isAdv);
            if (isAdv)
            {
                LedgerUi.Place((RectTransform)currencies.transform, 24, 56, tabWidth, 38);
                LedgerUi.Place((RectTransform)production.transform, 32 + tabWidth, 56, tabWidth, 38);
                LedgerUi.Place((RectTransform)showAllButton.transform, width - 32 - tabWidth * 2, 56, tabWidth, 38);
                LedgerUi.Place((RectTransform)collapseAll.transform, width - 24 - tabWidth, 56, tabWidth, 38);
            }
            else
            {
                LedgerUi.Place((RectTransform)showAllButton.transform, width - 24 - tabWidth, 56, tabWidth, 38);
            }

            float unitW = 50;
            float unitX = (width - (unitW * 3 + 8)) / 2;
            LedgerUi.Place((RectTransform)secButton.transform, unitX, 59, unitW, 32);
            LedgerUi.Place((RectTransform)minButton.transform, unitX + unitW + 4, 59, unitW, 32);
            LedgerUi.Place((RectTransform)hourButton.transform, unitX + (unitW + 4) * 2, 59, unitW + 4, 32);
            for (int i = 0; i < rows.Count; i++) rows[i].Layout(width - 60);
        }
        internal void SetViewMode(ViewMode mode)
        {
            if (viewMode == mode) return;
            viewMode = mode;
            Fit();
            scroll.StopMovement();
            content.anchoredPosition = Vector2.zero;
            RefreshNow();
        }
        internal void SetTimeUnit(TimeUnit unit)
        {
            if (timeUnit == unit) return;
            timeUnit = unit;
            RefreshNow();
        }
        internal void ResetTree() { expanded.Clear(); showAll = false; locations.Reset(); scroll.verticalNormalizedPosition = 1; }
        internal void ToggleShowAll()
        {
            showAll = !showAll;
            RefreshNow();
        }
        internal void ToggleRollingAverage()
        {
            if (GlobalLedgerPlugin.Instance?.EnableRollingAverage != null)
            {
                GlobalLedgerPlugin.Instance.EnableRollingAverage.Value = !GlobalLedgerPlugin.Instance.EnableRollingAverage.Value;
            }
            RefreshNow();
        }
        internal void CollapseAll()
        {
            expanded.Clear();
            scroll.StopMovement();
            content.anchoredPosition = Vector2.zero;
            RefreshNow();
        }
        internal void SelectTab(int value)
        {
            tab = value; scroll.StopMovement(); content.anchoredPosition = Vector2.zero; RefreshNow();
        }
        internal void ToggleRow(string key)
        {
            if (!expanded.Add(key)) expanded.Remove(key);
            RefreshNow();
        }
        void Activate(RowData row)
        {
            if (row.Expandable) ToggleRow(row.Key);
        }
        void View(RowData row)
        {
            if (row?.Target != null) GlobalLedgerPlugin.Instance.Navigation.Focus(row.Target);
        }
        internal void RefreshNow()
        {
            nextRefresh = Time.unscaledTime + .5f;
            VisibleRows.Clear();
            basicButton.GetComponentInChildren<TMP_Text>().color = viewMode == ViewMode.Basic ? LedgerUi.Gold : LedgerUi.Paper;
            advButton.GetComponentInChildren<TMP_Text>().color = viewMode == ViewMode.Adv ? LedgerUi.Gold : LedgerUi.Paper;
            secButton.GetComponentInChildren<TMP_Text>().color = timeUnit == TimeUnit.Sec ? LedgerUi.Gold : LedgerUi.Paper;
            minButton.GetComponentInChildren<TMP_Text>().color = timeUnit == TimeUnit.Min ? LedgerUi.Gold : LedgerUi.Paper;
            hourButton.GetComponentInChildren<TMP_Text>().color = timeUnit == TimeUnit.Hour ? LedgerUi.Gold : LedgerUi.Paper;

            currencies.GetComponentInChildren<TMP_Text>().color = tab == 0 ? LedgerUi.Gold : LedgerUi.Paper;
            production.GetComponentInChildren<TMP_Text>().color = tab == 1 ? LedgerUi.Gold : LedgerUi.Paper;
            showAllButton.GetComponentInChildren<TMP_Text>().color = showAll ? LedgerUi.Gold : LedgerUi.Paper;

            bool rolling = GlobalLedgerPlugin.Instance?.EnableRollingAverage?.Value == true;
            rollingButton.GetComponentInChildren<TMP_Text>().color = rolling ? LedgerUi.Gold : LedgerUi.Paper;
            int entries = GlobalLedgerPlugin.Instance?.RollingAverageEntries != null ? Mathf.Clamp(GlobalLedgerPlugin.Instance.RollingAverageEntries.Value, 2, 60) : 3;
            divisor = rolling ? entries : 1;

            if (rolling)
            {
                if (timeUnit == TimeUnit.Sec) windowSeconds = entries;
                else windowSeconds = entries * 60;
            }
            else
            {
                windowSeconds = 60;
            }

            string rateUnitStr = timeUnit == TimeUnit.Sec ? "second" : timeUnit == TimeUnit.Min ? "minute" : "hour";
            string modeDesc = rolling
                ? "Rolling " + entries + (timeUnit == TimeUnit.Sec ? "s" : "m") + " average (Rates: per " + rateUnitStr + ")"
                : "Last 60 seconds (Rates: per " + rateUnitStr + ")";
            int elapsedSec = (int)Math.Floor(book.Elapsed);
            status.text = "All workspaces  |  " + modeDesc + (elapsedSec < windowSeconds ? "  |  Collecting " + elapsedSec + "/" + windowSeconds + "s" : "");

            if (viewMode == ViewMode.Basic)
            {
                footer.text = "Basic mode: Net collection summary across all workspaces. Click Adv for container and building breakdowns.";
                BuildBasic();
            }
            else if (tab == 0)
            {
                footer.text = "Hover to highlight. Click a row to expand/collapse; click View to center it.\nBuilding activity; currencies stay separate. Session history only.";
                BuildCurrencies();
            }
            else
            {
                footer.text = "Hover to highlight. Click a row to expand/collapse; click View to center it.\nBuilding production; items stay separate. Session history only.";
                BuildProduction();
            }
            for (int i = 0; i < VisibleRows.Count; i++)
            {
                while (rows.Count <= i) rows.Add(new TreeRow(this, ui, content, rows.Count));
                rows[i].Bind(VisibleRows[i], width - 60);
            }
            for (int i = VisibleRows.Count; i < rows.Count; i++) rows[i].Root.gameObject.SetActive(false);
            content.sizeDelta = new Vector2(0, VisibleRows.Count * 36 + 8);
        }
        void BuildBasic()
        {
            if (GamePlayer.Current == null) return;
            var currentTotals = book.Snapshot(windowSeconds);

            // Section 1: CURRENCIES
            VisibleRows.Add(new RowData("basic/header/currencies", "CURRENCY NET COLLECTION", 0) { IsHeader = true });
            int currencyCount = 0;
            foreach (Currency currency in Enum.GetValues(typeof(Currency)))
            {
                if (!currency.IsUnlocked()) continue;
                int c = (int)currency;
                bool hasActivity = book.HasCurrencyActivity(c);
                bool isActiveInWindow = currentTotals.Any(t => t.Active(c));
                double timeSinceActivity = book.TimeSinceCurrencyActivity(c);

                if (!showAll)
                {
                    if (currency != Currency.Gold && !hasActivity) continue;
                    if (!isActiveInWindow && timeSinceActivity > windowSeconds + 60.0 && currency != Currency.Gold) continue;
                }

                long net = currentTotals.Where(t => t.Active(c)).Sum(t => t.Net(c));
                var row = new RowData("basic/currency/" + c, LedgerUi.CurrencyName(currency), 1)
                {
                    Currency = currency,
                    Amount = net,
                    Expandable = false,
                    Icon = currency.GetIcon()
                };
                VisibleRows.Add(row);
                currencyCount++;
            }
            if (currencyCount == 0 && !showAll)
            {
                VisibleRows.Add(new RowData("basic/currency/empty", "No observed currency activity in the recent window. Click Show All to list unlocked currencies.", 1));
            }

            // Section 2: PRODUCTION
            VisibleRows.Add(new RowData("basic/header/production", "ITEM NET PRODUCTION", 0) { IsHeader = true });
            var items = LedgerUi.OrderedUnlockedItems();
            int count = 0;
            foreach (var item in items)
            {
                string id = item.Identifier;
                bool hasActivity = book.HasItemActivity(id);
                bool isActiveInWindow = currentTotals.Any(t => t.ItemActive(id));
                double timeSinceActivity = book.TimeSinceItemActivity(id);

                if (!showAll)
                {
                    // 1. If something hasnt been made DO NOT SHOW IT
                    if (!hasActivity) continue;

                    // 2. If somethings qty hasnt been greater than 0 within the selected time,
                    // and it has been more than a rolling minute (60s) since it was last made, DO NOT SHOW IT.
                    // 3. If something has been made, and value keeps changing: SHOW IT (isActiveInWindow == true).
                    // 4. If something has been made recently, but drops to 0: SHOW IT FOR A ROLLING MINUTE (timeSinceActivity <= windowSeconds + 60.0).
                    if (!isActiveInWindow && timeSinceActivity > windowSeconds + 60.0) continue;
                }

                long net = currentTotals.Where(t => t.ItemActive(id)).Sum(t => t.ItemNet(id));
                var row = new RowData("basic/item/" + id, LedgerUi.ItemName(item), 1)
                {
                    Item = item,
                    Amount = net,
                    Expandable = false,
                    Icon = item.GetIcon()
                };
                VisibleRows.Add(row);
                count++;
            }
            if (count == 0 && !showAll)
            {
                VisibleRows.Add(new RowData("basic/item/empty", "No observed production in the recent window. Click Show All to list unlocked items.", 1));
            }
        }
        void BuildCurrencies()
        {
            if (GamePlayer.Current == null) return;
            var data = locations.Resolve(book.Snapshot(windowSeconds));
            foreach (Currency currency in Enum.GetValues(typeof(Currency)))
            {
                if (!currency.IsUnlocked()) continue;
                int c = (int)currency;
                string root = "currency/" + c;
                var active = data.Where(t => t.Totals.Active(c)).ToArray();
                Add(root, LedgerUi.CurrencyName(currency) + " - Net Earnings", 0, currency, active.Sum(t => t.Totals.Net(c)), true);
                if (!expanded.Contains(root)) continue;
                if (active.Length == 0) { VisibleRows.Add(new RowData(root + "/empty", "No observed activity in the recent window.", 1)); continue; }
                foreach (var group in active.GroupBy(t => t.GroupKey).OrderBy(g => g.First().GroupName, StringComparer.CurrentCulture).ThenBy(g => g.Key, StringComparer.Ordinal))
                {
                    string groupKey = root + "/" + group.Key;
                    var container = group.First();
                    var containerRow = Add(groupKey, container.GroupName, 1, currency, group.Sum(t => t.Totals.Net(c)), true);
                    containerRow.PrefixIcon = container.GroupTarget != null ? LedgerLocations.ContainerIcon() : null;
                    containerRow.Icon = container.GroupIcon; containerRow.Target = container.GroupTarget;
                    if (!expanded.Contains(groupKey)) continue;
                    foreach (var entry in group.OrderBy(t => t.BuildingName, StringComparer.CurrentCulture).ThenBy(t => t.Totals.Source.Id, StringComparer.Ordinal))
                    {
                        var building = entry.Totals;
                        string buildingKey = groupKey + "/" + building.Source.Id;
                        var buildingRow = Add(buildingKey, entry.BuildingName, 2, currency, building.Net(c), true);
                        buildingRow.Icon = entry.BuildingIcon; buildingRow.Target = entry.BuildingTarget;
                        if (!expanded.Contains(buildingKey)) continue;
                        string costs = buildingKey + "/costs";
                        VisibleRows.Add(new RowData(costs, "Operating Costs", 3) { Expandable = true, Open = expanded.Contains(costs) });
                        if (!expanded.Contains(costs)) continue;
                        bool any = false;
                        foreach (Currency costCurrency in Enum.GetValues(typeof(Currency)))
                            if (building.Spent[(int)costCurrency] != 0 && costCurrency.IsUnlocked())
                            {
                                var cost = Add(costs + "/" + (int)costCurrency, LedgerUi.CurrencyName(costCurrency), 4, costCurrency, -building.Spent[(int)costCurrency], false);
                                cost.Icon = costCurrency.GetIcon(); cost.IconOnly = true; any = true;
                            }
                        if (!any) VisibleRows.Add(new RowData(costs + "/empty", "No observed operating costs", 4));
                    }
                }
            }
        }
        void BuildProduction()
        {
            if (GamePlayer.Current == null) return;
            var data = locations.Resolve(book.Snapshot(windowSeconds));
            foreach (var item in LedgerUi.OrderedUnlockedItems())
            {
                string id = item.Identifier;
                string root = "item/" + id;
                var active = data.Where(t => t.Totals.ItemActive(id)).ToArray();
                if (!showAll && active.Length == 0) continue;
                AddItem(root, LedgerUi.ItemName(item) + " - Net Production", 0, item, active.Sum(t => t.Totals.ItemNet(id)), true);
                if (!expanded.Contains(root)) continue;
                if (active.Length == 0) { VisibleRows.Add(new RowData(root + "/empty", "No observed activity in the recent window.", 1)); continue; }
                foreach (var group in active.GroupBy(t => t.GroupKey).OrderBy(g => g.First().GroupName, StringComparer.CurrentCulture).ThenBy(g => g.Key, StringComparer.Ordinal))
                {
                    string groupKey = root + "/" + group.Key;
                    var container = group.First();
                    var containerRow = AddItem(groupKey, container.GroupName, 1, item, group.Sum(t => t.Totals.ItemNet(id)), true);
                    containerRow.PrefixIcon = container.GroupTarget != null ? LedgerLocations.ContainerIcon() : null;
                    containerRow.Icon = container.GroupIcon; containerRow.Target = container.GroupTarget;
                    if (!expanded.Contains(groupKey)) continue;
                    foreach (var entry in group.OrderBy(t => t.BuildingName, StringComparer.CurrentCulture).ThenBy(t => t.Totals.Source.Id, StringComparer.Ordinal))
                    {
                        var building = entry.Totals;
                        string buildingKey = groupKey + "/" + building.Source.Id;
                        var buildingRow = AddItem(buildingKey, entry.BuildingName, 2, item, building.ItemNet(id), true);
                        buildingRow.Icon = entry.BuildingIcon; buildingRow.Target = entry.BuildingTarget;
                        if (!expanded.Contains(buildingKey)) continue;
                        string costs = buildingKey + "/costs";
                        VisibleRows.Add(new RowData(costs, "Operating Costs", 3) { Expandable = true, Open = expanded.Contains(costs) });
                        if (!expanded.Contains(costs)) continue;
                        bool any = false;
                        foreach (var costItem in LedgerUi.OrderedUnlockedItems())
                        {
                            long spentItem = building.ItemConsumed(costItem.Identifier);
                            if (spentItem != 0)
                            {
                                var cost = AddItem(costs + "/item/" + costItem.Identifier, LedgerUi.ItemName(costItem), 4, costItem, -spentItem, false);
                                cost.Icon = costItem.GetIcon(); cost.IconOnly = true; any = true;
                            }
                        }
                        foreach (Currency costCurrency in Enum.GetValues(typeof(Currency)))
                            if (building.Spent[(int)costCurrency] != 0 && costCurrency.IsUnlocked())
                            {
                                var cost = Add(costs + "/currency/" + (int)costCurrency, LedgerUi.CurrencyName(costCurrency), 4, costCurrency, -building.Spent[(int)costCurrency], false);
                                cost.Icon = costCurrency.GetIcon(); cost.IconOnly = true; any = true;
                            }
                        if (!any) VisibleRows.Add(new RowData(costs + "/empty", "No observed operating costs", 4));
                    }
                }
            }
            if (VisibleRows.Count == 0)
                VisibleRows.Add(new RowData("production/empty", "No observed production in the last minute. Click Show All to list unlocked items.", 0));
        }
        RowData Add(string key, string label, int depth, Currency currency, long amount, bool expandable)
        {
            var row = new RowData(key, label, depth) { Currency = currency, Amount = amount, Expandable = expandable, Open = expanded.Contains(key), Icon = depth == 0 ? currency.GetIcon() : null };
            VisibleRows.Add(row); return row;
        }
        RowData AddItem(string key, string label, int depth, ItemType item, long amount, bool expandable)
        {
            var row = new RowData(key, label, depth) { Item = item, Amount = amount, Expandable = expandable, Open = expanded.Contains(key), Icon = depth == 0 ? item.GetIcon() : null };
            VisibleRows.Add(row); return row;
        }
        internal sealed class RowData
        {
            internal readonly string Key, Label;
            internal readonly int Depth;
            internal Currency? Currency;
            internal ItemType Item;
            internal long Amount;
            internal bool Expandable, Open;
            internal bool IconOnly, IsHeader;
            internal Sprite PrefixIcon, Icon;
            internal LedgerTarget Target;
            internal RowData(string key, string label, int depth) { Key = key; Label = label; Depth = depth; }
        }
        internal sealed class TreeRow
        {
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
            RowData data;
            internal TreeRow(LedgerPanel owner, LedgerUi ui, Transform parent, int index)
            {
                this.owner = owner;
                this.index = index;
                Root = LedgerUi.Rect(parent, "TreeRow" + index);
                shade = ui.Background(Root, Color.clear);
                Button = Root.gameObject.AddComponent<Button>(); Button.targetGraphic = shade;
                Button.navigation = new Navigation { mode = Navigation.Mode.None };
                Button.onClick.AddListener(() => {
                    if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
                    if (data != null) { UISounds.Button(); owner.Activate(data); }
                });
                hover = Root.gameObject.AddComponent<LedgerRowHover>();
                var arrowRect = LedgerUi.Rect(Root, "Expander");
                var arrowHit = ui.Background(arrowRect, Color.clear);
                expander = arrowRect.gameObject.AddComponent<Button>(); expander.targetGraphic = arrowHit;
                expander.navigation = new Navigation { mode = Navigation.Mode.None };
                expander.onClick.AddListener(() => {
                    if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
                    if (data?.Expandable == true) { UISounds.Button(); owner.ToggleRow(data.Key); }
                });
                arrow = ui.Text(arrowRect, "Arrow", "", 17);
                prefixIcon = LedgerUi.Rect(Root, "ContainerIcon").gameObject.AddComponent<Image>(); prefixIcon.preserveAspect = true; prefixIcon.raycastTarget = false;
                separator = ui.Text(Root, "IconSeparator", "", 17); separator.alignment = TextAlignmentOptions.Center; separator.color = LedgerUi.Muted;
                icon = LedgerUi.Rect(Root, "CurrencyIcon").gameObject.AddComponent<Image>(); icon.preserveAspect = true; icon.raycastTarget = false;
                label = ui.Text(Root, "Name", ""); amount = ui.Text(Root, "Rate", ""); amount.alignment = TextAlignmentOptions.MidlineRight;
                amount.enableAutoSizing = true; amount.fontSizeMin = 13; amount.fontSizeMax = 19;
                goldGroup = LedgerUi.Rect(Root, "GoldRate");
                goldText = CoinText(ui, goldGroup, "GoldAmount"); goldCoin = CoinIcon(goldGroup, "GoldCoin");
                silverText = CoinText(ui, goldGroup, "SilverAmount"); silverCoin = CoinIcon(goldGroup, "SilverCoin");
                copperText = CoinText(ui, goldGroup, "CopperAmount"); copperCoin = CoinIcon(goldGroup, "CopperCoin");
                goldUnit = ui.Text(goldGroup, "GoldUnit", "", 13); goldUnit.color = LedgerUi.Muted; goldUnit.alignment = TextAlignmentOptions.MidlineLeft;
                ViewButton = ui.Button(Root, "View", "View", () => { if (data?.Target != null) owner.View(data); }, 14);
            }
            static TMP_Text CoinText(LedgerUi ui, Transform parent, string name)
            {
                var text = ui.Text(parent, name, "", 18);
                text.alignment = TextAlignmentOptions.MidlineRight;
                text.overflowMode = TextOverflowModes.Overflow;
                return text;
            }
            static Image CoinIcon(Transform parent, string name)
            {
                var image = LedgerUi.Rect(parent, name).gameObject.AddComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                return image;
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
            internal void Bind(RowData value, float width)
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
}
