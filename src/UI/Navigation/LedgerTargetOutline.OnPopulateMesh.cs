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
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect;
            for (int layer = 0; layer < 3; layer++)
            {
                float extra = width * (2 - layer) * .6f;
                var tint = color; tint.a = layer == 0 ? .12f : layer == 1 ? .3f : 1;
                float left = r.xMin - extra, right = r.xMax + extra, bottom = r.yMin - extra, top = r.yMax + extra;
                float stroke = width * (layer == 2 ? .5f : 1);
                Quad(vh, left, bottom, right, bottom + stroke, tint); Quad(vh, left, top - stroke, right, top, tint);
                Quad(vh, left, bottom + stroke, left + stroke, top - stroke, tint); Quad(vh, right - stroke, bottom + stroke, right, top - stroke, tint);
            }
        }

    }
}
