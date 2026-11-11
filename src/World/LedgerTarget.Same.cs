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
    internal sealed partial class LedgerTarget
    {
        internal static bool Same(LedgerTarget a, LedgerTarget b) => ReferenceEquals(a, b) || a != null && b != null
            && ReferenceEquals(a.Flow, b.Flow) && ReferenceEquals(a.Building, b.Building) && ReferenceEquals(a.Container, b.Container);

    }
}
