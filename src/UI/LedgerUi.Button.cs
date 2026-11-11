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
        internal Button Button(Transform parent, string name, string value, Action clicked, float fontSize = 19)
        {
            var rect = Rect(parent, name);
            var image = Background(rect, buttonStyle.color);
            image.sprite = buttonStyle.sprite; image.type = buttonStyle.type; image.pixelsPerUnitMultiplier = buttonStyle.pixelsPerUnitMultiplier;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => {
                if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
                Assets.Behaviour.UI.UISounds.Button();
                clicked();
            });
            var label = Text(rect, "Label", value, fontSize); label.alignment = TextAlignmentOptions.Center; return button;
        }

    }
}
