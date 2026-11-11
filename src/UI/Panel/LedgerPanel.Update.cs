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
        void Update()
        {
            var bar = anchor ? anchor.transform.parent?.parent?.parent as RectTransform : null;
            if (!anchor || !anchor.gameObject.activeInHierarchy || !anchor.GetComponentInParent<Canvas>().enabled || (bar && bar.anchoredPosition.y > 0.5f))
            { if (GameUI.Instance) ClosePopup(); return; }
            if (transform.GetSiblingIndex() != transform.parent.childCount - 1) transform.SetAsLastSibling();
            Fit();
            if (Time.unscaledTime >= nextRefresh) RefreshNow();
        }

    }
}
