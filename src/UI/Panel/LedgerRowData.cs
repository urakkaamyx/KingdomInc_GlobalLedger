using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerRowData
    {
        internal LedgerRowData(string key, string label, int depth)
        {
            Key = key;
            Label = label;
            Depth = depth;
        }

        internal readonly string Key, Label;
        internal readonly int Depth;
        internal GameCurrency? GameCurrency;
        internal GameItemType Item;
        internal long Amount;
        internal bool Expandable, Open;
        internal bool IconOnly, IsHeader;
        internal Sprite PrefixIcon, Icon;
        internal LedgerTarget Target;
    }
}
