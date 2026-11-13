using KingdomFoundation.Api;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerRowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal void Bind(LedgerTarget value)
        {
            if (inside && !LedgerTarget.Same(target, value))
                GlobalLedgerPlugin.Instance?.Navigation?.Unhover(target);
            target = value;
            if (inside && target != null)
                GlobalLedgerPlugin.Instance?.Navigation?.Hover(target);
        }

        LedgerTarget target;
        bool inside;
        void OnDisable()
        {
            if (inside)
                OnPointerExit(null);
        }

        public void OnPointerEnter(PointerEventData data)
        {
            inside = true;
            if (target != null)
                GlobalLedgerPlugin.Instance?.Navigation?.Hover(target);
        }

        public void OnPointerExit(PointerEventData data)
        {
            inside = false;
            GlobalLedgerPlugin.Instance?.Navigation?.Unhover(target);
        }
    }
}
