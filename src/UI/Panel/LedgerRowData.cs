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
    internal sealed partial class LedgerRowData
    {
        internal readonly string Key, Label;
        internal readonly int Depth;
        internal Currency? Currency;
        internal ItemType Item;
        internal long Amount;
        internal bool Expandable, Open;
        internal bool IconOnly, IsHeader;
        internal Sprite PrefixIcon, Icon;
        internal LedgerTarget Target;

    }
}
