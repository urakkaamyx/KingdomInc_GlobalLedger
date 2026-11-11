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

    }
}
