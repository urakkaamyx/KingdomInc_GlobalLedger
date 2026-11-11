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
        public void ClosePopup(bool playSound)
        {
            if (!gameObject.activeSelf) return;
            if (playSound && UISounds.Instance) UISounds.WindowClose();
            gameObject.SetActive(false);
            OnPopupClosed?.Invoke();
        }

    }
}
