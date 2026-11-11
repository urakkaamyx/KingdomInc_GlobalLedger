using System.Reflection;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.TechTree;
using Assets.Source.Flow;
using Assets.Source.Player;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerNavigation : MonoBehaviour
    {
        static FlowElement FindMacroRecursive(FlowDiagram currentFlow, FlowDiagram targetFlow)
        {
            if (currentFlow?.Elements == null) return null;
            for (int i = 0; i < currentFlow.Elements.Count; i++)
            {
                var el = currentFlow.Elements[i];
                if (el == null) continue;
                var prop = el.GetType().GetProperty("InnerFlow", BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && prop.GetValue(el) is FlowDiagram inner)
                {
                    if (inner == targetFlow) return el;
                    var deeper = FindMacroRecursive(inner, targetFlow);
                    if (deeper != null) return deeper;
                }
            }
            return null;
        }

    }
}
