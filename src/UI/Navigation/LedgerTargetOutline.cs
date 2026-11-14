using KingdomFoundation.Api;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerTargetOutline : MaskableGraphic
    {
        internal static LedgerTargetOutline Create(RectTransform target)
        {
            var root = LedgerUi.Rect(target, "Ledger.TargetHighlight");
            root.gameObject.layer = target.gameObject.layer;
            LedgerUi.Fill(root, -5, -5, -5, -5);
            var graphic = root.gameObject.AddComponent<LedgerTargetOutline>();
            graphic.raycastTarget = false;
            graphic.color = new Color(.35f, 1, 1, 1);
            return graphic;
        }

        float width = 5;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            for (int layer = 0; layer < 3; layer++)
            {
                float extra = width * (2 - layer) * .6f;
                var tint = color;
                tint.a = layer == 0 ? .12f : layer == 1 ? .3f : 1;
                float left = r.xMin - extra, right = r.xMax + extra, bottom = r.yMin - extra, top = r.yMax + extra;
                float stroke = width * (layer == 2 ? .5f : 1);
                Quad(vh, left, bottom, right, bottom + stroke, tint);
                Quad(vh, left, top - stroke, right, top, tint);
                Quad(vh, left, bottom + stroke, left + stroke, top - stroke, tint);
                Quad(vh, right - stroke, bottom + stroke, right, top - stroke, tint);
            }
        }

        static void Quad(VertexHelper vh, float left, float bottom, float right, float top, Color tint)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(left, bottom), tint, Vector2.zero);
            vh.AddVert(new Vector3(left, top), tint, Vector2.zero);
            vh.AddVert(new Vector3(right, top), tint, Vector2.zero);
            vh.AddVert(new Vector3(right, bottom), tint, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }

        internal void SetWidth(float value)
        {
            if (!Mathf.Approximately(value, width))
            {
                width = value;
                SetVerticesDirty();
            }
        }
    }
}
