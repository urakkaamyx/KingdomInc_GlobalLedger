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
        internal readonly FlowDiagram Flow;
        internal readonly FlowElement Building;
        internal readonly FlowDeco Container;
        internal bool Exists => GamePlayer.Current != null && LedgerLocations.GetAllFlows(GamePlayer.Current).Contains(Flow)
            && (Building != null ? Flow.Elements.Contains(Building) : Container != null && Flow.Decorations.Contains(Container));
        internal RectTransform View => Building != null ? (Building.World ? (RectTransform)Building.World.transform : null)
            : Container?.World ? (RectTransform)Container.World.transform : null;

    }
}
