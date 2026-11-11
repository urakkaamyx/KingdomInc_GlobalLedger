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
    internal sealed partial class LedgerLocationEntry
    {
        internal LedgerBookTotals Totals;
        internal string GroupKey, GroupName, BuildingName;
        internal Sprite GroupIcon, BuildingIcon;
        internal LedgerTarget GroupTarget, BuildingTarget;

    }
}
