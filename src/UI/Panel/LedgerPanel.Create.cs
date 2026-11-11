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

    }
}
