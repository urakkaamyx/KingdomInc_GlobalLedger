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
        LedgerTarget hover, selected, shown;
        LedgerTargetOutline outline;
        float selectedUntil;
        internal LedgerTarget Highlighted => shown;

    }
}
