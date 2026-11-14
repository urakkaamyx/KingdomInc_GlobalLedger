using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerNameScroll : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal const float HoverDelay = 2f, Speed = 32f, EndPause = 1.5f;
        TMP_Text label;
        RectTransform viewport;
        string identity;
        float elapsed, distance, width;
        bool inside, measured;

        internal void Initialize(TMP_Text text)
        {
            label = text;
            viewport = (RectTransform)transform;
        }

        internal void Bind(string key, string value)
        {
            if (identity == key && label.text == value) return;
            identity = key;
            label.text = value;
            ResetScroll();
        }

        internal void Layout()
        {
            if (Mathf.Abs(width - viewport.rect.width) > .01f)
            {
                width = viewport.rect.width;
                ResetScroll();
            }
            if (!measured) LedgerUi.Place(label.rectTransform, 0, 0, width, viewport.rect.height);
        }

        public void OnPointerEnter(PointerEventData data)
        {
            inside = true;
            ResetScroll();
        }

        public void OnPointerExit(PointerEventData data)
        {
            inside = false;
            ResetScroll();
        }

        void Update() => Advance(Time.unscaledDeltaTime);

        internal void Advance(float delta)
        {
            if (!inside || !label) return;
            elapsed += delta;
            if (elapsed < HoverDelay) return;
            if (!measured) Measure();
            if (distance <= 0) return;
            float travel = distance / Speed;
            float phase = (elapsed - HoverDelay) % (2 * travel + 2 * EndPause);
            float offset = phase < travel ? phase * Speed
                : phase < travel + EndPause ? distance
                : phase < 2 * travel + EndPause ? distance - (phase - travel - EndPause) * Speed : 0;
            label.rectTransform.anchoredPosition = new Vector2(-offset, 0);
        }

        void Measure()
        {
            measured = true;
            float fullWidth = label.GetPreferredValues(label.text, Mathf.Infinity, Mathf.Infinity).x;
            distance = Mathf.Max(0, fullWidth - width);
            if (distance <= .5f) { distance = 0; return; }
            label.overflowMode = TextOverflowModes.Overflow;
            LedgerUi.Place(label.rectTransform, 0, 0, fullWidth + 1, viewport.rect.height);
        }

        void ResetScroll()
        {
            elapsed = distance = 0;
            measured = false;
            if (!label) return;
            label.overflowMode = TextOverflowModes.Truncate;
            LedgerUi.Place(label.rectTransform, 0, 0, viewport.rect.width, viewport.rect.height);
        }

        void OnDisable() => OnPointerExit(null);
    }
}
