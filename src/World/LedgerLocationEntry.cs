using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerLocationEntry
    {
        internal LedgerBookTotals Totals;
        internal string GroupKey, GroupName, BuildingName;
        internal Sprite GroupIcon, BuildingIcon;
        internal LedgerTarget GroupTarget, BuildingTarget;
    }
}
