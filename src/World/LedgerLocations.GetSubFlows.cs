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
        private static IEnumerable<FlowDiagram> GetSubFlows(FlowDiagram flow, HashSet<FlowDiagram> visited)
        {
            if (flow?.Elements == null) yield break;
            for (int i = 0; i < flow.Elements.Count; i++)
            {
                var el = flow.Elements[i];
                if (el == null) continue;
                var prop = el.GetType().GetProperty("InnerFlow", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (prop != null && prop.GetValue(el) is FlowDiagram inner && visited.Add(inner))
                {
                    yield return inner;
                    foreach (var sub in GetSubFlows(inner, visited))
                        yield return sub;
                }
            }
        }

    }
}
