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

    }
}
