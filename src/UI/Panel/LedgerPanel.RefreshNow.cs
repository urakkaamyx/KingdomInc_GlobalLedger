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
                while (rows.Count <= i) rows.Add(new LedgerTreeRow(this, ui, content, rows.Count));
                rows[i].Bind(VisibleRows[i], width - 60);
            }
            for (int i = VisibleRows.Count; i < rows.Count; i++) rows[i].Root.gameObject.SetActive(false);
            content.sizeDelta = new Vector2(0, VisibleRows.Count * 36 + 8);
        }

    }
}
