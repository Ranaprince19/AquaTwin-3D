using UnityEngine;
using UnityEngine.UI;

namespace LeakWatch
{
    /// One-line caption (top left) and the small controls hint (bottom right).
    public class HudUI : MonoBehaviour
    {
        Text caption;
        CanvasGroup captionGroup;
        string pending;
        float fade = 1f;

        public void Build()
        {
            var cap = UIFactory.Panel(Ctx.SafeRoot, "Caption", new Color(0.04f, 0.06f, 0.09f, 0.82f));
            UIFactory.Place(cap, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(1000f, 76f));
            cap.GetComponent<Image>().raycastTarget = false;
            captionGroup = cap.gameObject.AddComponent<CanvasGroup>();
            Ctx.BlockRects.Add(cap);
            caption = UIFactory.Label(cap, "", 40, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Fill(caption.GetComponent<RectTransform>());

            var hint = UIFactory.Panel(Ctx.SafeRoot, "Controls", new Color(0.04f, 0.06f, 0.09f, 0.7f));
            UIFactory.Place(hint, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(820f, 44f));
            hint.GetComponent<Image>().raycastTarget = false;
            Ctx.BlockRects.Add(hint);
            var t = UIFactory.Label(hint, "Space next   B back   A auto   L leak   R repair   F9 screenshots", 22, new Color(0.85f, 0.9f, 0.95f), TextAnchor.MiddleCenter);
            UIFactory.Fill(t.GetComponent<RectTransform>());
        }

        public void SetCaption(string text)
        {
            pending = text;
            fade = 0f;
        }

        void Update()
        {
            fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime * 3f);
            if (pending != null) { caption.text = pending; pending = null; }
            captionGroup.alpha = fade;
        }
    }
}
