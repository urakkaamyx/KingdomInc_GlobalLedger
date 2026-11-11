using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Assets.Source.Adventure;
using Assets.Source.Flow;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerLocations
    {
        internal static Sprite ContainerIcon()
        {
            if (cachedContainerIcon) return cachedContainerIcon;
            if (Assets.Behaviour.UI.GameUI.Instance)
            {
                var decoMenu = AccessTools.Field(typeof(Assets.Behaviour.UI.GameUI), "_decoMenu").GetValue(Assets.Behaviour.UI.GameUI.Instance) as Component;
                if (decoMenu)
                {
                    var isLabel = AccessTools.Field(typeof(Assets.Behaviour.UI.Deco.DecoBuyMenuRow), "_isLabel");
                    var isSticker = AccessTools.Field(typeof(Assets.Behaviour.UI.Deco.DecoBuyMenuRow), "_isSticker");
                    var costField = AccessTools.Field(typeof(Assets.Behaviour.UI.Deco.DecoBuyMenuRow), "_cost");
                    foreach (var row in decoMenu.GetComponentsInChildren<Assets.Behaviour.UI.Deco.DecoBuyMenuRow>(true))
                    {
                        if ((bool)isLabel.GetValue(row) || (bool)isSticker.GetValue(row)) continue;
                        var cost = costField.GetValue(row) as Component;
                        var icon = row.GetComponentsInChildren<Image>(true)
                            .FirstOrDefault(i => i.gameObject != row.gameObject && i.sprite != null && i.type != Image.Type.Sliced && (cost == null || !i.transform.IsChildOf(cost.transform)));
                        if (icon != null) return cachedContainerIcon = icon.sprite;
                    }
                }
            }
            return SpriteLibrary.Get("Icons_31");
        }

    }
}
