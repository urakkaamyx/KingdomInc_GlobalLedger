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
        LedgerRowData AddItem(string key, string label, int depth, ItemType item, long amount, bool expandable)
        {
            var row = new LedgerRowData(key, label, depth) { Item = item, Amount = amount, Expandable = expandable, Open = expanded.Contains(key), Icon = depth == 0 ? item.GetIcon() : null };
            VisibleRows.Add(row); return row;
        }

    }
}
