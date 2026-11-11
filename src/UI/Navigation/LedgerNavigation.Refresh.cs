using System.Reflection;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.TechTree;
using Assets.Source.Flow;
using Assets.Source.Player;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerNavigation : MonoBehaviour
    {
        void Refresh()
        {
            if (!GameUI.Instance || !GameUI.Instance.GetComponent<Canvas>().enabled) { Clear(); return; }
            if (selected != null && (Time.unscaledTime >= selectedUntil || !selected.Exists)) selected = null;
            if (hover != null && !hover.Exists) hover = null;
            var target = hover ?? selected;
            var view = target?.View;
            if (target == null || !view || !view.gameObject.activeInHierarchy) { Hide(); return; }
            if (!LedgerTarget.Same(shown, target) || !outline)
            {
                Hide(); shown = target; outline = LedgerTargetOutline.Create(view);
            }
            outline.SetWidth(WorldFlowCamera.Instance ? 10 / Mathf.Max(.1f, WorldFlowCamera.Instance.ZoomScale) : 5);
        }

    }
}
