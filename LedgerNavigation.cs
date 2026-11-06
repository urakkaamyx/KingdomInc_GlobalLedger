using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Source.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerNavigation : MonoBehaviour
    {
        LedgerTarget hover, selected, shown;
        LedgerTargetOutline outline;
        float selectedUntil;
        internal LedgerTarget Highlighted => shown;
        internal void Hover(LedgerTarget target) { hover = target; Refresh(); }
        internal void Unhover(LedgerTarget target) { if (LedgerTarget.Same(hover, target)) { hover = null; Refresh(); } }
        internal bool Focus(LedgerTarget target)
        {
            if (target == null || !target.Exists || !WorldFlowManager.Instance || !GameUI.Instance) return false;
            int index = GamePlayer.Current.Flows.IndexOf(target.Flow);
            WorldFlowManager.Instance.ShowFlow(index);
            if (WorldFlowCamera.Instance) WorldFlowCamera.Instance.RecalculateBounds();
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
        void LateUpdate() => Refresh();
        void Refresh()
        {
            if (!GameUI.Instance || !GameUI.Instance.GetComponent<Canvas>().enabled) { Clear(); return; }
            if (selected != null && (Time.unscaledTime >= selectedUntil || !selected.Exists)) selected = null;
            if (hover != null && !hover.Exists) hover = null;
            var target = hover ?? selected;
            var view = target?.View;
            if (target == null || !view || !view.gameObject.activeInHierarchy) { Hide(); return; }
            if (!LedgerTarget.Same(shown, target) || !outline)
            {
                Hide(); shown = target; outline = LedgerTargetOutline.Create(view);
            }
            outline.SetWidth(WorldFlowCamera.Instance ? 10 / Mathf.Max(.1f, WorldFlowCamera.Instance.ZoomScale) : 5);
        }
        void Hide()
        {
            if (outline) { outline.gameObject.SetActive(false); Destroy(outline.gameObject); }
            outline = null; shown = null;
        }
        internal void Clear() { hover = selected = null; Hide(); }
        void OnDestroy() => Clear();
    }

    internal sealed class LedgerRowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        LedgerTarget target;
        bool inside;
        internal void Bind(LedgerTarget value)
        {
            if (inside && !LedgerTarget.Same(target, value)) GlobalLedgerPlugin.Instance?.Navigation?.Unhover(target);
            target = value;
            if (inside && target != null) GlobalLedgerPlugin.Instance?.Navigation?.Hover(target);
        }
        public void OnPointerEnter(PointerEventData data) { inside = true; if (target != null) GlobalLedgerPlugin.Instance?.Navigation?.Hover(target); }
        public void OnPointerExit(PointerEventData data) { inside = false; GlobalLedgerPlugin.Instance?.Navigation?.Unhover(target); }
        void OnDisable() { if (inside) OnPointerExit(null); }
    }

    // An independent, non-raycasting overlay; never changes native colors, selection,
    // containers or serialized models, and follows a moving/resized target's transform.
    internal sealed class LedgerTargetOutline : MaskableGraphic
    {
        float width = 5;
        internal static LedgerTargetOutline Create(RectTransform target)
        {
            var root = LedgerUi.Rect(target, "GlobalLedger.TargetHighlight"); root.gameObject.layer = target.gameObject.layer;
            LedgerUi.Fill(root, -5, -5, -5, -5);
            var graphic = root.gameObject.AddComponent<LedgerTargetOutline>();
            graphic.raycastTarget = false; graphic.color = new Color(.35f, 1, 1, 1); return graphic;
        }
        internal void SetWidth(float value) { if (!Mathf.Approximately(value, width)) { width = value; SetVerticesDirty(); } }
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
        static void Quad(VertexHelper vh, float left, float bottom, float right, float top, Color tint)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(left, bottom), tint, Vector2.zero); vh.AddVert(new Vector3(left, top), tint, Vector2.zero);
            vh.AddVert(new Vector3(right, top), tint, Vector2.zero); vh.AddVert(new Vector3(right, bottom), tint, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
