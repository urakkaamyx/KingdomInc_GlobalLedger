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
    internal sealed partial class LedgerRowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal void Bind(LedgerTarget value)
        {
            if (inside && !LedgerTarget.Same(target, value)) GlobalLedgerPlugin.Instance?.Navigation?.Unhover(target);
            target = value;
            if (inside && target != null) GlobalLedgerPlugin.Instance?.Navigation?.Hover(target);
        }

    }
}
