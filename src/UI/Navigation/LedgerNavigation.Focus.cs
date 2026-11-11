using KingdomFoundation.Api;
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
        internal bool Focus(LedgerTarget target)
        {
            if (target == null || !target.Exists || !WorldFlowManager.Instance || !GameUI.Instance) return false;
            if (TechTreeMenu.Visible) GameUI.Instance.ToggleTechMenu(false);
            int index = GamePlayer.Current.Flows.IndexOf(target.Flow);
            if (index >= 0)
            {
                FoundationApi.World.SelectFlow(index);
            }
            else
            {
                var parentMacro = FindMacroForFlow(target.Flow);
                if (parentMacro != null)
                {
                    var mdmType = AccessTools.TypeByName("KingdomLeylineLogic.MacroDimensionManager");
                    var enterMethod = mdmType != null ? AccessTools.Method(mdmType, "EnterMacroDimension") : null;
                    enterMethod?.Invoke(null, new object[] { parentMacro });
                }
            }
            if (WorldFlowCamera.Instance) FoundationApi.View.RecalculateCameraBounds();
            var view = target.View;
            if (!view || !Camera.main) return false;
            var center = view.TransformPoint(view.rect.center);
            var camera = Camera.main;
            camera.transform.position = new Vector3(center.x, center.y, camera.transform.position.z);
            target.Flow.CameraPos = new Vector2Int(Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y));
            selected = target; selectedUntil = Time.unscaledTime + 5;
            hover = null; Refresh();
            return true;
        }

    }
}
