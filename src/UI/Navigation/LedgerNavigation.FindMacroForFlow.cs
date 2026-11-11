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
        static FlowElement FindMacroForFlow(FlowDiagram targetFlow)
        {
            if (targetFlow == null || GamePlayer.Current?.Flows == null) return null;
            foreach (var rootFlow in GamePlayer.Current.Flows)
            {
                var found = FindMacroRecursive(rootFlow, targetFlow);
                if (found != null) return found;
            }
            return null;
        }

    }
}
