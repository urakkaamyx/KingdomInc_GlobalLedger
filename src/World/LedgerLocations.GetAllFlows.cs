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
    internal sealed partial class LedgerLocations
    {
        internal static IEnumerable<FlowDiagram> GetAllFlows(GamePlayer player)
        {
            if (player?.Flows == null) yield break;
            var visited = new HashSet<FlowDiagram>();
            foreach (var flow in player.Flows)
            {
                if (flow == null || !visited.Add(flow)) continue;
                yield return flow;
                foreach (var sub in GetSubFlows(flow, visited))
                    yield return sub;
            }
        }

    }
}
