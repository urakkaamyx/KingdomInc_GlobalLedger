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
            if (row.IsHeader) return;
            selectedKey = row.Key;
            if (row.Expandable) ToggleRow(row.Key);
            else RefreshNow();
        }

        LedgerRowData Add(string key, string label, int depth, GameCurrency currency, long amount, bool expandable, long income = 0, long outflow = 0)
        {
            var row = new LedgerRowData(key, label, depth)
            {
                GameCurrency = currency,
                Amount = amount,
                Income = income,
                Outflow = outflow,
                Expandable = expandable,
                Open = expanded.Contains(key),
                Icon = depth == 0 ? currency.GetIcon() : null
            };
            VisibleRows.Add(row);
            return row;
        }

        LedgerRowData AddItem(string key, string label, int depth, GameItemType item, long amount, bool expandable, long income = 0, long outflow = 0)
        {
            var row = new LedgerRowData(key, label, depth)
            {
                Item = item,
                Amount = amount,
                Income = income,
                Outflow = outflow,
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
            var currentTotals = snapshot;
            // Section 1: CURRENCIES
            VisibleRows.Add(new LedgerRowData("basic/header/currencies", "CURRENCIES", 0) { IsHeader = true });
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
                    Income = currentTotals.Sum(t => t.Earned[c]),
                    Outflow = currentTotals.Sum(t => t.Spent[c]),
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
            VisibleRows.Add(new LedgerRowData("basic/header/production", "ITEM PRODUCTION", 0) { IsHeader = true });
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
                    Income = currentTotals.Sum(t => t.ItemProduced(id)),
                    Outflow = currentTotals.Sum(t => t.ItemConsumed(id)),
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
            var data = locations.Resolve(snapshot);
            foreach (GameCurrency currency in Enum.GetValues(typeof(GameCurrency)))
            {
                if (!currency.IsUnlocked())
                    continue;
                int c = (int)currency;
                if (!showAll && !book.HasCurrencyActivity(c) && currency != GameCurrency.Gold) continue;
                string root = "currency/" + c;
                var active = data.Where(t => t.Totals.Active(c)).ToArray();
                Add(root, LedgerUi.CurrencyName(currency), 0, currency, active.Sum(t => t.Totals.Net(c)), true, active.Sum(t => t.Totals.Earned[c]), active.Sum(t => t.Totals.Spent[c]));
                if (!expanded.Contains(root) && string.IsNullOrEmpty(search))
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
                    var containerRow = Add(groupKey, container.GroupName, 1, currency, group.Sum(t => t.Totals.Net(c)), true, group.Sum(t => t.Totals.Earned[c]), group.Sum(t => t.Totals.Spent[c]));
                    containerRow.PrefixIcon = container.GroupTarget != null ? LedgerLocations.ContainerIcon() : null;
                    containerRow.Icon = container.GroupIcon;
                    containerRow.Target = container.GroupTarget;
                    if (!expanded.Contains(groupKey) && string.IsNullOrEmpty(search))
                        continue;
                    foreach (var entry in group.OrderBy(t => t.BuildingName, StringComparer.CurrentCulture).ThenBy(t => t.Totals.Source.Id, StringComparer.Ordinal))
                    {
                        var building = entry.Totals;
                        string buildingKey = groupKey + "/" + building.Source.Id;
                        var buildingRow = Add(buildingKey, entry.BuildingName, 2, currency, building.Net(c), true, building.Earned[c], building.Spent[c]);
                        buildingRow.Icon = entry.BuildingIcon;
                        buildingRow.Target = entry.BuildingTarget;
                        if (!expanded.Contains(buildingKey) && string.IsNullOrEmpty(search))
                            continue;
                        string costs = buildingKey + "/costs";
                        VisibleRows.Add(new LedgerRowData(costs, "Operating Costs", 3) { Expandable = true, Open = expanded.Contains(costs) });
                        if (!expanded.Contains(costs) && string.IsNullOrEmpty(search))
                            continue;
                        bool any = false;
                        foreach (GameCurrency costCurrency in Enum.GetValues(typeof(GameCurrency)))
                            if (building.Spent[(int)costCurrency] != 0 && costCurrency.IsUnlocked())
                            {
                                var cost = Add(costs + "/" + (int)costCurrency, LedgerUi.CurrencyName(costCurrency), 4, costCurrency, -building.Spent[(int)costCurrency], false, 0, building.Spent[(int)costCurrency]);
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
            var data = locations.Resolve(snapshot);
            foreach (var item in LedgerUi.OrderedUnlockedItems())
            {
                string id = item.Identifier;
                string root = "item/" + id;
                var active = data.Where(t => t.Totals.ItemActive(id)).ToArray();
                if (!showAll && active.Length == 0)
                    continue;
                AddItem(root, LedgerUi.ItemName(item), 0, item, active.Sum(t => t.Totals.ItemNet(id)), true, active.Sum(t => t.Totals.ItemProduced(id)), active.Sum(t => t.Totals.ItemConsumed(id)));
                if (!expanded.Contains(root) && string.IsNullOrEmpty(search))
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
                    var containerRow = AddItem(groupKey, container.GroupName, 1, item, group.Sum(t => t.Totals.ItemNet(id)), true, group.Sum(t => t.Totals.ItemProduced(id)), group.Sum(t => t.Totals.ItemConsumed(id)));
                    containerRow.PrefixIcon = container.GroupTarget != null ? LedgerLocations.ContainerIcon() : null;
                    containerRow.Icon = container.GroupIcon;
                    containerRow.Target = container.GroupTarget;
                    if (!expanded.Contains(groupKey) && string.IsNullOrEmpty(search))
                        continue;
                    foreach (var entry in group.OrderBy(t => t.BuildingName, StringComparer.CurrentCulture).ThenBy(t => t.Totals.Source.Id, StringComparer.Ordinal))
                    {
                        var building = entry.Totals;
                        string buildingKey = groupKey + "/" + building.Source.Id;
                        var buildingRow = AddItem(buildingKey, entry.BuildingName, 2, item, building.ItemNet(id), true, building.ItemProduced(id), building.ItemConsumed(id));
                        buildingRow.Icon = entry.BuildingIcon;
                        buildingRow.Target = entry.BuildingTarget;
                        if (!expanded.Contains(buildingKey) && string.IsNullOrEmpty(search))
                            continue;
                        string costs = buildingKey + "/costs";
                        VisibleRows.Add(new LedgerRowData(costs, "Operating Costs", 3) { Expandable = true, Open = expanded.Contains(costs) });
                        if (!expanded.Contains(costs) && string.IsNullOrEmpty(search))
                            continue;
                        bool any = false;
                        foreach (var costItem in LedgerUi.OrderedUnlockedItems())
                        {
                            long spentItem = building.ItemConsumed(costItem.Identifier);
                            if (spentItem != 0)
                            {
                                var cost = AddItem(costs + "/item/" + costItem.Identifier, LedgerUi.ItemName(costItem), 4, costItem, -spentItem, false, 0, spentItem);
                                cost.Icon = costItem.GetIcon();
                                cost.IconOnly = true;
                                any = true;
                            }
                        }

                        foreach (GameCurrency costCurrency in Enum.GetValues(typeof(GameCurrency)))
                            if (building.Spent[(int)costCurrency] != 0 && costCurrency.IsUnlocked())
                            {
                                var cost = Add(costs + "/currency/" + (int)costCurrency, LedgerUi.CurrencyName(costCurrency), 4, costCurrency, -building.Spent[(int)costCurrency], false, 0, building.Spent[(int)costCurrency]);
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
                VisibleRows.Add(new LedgerRowData("production/empty", "No observed production in the selected window. Click Show All to list unlocked items.", 0));
        }

        public void ClosePopup() => ClosePopup(true);
        public void ClosePopup(bool playSound)
        {
            if (!gameObject.activeSelf) return;
            if (playSound) FoundationApi.UiAccess.PlaySound(GameUiSound.WindowClose);
            gameObject.SetActive(false);
            OnPopupClosed?.Invoke();
        }

        internal void CollapseAll()
        {
            expanded.Clear();
            ResetScroll();
            RefreshNow();
        }

        internal static LedgerPanel Create(LedgerBook book, Button anchor)
        {
            var root = LedgerUi.Rect(anchor.GetComponentInParent<Canvas>().rootCanvas.transform, "GlobalLedgerDropdown");
            LedgerUi.Fill(root);
            root.gameObject.SetActive(false);
            var result = root.gameObject.AddComponent<LedgerPanel>();
            try { result.Initialize(book, anchor, root); return result; }
            catch { Destroy(root.gameObject); throw; }
        }

        public enum ViewMode { Basic, Adv }
        public enum TimeUnit { Sec, Min, Hour }
        public Action OnPopupClosed;
        internal const float Width = 840, Height = 760, RowHeight = 36;
        internal const float IncomeX = 300, OutflowX = 430, NetX = 560, CellWidth = 122, ViewX = 700;
        LedgerBook book;
        LedgerUi ui;
        Button anchor;
        RectTransform overlay, panel, content, viewport, controls, track;
        TMP_Text status, footer, title;
        TMP_InputField searchInput;
        Button basicButton, advButton, secButton, minButton, hourButton, rollingButton;
        Button currencies, production, showAllButton, collapseAll, sortButton, directionButton, refreshButton, freezeButton;
        readonly Button[] windowButtons = new Button[3];
        readonly TMP_Text[] columnHeadings = new TMP_Text[3];
        ScrollRect scroll;
        readonly HashSet<string> expanded = new HashSet<string>();
        readonly List<LedgerTreeRow> rows = new List<LedgerTreeRow>();
        readonly LedgerRowOrdering ordering = new LedgerRowOrdering();
        readonly LedgerSmoothing smoothing = new LedgerSmoothing();
        LedgerLocations locations => GlobalLedgerPlugin.Instance.Locations;
        internal readonly List<LedgerRowData> VisibleRows = new List<LedgerRowData>();
        IReadOnlyList<LedgerBookTotals> snapshot;
        readonly Vector3[] anchorCorners = new Vector3[4];
        float nextRefresh;
        double snapshotElapsed;
        int tab, sortField, windowIndex, refreshSeconds = 1;
        bool showAll, descending, frozen, controlsCollapsed;
        string search = "", selectedKey = "";
        ViewMode viewMode = ViewMode.Basic;
        TimeUnit timeUnit = TimeUnit.Min;
        static readonly int[][] WindowOptions = { new[] { 1, 10, 30 }, new[] { 1, 5, 10 }, new[] { 1, 12, 24 } };
        int UnitSeconds => timeUnit == TimeUnit.Sec ? 1 : timeUnit == TimeUnit.Hour ? 3600 : 60;
        int WindowAmount => WindowOptions[(int)timeUnit][windowIndex];
        int windowSeconds => WindowAmount * UnitSeconds;
        float ControlsShift => controlsCollapsed ? 132 : 0;
        internal int SelectedTab => tab;
        internal bool ShowingAll => showAll;
        internal ViewMode Mode => viewMode;
        internal TimeUnit Unit => timeUnit;
        internal int WindowSeconds => windowSeconds;
        internal string StatusText => status ? status.text : "";
        internal RectTransform Frame => panel;
        internal ScrollRect Scroller => scroll;
        internal string SelectedKey => selectedKey;
        internal bool ControlsCollapsed => controlsCollapsed;
        internal bool Frozen => frozen;
        internal double Rate(long value) => (double)value / windowSeconds * UnitSeconds;

        void Initialize(LedgerBook book, Button anchor, RectTransform root)
        {
            this.book = book;
            this.anchor = anchor;
            overlay = root;
            ui = new LedgerUi(anchor);
            panel = LedgerUi.Rect(root, "GlobalLedgerPanel");
            ui.Background(panel, new Color(.16f, .09f, .09f), true);
            panel.gameObject.AddComponent<FoundationUiInputBlocker>();
            CreateHeader();
            CreateControls();
            CreateTable();
            Fit();
        }

        Button Control(Transform parent, string name, string label, Action action, float x, float y, float width, float fontSize = 17)
        {
            var button = ui.Button(parent, name, label, action, fontSize);
            LedgerUi.Place((RectTransform)button.transform, x, y, width, 32);
            return button;
        }

        void CreateHeader()
        {
            title = ui.Text(panel, "Title", "GLOBAL LEDGER", 24);
            title.color = LedgerUi.Gold;
            LedgerUi.Place(title.rectTransform, 24, 13, 320, 34);
            Control(panel, "ControlsToggle", "Controls", ToggleControls, Width - 350, 14, 108);
            basicButton = Control(panel, "BasicMode", "Basic", () => SetViewMode(ViewMode.Basic), Width - 232, 14, 74);
            advButton = Control(panel, "AdvMode", "Adv", () => SetViewMode(ViewMode.Adv), Width - 152, 14, 74);
            Control(panel, "Close", "X", ClosePopup, Width - 62, 14, 38);
        }

        void CreateControls()
        {
            controls = LedgerUi.Rect(panel, "Controls");
            LedgerUi.Place(controls, 0, 56, Width, 128);
            secButton = Control(controls, "UnitSec", "sec", () => SetTimeUnit(TimeUnit.Sec), 24, 0, 58);
            minButton = Control(controls, "UnitMin", "min", () => SetTimeUnit(TimeUnit.Min), 88, 0, 58);
            hourButton = Control(controls, "UnitHour", "hour", () => SetTimeUnit(TimeUnit.Hour), 152, 0, 58);
            for (int i = 0; i < windowButtons.Length; i++)
            {
                int option = i;
                windowButtons[i] = Control(controls, "Window" + i, "", () => SetWindow(option), 24 + i * 64, 42, 58);
            }
            rollingButton = Control(controls, "RollingToggle", "", ToggleRollingAverage, 638, 0, 178);
            freezeButton = Control(controls, "Freeze", "Freeze", ToggleFreeze, 690, 42, 126);
            sortButton = Control(controls, "Sort", "", () => SetSort((sortField + 1) % LedgerRowOrdering.Fields.Length), 232, 0, 238);
            directionButton = Control(controls, "Direction", "Ascending", () => SetDescending(!descending), 480, 0, 148);
            refreshButton = Control(controls, "RefreshInterval", "", () => SetRefresh(refreshSeconds % 10 + 1), 232, 42, 180);
            currencies = Control(controls, "CurrenciesTab", "Currencies", () => SelectTab(0), 424, 42, 120);
            production = Control(controls, "ProductionTab", "Production", () => SelectTab(1), 552, 42, 126);
            searchInput = ui.Search(controls, "Search", SetSearch);
            LedgerUi.Place((RectTransform)searchInput.transform, 24, 84, 384, 32);
            showAllButton = Control(controls, "ShowAll", "Show All", ToggleShowAll, 420, 84, 172);
            collapseAll = Control(controls, "CollapseAll", "Collapse All", CollapseAll, 604, 84, 212);
        }

        void CreateTable()
        {
            status = ui.Text(panel, "Window", "", 15);
            status.color = LedgerUi.Muted;
            string[] names = { "IncomeHeading", "OutflowHeading", "NetHeading" };
            int[] fields = { 2, 3, 4 };
            for (int i = 0; i < names.Length; i++)
            {
                int field = fields[i];
                var button = Control(panel, names[i], "", () => SetSort(field), 24 + IncomeX + i * (OutflowX - IncomeX), 208, CellWidth, 15);
                columnHeadings[i] = button.GetComponentInChildren<TMP_Text>();
            }
            viewport = LedgerUi.Rect(panel, "TreeViewport");
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
            scroll.scrollSensitivity = 36;
            track = LedgerUi.Rect(panel, "ScrollTrack");
            ui.Background(track, new Color(0, 0, 0, .28f));
            var handle = LedgerUi.Rect(track, "ScrollHandle");
            LedgerUi.Fill(handle);
            var image = ui.Background(handle, new Color(.65f, .48f, .29f, .85f));
            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = image;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            footer = ui.Text(panel, "Footnote", "", 14);
            footer.color = LedgerUi.Muted;
        }

        void Fit()
        {
            if (!anchor || !panel) return;
            ((RectTransform)anchor.transform).GetWorldCorners(anchorCorners);
            Vector2 point = overlay.InverseTransformPoint(anchorCorners[0]);
            float top = Mathf.Round(Mathf.Max(8, overlay.rect.yMax - point.y + 8));
            float maximumHeight = Height - ControlsShift;
            float minimumHeight = 380 - ControlsShift;
            if (overlay.rect.height - top - 12 < minimumHeight)
                top = Mathf.Round(Mathf.Max(8, overlay.rect.height - minimumHeight - 12));
            float height = Mathf.Floor(Mathf.Min(maximumHeight, overlay.rect.height - top - 12));
            float x = Mathf.Round(Mathf.Clamp(point.x - overlay.rect.xMin, 10, Mathf.Max(10, overlay.rect.width - Width - 10)));
            LedgerUi.Place(panel, x, top, Width, height);
            // Match original V1: preserve the native pixel font/art scale and scroll
            // through fewer rows on shorter canvases instead of shrinking the panel.
            panel.localScale = Vector3.one;
            controls.gameObject.SetActive(!controlsCollapsed);
            LedgerUi.Place(status.rectTransform, 24, 186 - ControlsShift, Width - 48, 22);
            for (int i = 0; i < columnHeadings.Length; i++)
                LedgerUi.Place((RectTransform)columnHeadings[i].transform.parent, 24 + IncomeX + i * (OutflowX - IncomeX), 210 - ControlsShift, CellWidth, 28);
            LedgerUi.Place(viewport, 24, 242 - ControlsShift, Width - 72, Mathf.Max(1, height - 304 + ControlsShift));
            LedgerUi.Place(track, Width - 40, 242 - ControlsShift, 10, Mathf.Max(1, height - 304 + ControlsShift));
            LedgerUi.Place(footer.rectTransform, 24, height - 48, Width - 48, 28);
        }

        internal void RefreshNow()
        {
            bool sample = snapshot == null || (!frozen && Time.unscaledTime >= nextRefresh);
            if (sample)
            {
                snapshot = book.Snapshot(windowSeconds);
                snapshotElapsed = book.Elapsed;
                nextRefresh = Time.unscaledTime + refreshSeconds;
            }
            UpdateControls();
            VisibleRows.Clear();
            if (viewMode == ViewMode.Basic) BuildBasic();
            else if (tab == 0) BuildCurrencies();
            else BuildProduction();
            if (GlobalLedgerPlugin.Instance?.EnableRollingAverage?.Value == true)
                smoothing.Apply(VisibleRows, Mathf.Clamp(GlobalLedgerPlugin.Instance.RollingAverageEntries.Value, 2, 30), sample);
            var ordered = ordering.Arrange(VisibleRows, search, sortField, descending);
            VisibleRows.Clear();
            VisibleRows.AddRange(ordered);
            BindRows();
            status.text = "Last " + WindowAmount + (timeUnit == TimeUnit.Sec ? "s" : timeUnit == TimeUnit.Hour ? "h" : "m")
                + " | Rates " + LedgerUi.TimeUnitSuffix(timeUnit) + (frozen ? " | FROZEN" : " | Live")
                + (snapshotElapsed < windowSeconds ? " | Collecting " + (int)snapshotElapsed + "/" + windowSeconds + "s" : "");
            int count = VisibleRows.Count(row => !row.IsHeader && (row.GameCurrency.HasValue || row.Item != null));
            footer.text = count == 0 && search.Length != 0 ? "No matching resources or sources."
                : count + " rows | In: made/earned. Out: used/spent. Sort by heading; View locates.";
        }

        void BindRows()
        {
            for (int i = 0; i < VisibleRows.Count; i++)
            {
                VisibleRows[i].Top = i * RowHeight;
                while (rows.Count <= i) rows.Add(new LedgerTreeRow(this, ui, content, rows.Count));
                rows[i].Bind(VisibleRows[i], Width - 72);
            }
            for (int i = VisibleRows.Count; i < rows.Count; i++) rows[i].Root.gameObject.SetActive(false);
            content.sizeDelta = new Vector2(0, VisibleRows.Count * RowHeight + 8);
        }

        static void Select(Button button, bool active) => button.GetComponentInChildren<TMP_Text>().color = active ? LedgerUi.Gold : LedgerUi.Paper;
        void UpdateControls()
        {
            Select(basicButton, viewMode == ViewMode.Basic);
            Select(advButton, viewMode == ViewMode.Adv);
            Select(secButton, timeUnit == TimeUnit.Sec);
            Select(minButton, timeUnit == TimeUnit.Min);
            Select(hourButton, timeUnit == TimeUnit.Hour);
            Select(currencies, tab == 0);
            Select(production, tab == 1);
            Select(showAllButton, showAll);
            bool rolling = GlobalLedgerPlugin.Instance?.EnableRollingAverage?.Value == true;
            Select(rollingButton, rolling);
            rollingButton.GetComponentInChildren<TMP_Text>().text = "Average: " + (rolling ? "On" : "Off");
            freezeButton.GetComponentInChildren<TMP_Text>().text = frozen ? "Resume" : "Freeze";
            sortButton.GetComponentInChildren<TMP_Text>().text = "Sort: " + LedgerRowOrdering.Fields[sortField];
            directionButton.GetComponentInChildren<TMP_Text>().text = descending ? "Descending" : "Ascending";
            refreshButton.GetComponentInChildren<TMP_Text>().text = "Refresh: " + refreshSeconds + "s";
            currencies.gameObject.SetActive(viewMode == ViewMode.Adv);
            production.gameObject.SetActive(viewMode == ViewMode.Adv);
            collapseAll.gameObject.SetActive(viewMode == ViewMode.Adv);
            for (int i = 0; i < windowButtons.Length; i++)
            {
                Select(windowButtons[i], windowIndex == i);
                windowButtons[i].GetComponentInChildren<TMP_Text>().text = WindowOptions[(int)timeUnit][i] + (timeUnit == TimeUnit.Sec ? "s" : timeUnit == TimeUnit.Hour ? "h" : "m");
            }
            string suffix = LedgerUi.TimeUnitSuffix(timeUnit);
            columnHeadings[0].text = "IN " + suffix;
            columnHeadings[1].text = "OUT " + suffix;
            columnHeadings[2].text = "NET " + suffix;
        }

        void ResetScroll() { scroll.StopMovement(); content.anchoredPosition = Vector2.zero; }
        internal void SetWindow(int index) { windowIndex = Mathf.Clamp(index, 0, 2); InvalidateWindow(); }
        void InvalidateWindow() { smoothing.Clear(); snapshot = null; RefreshNow(); }
        internal void SetRefresh(int seconds) { refreshSeconds = Mathf.Clamp(seconds, 1, 10); nextRefresh = Time.unscaledTime + refreshSeconds; RefreshNow(); }
        internal void ToggleFreeze() { frozen = !frozen; if (!frozen) nextRefresh = 0; RefreshNow(); }
        internal void SetSearch(string value) { search = value ?? ""; ResetScroll(); RefreshNow(); }
        internal void SetSort(int field) { if (field == sortField) descending = !descending; else sortField = Mathf.Clamp(field, 0, LedgerRowOrdering.Fields.Length - 1); RefreshNow(); }
        internal void SetDescending(bool value) { descending = value; RefreshNow(); }
        internal void ToggleControls() { controlsCollapsed = !controlsCollapsed; Fit(); }
        internal void SelectTab(int value) { tab = value; ResetScroll(); RefreshNow(); }
        internal void SetTimeUnit(TimeUnit unit) { if (timeUnit == unit) return; timeUnit = unit; InvalidateWindow(); }
        internal void SetViewMode(ViewMode mode) { viewMode = mode; ResetScroll(); RefreshNow(); }
        internal void ToggleRollingAverage()
        {
            smoothing.Clear();
            var setting = GlobalLedgerPlugin.Instance?.EnableRollingAverage;
            if (setting != null) setting.Value = !setting.Value;
            RefreshNow();
        }
        internal void ToggleRow(string key) { if (!expanded.Add(key)) expanded.Remove(key); RefreshNow(); }
        internal void ToggleShowAll() { showAll = !showAll; RefreshNow(); }
        public void TogglePopup() { if (gameObject.activeSelf) ClosePopup(); else OpenPopup(); }
        public void OpenPopup()
        {
            if (gameObject.activeSelf) return;
            FoundationApi.UiAccess.PlaySound(GameUiSound.WindowOpen);
            nextRefresh = 0;
            gameObject.SetActive(true);
        }
        void OnEnable() { if (book != null) { transform.SetAsLastSibling(); Fit(); RefreshNow(); } }
        internal void ResetTree()
        {
            expanded.Clear(); smoothing.Clear(); snapshot = null; frozen = false; showAll = false;
            search = ""; searchInput.SetTextWithoutNotify(""); ResetScroll();
        }
        void Update()
        {
            var bar = anchor ? anchor.transform.parent?.parent?.parent as RectTransform : null;
            if (!anchor || !anchor.gameObject.activeInHierarchy || !anchor.GetComponentInParent<Canvas>().enabled || (bar && bar.anchoredPosition.y > .5f))
            {
                if (FoundationApi.UiAccess.Root) ClosePopup();
                return;
            }
            if (transform.GetSiblingIndex() != transform.parent.childCount - 1) transform.SetAsLastSibling();
            Fit();
            if (!frozen && Time.unscaledTime >= nextRefresh) RefreshNow();
        }
        internal void View(LedgerRowData row) { if (row?.Target != null) GlobalLedgerPlugin.Instance.Navigation.Focus(row.Target); }
    }
}
