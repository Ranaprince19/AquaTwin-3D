using UnityEngine;
using UnityEngine.UI;

namespace LeakWatch
{
    /// UI label that follows a world position (screen-space, always readable).
    public class ScreenLabel : MonoBehaviour
    {
        public Vector3 World;
        public Vector2 PixelOffset;
        public Text Text;
        public Image Background;

        CanvasGroup group;
        RectTransform rect;
        float alpha;
        float targetAlpha;
        float scale = 1f;
        bool wantVisible;

        public void Init(Text text, Image bg)
        {
            Text = text;
            Background = bg;
            rect = GetComponent<RectTransform>();
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        public void SetText(string s)
        {
            if (Text.text == s) return;
            Text.text = s;
            Resize();
        }

        public void Resize()
        {
            float w = Text.preferredWidth + 36f;
            float h = Text.preferredHeight + 14f;
            rect.sizeDelta = new Vector2(w, h);
        }

        static readonly Vector3[] corners = new Vector3[4];

        /// 0 when the label would sit on a UI panel, fading in over 50 px outside the panel edge.
        float PanelFade(Vector3 sp)
        {
            float scale = Ctx.WorldRect.lossyScale.x;
            Vector2 half = rect.sizeDelta * scale * 0.5f;
            float fade = 1f;
            foreach (var r in Ctx.BlockRects)
            {
                if (r == null || !r.gameObject.activeInHierarchy) continue;
                var g = r.GetComponent<CanvasGroup>();
                float a = g != null ? g.alpha : 1f;
                if (a < 0.05f) continue;
                r.GetWorldCorners(corners);
                float dx = Mathf.Max(corners[0].x - half.x - sp.x, sp.x - (corners[2].x + half.x), 0f);
                float dy = Mathf.Max(corners[0].y - half.y - sp.y, sp.y - (corners[2].y + half.y), 0f);
                float d = Mathf.Max(dx, dy);
                float f = Mathf.Clamp01(d / 50f);
                fade = Mathf.Min(fade, Mathf.Lerp(1f, f, a));
            }
            return fade;
        }

        public void SetVisible(bool v) { wantVisible = v; targetAlpha = v ? 1f : 0f; }
        public void SetScale(float s) { scale = s; }
        public void SetColors(Color text, Color bg) { Text.color = text; Background.color = bg; }

        void LateUpdate()
        {
            alpha = Mathf.MoveTowards(alpha, targetAlpha, Time.unscaledDeltaTime * 4f);
            var cam = Ctx.Cam;
            if (cam == null || Ctx.WorldRect == null) return;

            Vector3 sp = cam.WorldToScreenPoint(World);
            bool front = sp.z > 0.1f;
            group.alpha = front ? alpha * PanelFade(sp) : 0f;

            if (front && (alpha > 0.001f))
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(Ctx.WorldRect, new Vector2(sp.x, sp.y), null, out Vector2 local);
                rect.anchoredPosition = local + PixelOffset;
                rect.localScale = Vector3.one * scale;
            }
        }
    }
}
