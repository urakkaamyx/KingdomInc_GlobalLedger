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
    internal sealed partial class LedgerTreeRow
    {
        internal readonly RectTransform Root;
        internal readonly Button Button, ViewButton;
        readonly TMP_Text label, amount, arrow, separator, goldText, silverText, copperText, goldUnit;
        readonly RectTransform goldGroup;
        readonly Image goldCoin, silverCoin, copperCoin;
        readonly Button expander;
        readonly LedgerRowHover hover;
        readonly Image prefixIcon, icon, shade;
        readonly int index;
        readonly LedgerPanel owner;
        LedgerRowData data;

    }
}
