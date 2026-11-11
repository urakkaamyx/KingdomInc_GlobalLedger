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
    internal sealed partial class LedgerTargetOutline : MaskableGraphic
    {
        static void Quad(VertexHelper vh, float left, float bottom, float right, float top, Color tint)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(left, bottom), tint, Vector2.zero); vh.AddVert(new Vector3(left, top), tint, Vector2.zero);
            vh.AddVert(new Vector3(right, top), tint, Vector2.zero); vh.AddVert(new Vector3(right, bottom), tint, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

    }
}
