using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Behaviour.UI.Shared;
using Assets.Source.Items;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerUi
    {
        internal LedgerUi(Button nativeButton)
        {
            font = nativeButton.GetComponentInChildren<TMP_Text>(true);
            buttonStyle = nativeButton.GetComponent<Image>();
            frame = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(s => s.name == "DefaultBorderDark");
        }

    }
}
