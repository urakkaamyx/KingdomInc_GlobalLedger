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
        internal static int ItemTier(ItemType item)
        {
            if (item == null) return int.MaxValue;
            int tier = 1;
            for (int i = 1; i < TierFirstItems.Length; i++)
            {
                var boundary = ItemType.All.FirstOrDefault(t => t.Identifier == TierFirstItems[i]);
                if (boundary != null && item.Ordinal >= boundary.Ordinal) tier = i + 1;
            }
            return tier;
        }

    }
}
