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
        internal LedgerTarget(FlowDiagram flow, FlowElement building = null, FlowDeco container = null)
        { Flow = flow; Building = building; Container = container; }

    }
}
