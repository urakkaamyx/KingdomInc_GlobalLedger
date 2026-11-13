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
    public sealed class LedgerPanel : MonoBehaviour
    {
        internal void Activate(LedgerRowData row)
        {
            if (row.Expandable)
                ToggleRow(row.Key);
        }

        LedgerRowData Add(string key, string label, int depth, GameCurrency currency, long amount, bool expandable)
        {
            var row = new LedgerRowData(key, label, depth)
            {
                GameCurrency = currency,
                Amount = amount,
                Expandable = expandable,
                Open = expanded.Contains(key),
                Icon = depth == 0 ? currency.GetIcon() : null
            };
            VisibleRows.Add(row);
            return row;
        }

        LedgerRowData AddItem(string key, string label, int depth, GameItemType item, long amount, bool expandable)
        {
            var row = new LedgerRowData(key, label, depth)
            {
                Item = item,
                Amount = amount,
                Expandable = expandable,
                Open = expanded.Contains(key),
                Icon = depth == 0 ? item.GetIcon() : null
            };
            VisibleRows.Add(row);
            return row;
        }

        void BuildBasic()
        {
            if (FoundationApi.Kingdom == null)
                return;
            var currentTotals = book.Snapshot(windowSeconds);
            // Section 1: CURRENCIES
            VisibleRows.Add(new LedgerRowData("basic/header/currencies", "CURRENCY NET COLLECTION", 0) { IsHeader = true });
            int currencyCount = 0;
            foreach (GameCurrency currency in Enum.GetValues(typeof(GameCurrency)))
            {
                if (!currency.IsUnlocked())
                    continue;
                int c = (int)currency;
                bool hasActivity = book.HasCurrencyActivity(c);
                bool isActiveInWindow = currentTotals.Any(t => t.Active(c));
                double timeSinceActivity = book.TimeSinceCurrencyActivity(c);
                if (!showAll)
                {
                    if (currency != GameCurrency.Gold && !hasActivity)
                        continue;
                    if (!isActiveInWindow && timeSinceActivity > windowSeconds + 60.0 && currency != GameCurrency.Gold)
                        continue;
                }

                long net = currentTotals.Where(t => t.Active(c)).Sum(t => t.Net(c));
                var row = new LedgerRowData("basic/currency/" + c, LedgerUi.CurrencyName(currency), 1)
                {
                    GameCurrency = currency,
                    Amount = net,
                    Expandable = false,
                    Icon = currency.GetIcon()
                };
                VisibleRows.Add(row);
                currencyCount++;
            }

            if (currencyCount == 0 && !showAll)
            {
                VisibleRows.Add(new LedgerRowData("basic/currency/empty", "No observed currency activity in the recent window. Click Show All to list unlocked currencies.", 1));
            }

            // Section 2: PRODUCTION
            VisibleRows.Add(new LedgerRowData("basic/header/production", "ITEM NET PRODUCTION", 0) { IsHeader = true });
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
                    if (!hasActivity)
                        continue;
                    // 2. If somethings qty hasnt been greater than 0 within the selected time,
                    // and it has been more than a rolling minute (60s) since it was last made, DO NOT SHOW IT.
                    // 3. If something has been made, and value keeps changing: SHOW IT (isActiveInWindow == true).
                    // 4. If something has been made recently, but drops to 0: SHOW IT FOR A ROLLING MINUTE (timeSinceActivity <= windowSeconds + 60.0).
                    if (!isActiveInWindow && timeSinceActivity > windowSeconds + 60.0)
                        continue;
                }

                long net = currentTotals.Where(t => t.ItemActive(id)).Sum(t => t.ItemNet(id));
                var row = new LedgerRowData("basic/item/" + id, LedgerUi.ItemName(item), 1)
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
                VisibleRows.Add(new LedgerRowData("basic/item/empty", "No observed production in the recent window. Click Show All to list unlocked items.", 1));
            }
        }

        void BuildCurrencies()
        {
            if (FoundationApi.Kingdom == null)
                return;
            var data = locations.Resolve(book.Snapshot(windowSeconds));
            foreach (GameCurrency currency in Enum.GetValues(typeof(GameCurrency)))
            {
                if (!currency.IsUnlocked())
                    continue;
                int c = (int)currency;
                string root = "currency/" + c;
                var active = data.Where(t => t.Totals.Active(c)).ToArray();
                Add(root, LedgerUi.CurrencyName(currency) + " - Net Earnings", 0, currency, active.Sum(t => t.Totals.Net(c)), true);
                if (!expanded.Contains(root))
                    continue;
                if (active.Length == 0)
                {
                    VisibleRows.Add(new LedgerRowData(root + "/empty", "No observed activity in the recent window.", 1));
                    continue;
                }

                foreach (var group in active.GroupBy(t => t.GroupKey).OrderBy(g => g.First().GroupName, StringComparer.CurrentCulture).ThenBy(g => g.Key, StringComparer.Ordinal))
                {
                    string groupKey = root + "/" + group.Key;
                    var container = group.First();
                    var containerRow = Add(groupKey, container.GroupName, 1, currency, group.Sum(t => t.Totals.Net(c)), true);
                    containerRow.PrefixIcon = container.GroupTarget != null ? LedgerLocations.ContainerIcon() : null;
                    containerRow.Icon = container.GroupIcon;
                    containerRow.Target = container.GroupTarget;
                    if (!expanded.Contains(groupKey))
                        continue;
                    foreach (var entry in group.OrderBy(t => t.BuildingName, StringComparer.CurrentCulture).ThenBy(t => t.Totals.Source.Id, StringComparer.Ordinal))
                    {
                        var building = entry.Totals;
                        string buildingKey = groupKey + "/" + building.Source.Id;
                        var buildingRow = Add(buildingKey, entry.BuildingName, 2, currency, building.Net(c), true);
                        buildingRow.Icon = entry.BuildingIcon;
                        buildingRow.Target = entry.BuildingTarget;
                        if (!expanded.Contains(buildingKey))
                            continue;
                        string costs = buildingKey + "/costs";
                        VisibleRows.Add(new LedgerRowData(costs, "Operating Costs", 3) { Expandable = true, Open = expanded.Contains(costs) });
                        if (!expanded.Contains(costs))
                            continue;
                        bool any = false;
                        foreach (GameCurrency costCurrency in Enum.GetValues(typeof(GameCurrency)))
                            if (building.Spent[(int)costCurrency] != 0 && costCurrency.IsUnlocked())
                            {
                                var cost = Add(costs + "/" + (int)costCurrency, LedgerUi.CurrencyName(costCurrency), 4, costCurrency, -building.Spent[(int)costCurrency], false);
                                cost.Icon = costCurrency.GetIcon();
                                cost.IconOnly = true;
                                any = true;
                            }

                        if (!any)
                            VisibleRows.Add(new LedgerRowData(costs + "/empty", "No observed operating costs", 4));
                    }
                }
            }
        }

        void BuildProduction()
        {
            if (FoundationApi.Kingdom == null)
                return;
            var data = locations.Resolve(book.Snapshot(windowSeconds));
            foreach (var item in LedgerUi.OrderedUnlockedItems())
            {
                string id = item.Identifier;
                string root = "item/" + id;
                var active = data.Where(t => t.Totals.ItemActive(id)).ToArray();
                if (!showAll && active.Length == 0)
                    continue;
                AddItem(root, LedgerUi.ItemName(item) + " - Net Production", 0, item, active.Sum(t => t.Totals.ItemNet(id)), true);
                if (!expanded.Contains(root))
                    continue;
                if (active.Length == 0)
                {
                    VisibleRows.Add(new LedgerRowData(root + "/empty", "No observed activity in the recent window.", 1));
                    continue;
                }

                foreach (var group in active.GroupBy(t => t.GroupKey).OrderBy(g => g.First().GroupName, StringComparer.CurrentCulture).ThenBy(g => g.Key, StringComparer.Ordinal))
                {
                    string groupKey = root + "/" + group.Key;
                    var container = group.First();
                    var containerRow = AddItem(groupKey, container.GroupName, 1, item, group.Sum(t => t.Totals.ItemNet(id)), true);
                    containerRow.PrefixIcon = container.GroupTarget != null ? LedgerLocations.ContainerIcon() : null;
                    containerRow.Icon = container.GroupIcon;
                    containerRow.Target = container.GroupTarget;
                    if (!expanded.Contains(groupKey))
                        continue;
                    foreach (var entry in group.OrderBy(t => t.BuildingName, StringComparer.CurrentCulture).ThenBy(t => t.Totals.Source.Id, StringComparer.Ordinal))
                    {
                        var building = entry.Totals;
                        string buildingKey = groupKey + "/" + building.Source.Id;
                        var buildingRow = AddItem(buildingKey, entry.BuildingName, 2, item, building.ItemNet(id), true);
                        buildingRow.Icon = entry.BuildingIcon;
                        buildingRow.Target = entry.BuildingTarget;
                        if (!expanded.Contains(buildingKey))
                            continue;
                        string costs = buildingKey + "/costs";
                        VisibleRows.Add(new LedgerRowData(costs, "Operating Costs", 3) { Expandable = true, Open = expanded.Contains(costs) });
                        if (!expanded.Contains(costs))
                            continue;
                        bool any = false;
                        foreach (var costItem in LedgerUi.OrderedUnlockedItems())
                        {
                            long spentItem = building.ItemConsumed(costItem.Identifier);
                            if (spentItem != 0)
                            {
                                var cost = AddItem(costs + "/item/" + costItem.Identifier, LedgerUi.ItemName(costItem), 4, costItem, -spentItem, false);
                                cost.Icon = costItem.GetIcon();
                                cost.IconOnly = true;
                                any = true;
                            }
                        }

                        foreach (GameCurrency costCurrency in Enum.GetValues(typeof(GameCurrency)))
                            if (building.Spent[(int)costCurrency] != 0 && costCurrency.IsUnlocked())
                            {
                                var cost = Add(costs + "/currency/" + (int)costCurrency, LedgerUi.CurrencyName(costCurrency), 4, costCurrency, -building.Spent[(int)costCurrency], false);
                                cost.Icon = costCurrency.GetIcon();
                                cost.IconOnly = true;
                                any = true;
                            }

                        if (!any)
                            VisibleRows.Add(new LedgerRowData(costs + "/empty", "No observed operating costs", 4));
                    }
                }
            }

            if (VisibleRows.Count == 0)
                VisibleRows.Add(new LedgerRowData("production/empty", "No observed production in the last minute. Click Show All to list unlocked items.", 0));
        }

        public void ClosePopup() => ClosePopup(true);
        public void ClosePopup(bool playSound)
        {
            if (!gameObject.activeSelf)
                return;
            if (playSound)
                FoundationApi.UiAccess.PlaySound(GameUiSound.WindowClose);
            gameObject.SetActive(false);
            OnPopupClosed?.Invoke();
        }

        internal void CollapseAll()
        {
            expanded.Clear();
            scroll.StopMovement();
            content.anchoredPosition = Vector2.zero;
            RefreshNow();
        }

        internal static LedgerPanel Create(LedgerBook book, Button anchor)
        {
            // Foundation owns the bar slot. This popup has its own native popup lifecycle
            // outside the clipped slot and is destroyed with the Foundation view.
            var canvas = anchor.GetComponentInParent<Canvas>().rootCanvas;
            var root = LedgerUi.Rect(canvas.transform, "GlobalLedgerDropdown");
            LedgerUi.Fill(root);
            root.gameObject.SetActive(false);
            var result = root.gameObject.AddComponent<LedgerPanel>();
            try
            {
                result.Initialize(book, anchor, root);
                return result;
            }
            catch
            {
                Destroy(root.gameObject);
                throw;
            }
        }

        public enum ViewMode
        {
            Basic,
            Adv
        }

        public enum TimeUnit
        {
            Sec,
            Min,
            Hour
        }

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
        readonly List<LedgerTreeRow> rows = new List<LedgerTreeRow>();
        readonly LedgerLocations locations = new LedgerLocations();
        internal readonly List<LedgerRowData> VisibleRows = new List<LedgerRowData>();
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

        void Fit()
        {
            if (!anchor)
                return;
            ((RectTransform)anchor.transform).GetWorldCorners(anchorCorners);
            Vector2 point = overlay.InverseTransformPoint(anchorCorners[0]);
            float fullWidth = overlay.rect.width, fullHeight = overlay.rect.height;
            width = Mathf.Min(860, fullWidth - 20);
            float x = Mathf.Clamp(point.x - overlay.rect.xMin, 10, Mathf.Max(10, fullWidth - width - 10));
            float top = overlay.rect.yMax - point.y + 8;
            height = Mathf.Min(670, fullHeight - top - 16);
            // At extreme UI scales fit the complete popup into the visible canvas.
            if (height < 280)
            {
                top = Mathf.Min(top, Mathf.Max(10, fullHeight - 296));
                height = Mathf.Min(670, fullHeight - top - 16);
            }

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
            for (int i = 0; i < rows.Count; i++)
                rows[i].Layout(width - 60);
        }

        void Initialize(LedgerBook book, Button anchor, RectTransform root)
        {
            this.book = book;
            this.anchor = anchor;
            overlay = root;
            ui = new LedgerUi(anchor);
            panel = LedgerUi.Rect(root, "GlobalLedgerPanel");
            ui.Background(panel, new Color(.16f, .09f, .09f), true);
            panel.gameObject.AddComponent<FoundationUiInputBlocker>();
            var title = ui.Text(panel, "Title", "GLOBAL LEDGER", 24);
            LedgerUi.Place(title.rectTransform, 24, 13, 500, 34);
            title.color = LedgerUi.Gold;
            var close = ui.Button(panel, "Close", "X", ClosePopup);
            close.GetComponent<RectTransform>().anchorMin = close.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
            close.GetComponent<RectTransform>().pivot = new Vector2(1, 1);
            close.GetComponent<RectTransform>().anchoredPosition = new Vector2(-16, -14);
            close.GetComponent<RectTransform>().sizeDelta = new Vector2(36, 32);
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
            currencies = ui.Button(panel, "CurrenciesTab", "Currencies", () => SelectTab(0));
            LedgerUi.Place((RectTransform)currencies.transform, 24, 56, 155, 38);
            production = ui.Button(panel, "ProductionTab", "Production", () => SelectTab(1));
            LedgerUi.Place((RectTransform)production.transform, 187, 56, 155, 38);
            showAllButton = ui.Button(panel, "ShowAll", "Show All", ToggleShowAll);
            LedgerUi.Place((RectTransform)showAllButton.transform, 518, 56, 155, 38);
            collapseAll = ui.Button(panel, "CollapseAll", "Collapse All", CollapseAll);
            LedgerUi.Place((RectTransform)collapseAll.transform, 681, 56, 155, 38);
            status = ui.Text(panel, "Window", "", 15);
            LedgerUi.Place(status.rectTransform, 24, 101, 740, 25);
            status.color = LedgerUi.Muted;
            viewport = LedgerUi.Rect(panel, "TreeViewport");
            LedgerUi.Fill(viewport, 20, 136, 24, 62);
            ui.Background(viewport, new Color(0, 0, 0, .15f));
            viewport.gameObject.AddComponent<RectMask2D>();
            content = LedgerUi.Rect(viewport, "TreeContent");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0, 1);
            content.sizeDelta = Vector2.zero;
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32;
            viewport.offsetMax = new Vector2(-40, -136);
            var track = LedgerUi.Rect(panel, "ScrollTrack");
            track.anchorMin = new Vector2(1, 0);
            track.anchorMax = Vector2.one;
            track.pivot = new Vector2(1, .5f);
            track.offsetMin = new Vector2(-33, 62);
            track.offsetMax = new Vector2(-23, -136);
            ui.Background(track, new Color(0, 0, 0, .28f));
            var handle = LedgerUi.Rect(track, "ScrollHandle");
            LedgerUi.Fill(handle);
            var handleImage = ui.Background(handle, new Color(.65f, .48f, .29f, .85f));
            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            footer = ui.Text(panel, "Footnote", "", 13);
            LedgerUi.Fill(footer.rectTransform, 24, 0, 24, 15);
            footer.rectTransform.anchorMin = footer.rectTransform.anchorMax = new Vector2(0, 0);
            footer.rectTransform.pivot = Vector2.zero;
            footer.rectTransform.sizeDelta = new Vector2(740, 38);
            footer.rectTransform.anchoredPosition = new Vector2(24, 14);
            footer.textWrappingMode = TextWrappingModes.Normal;
            footer.color = LedgerUi.Muted;
            Fit();
        }

        void OnEnable()
        {
            if (book != null)
            {
                transform.SetAsLastSibling();
                Fit();
                RefreshNow();
            }
        }

        public void OpenPopup()
        {
            if (gameObject.activeSelf)
                return;
            FoundationApi.UiAccess.PlaySound(GameUiSound.WindowOpen);
            gameObject.SetActive(true);
            RefreshNow();
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
                if (timeUnit == TimeUnit.Sec)
                    windowSeconds = entries;
                else
                    windowSeconds = entries * 60;
            }
            else
            {
                windowSeconds = 60;
            }

            string rateUnitStr = timeUnit == TimeUnit.Sec ? "second" : timeUnit == TimeUnit.Min ? "minute" : "hour";
            string modeDesc = rolling ? "Rolling " + entries + (timeUnit == TimeUnit.Sec ? "s" : "m") + " average (Rates: per " + rateUnitStr + ")" : "Last 60 seconds (Rates: per " + rateUnitStr + ")";
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
                while (rows.Count <= i)
                    rows.Add(new LedgerTreeRow(this, ui, content, rows.Count));
                rows[i].Bind(VisibleRows[i], width - 60);
            }

            for (int i = VisibleRows.Count; i < rows.Count; i++)
                rows[i].Root.gameObject.SetActive(false);
            content.sizeDelta = new Vector2(0, VisibleRows.Count * 36 + 8);
        }

        internal void ResetTree()
        {
            expanded.Clear();
            showAll = false;
            locations.Reset();
            scroll.verticalNormalizedPosition = 1;
        }

        internal void SelectTab(int value)
        {
            tab = value;
            scroll.StopMovement();
            content.anchoredPosition = Vector2.zero;
            RefreshNow();
        }

        internal void SetTimeUnit(TimeUnit unit)
        {
            if (timeUnit == unit)
                return;
            timeUnit = unit;
            RefreshNow();
        }

        internal void SetViewMode(ViewMode mode)
        {
            if (viewMode == mode)
                return;
            viewMode = mode;
            Fit();
            scroll.StopMovement();
            content.anchoredPosition = Vector2.zero;
            RefreshNow();
        }

        public void TogglePopup()
        {
            if (gameObject.activeSelf)
                ClosePopup();
            else
                OpenPopup();
        }

        internal void ToggleRollingAverage()
        {
            if (GlobalLedgerPlugin.Instance?.EnableRollingAverage != null)
            {
                GlobalLedgerPlugin.Instance.EnableRollingAverage.Value = !GlobalLedgerPlugin.Instance.EnableRollingAverage.Value;
            }

            RefreshNow();
        }

        internal void ToggleRow(string key)
        {
            if (!expanded.Add(key))
                expanded.Remove(key);
            RefreshNow();
        }

        internal void ToggleShowAll()
        {
            showAll = !showAll;
            RefreshNow();
        }

        void Update()
        {
            var bar = anchor ? anchor.transform.parent?.parent?.parent as RectTransform : null;
            if (!anchor || !anchor.gameObject.activeInHierarchy || !anchor.GetComponentInParent<Canvas>().enabled || (bar && bar.anchoredPosition.y > 0.5f))
            {
                if (FoundationApi.UiAccess.Root)
                    ClosePopup();
                return;
            }

            if (transform.GetSiblingIndex() != transform.parent.childCount - 1)
                transform.SetAsLastSibling();
            Fit();
            if (Time.unscaledTime >= nextRefresh)
                RefreshNow();
        }

        internal void View(LedgerRowData row)
        {
            if (row?.Target != null)
                GlobalLedgerPlugin.Instance.Navigation.Focus(row.Target);
        }
    }
}
