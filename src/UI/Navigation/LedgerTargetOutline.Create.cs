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
    internal sealed partial class LedgerTargetOutline : MaskableGraphic
    {
        internal static LedgerTargetOutline Create(RectTransform target)
        {
            var root = LedgerUi.Rect(target, "GlobalLedger.TargetHighlight"); root.gameObject.layer = target.gameObject.layer;
            LedgerUi.Fill(root, -5, -5, -5, -5);
            var graphic = root.gameObject.AddComponent<LedgerTargetOutline>();
            graphic.raycastTarget = false; graphic.color = new Color(.35f, 1, 1, 1); return graphic;
        }

    }
}
