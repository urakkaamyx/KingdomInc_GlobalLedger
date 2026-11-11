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
        public void OpenPopup()
        {
            if (gameObject.activeSelf) return;
            if (UISounds.Instance) UISounds.WindowOpen();
            gameObject.SetActive(true);
            RefreshNow();
        }

    }
}
