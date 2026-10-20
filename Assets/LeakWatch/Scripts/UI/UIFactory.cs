using UnityEngine;
using UnityEngine.UI;

namespace LeakWatch
{
    public static class UIFactory
    {
        static Font font;

        public static Font GetFont()
        {
            if (font != null) return font;
            try
            {
                font = Font.CreateDynamicFontFromOSFont(new[] { "Helvetica Neue", "Arial" }, 32);
            }
            catch (System.Exception) { font = null; }
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }

        static Texture2D wifiTex;

        /// Procedural Wi-Fi symbol: a dot and three arcs.
        public static Texture2D WifiTexture()
        {
            if (wifiTex != null) return wifiTex;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = x - 32f, dy = y - 12f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    bool on = d < 5.5f;
                    if (!on && ang > 48f && ang < 132f)
                        foreach (float r in new[] { 16f, 28f, 40f }) if (Mathf.Abs(d - r) < 2.8f) on = true;
                    px[y * n + x] = on ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            tex.SetPixels32(px);
            tex.Apply(false);
            wifiTex = tex;
            return tex;
        }

        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go.GetComponent<RectTransform>();
        }

        public static RectTransform Empty(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = GetFont();
            t.fontSize = Mathf.Max(22, size);
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.color = color;
            t.text = text;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static void Place(RectTransform r, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.pivot = pivot;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
        }

        public static void Fill(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        /// Position inside a parent using top-left origin (x right, y down).
        public static void TopLeft(RectTransform r, float x, float y, float w, float h)
            => Place(r, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(w, h));

        public static Image Block(Transform parent, string name, Color color, float x, float y, float w, float h)
        {
            var r = Panel(parent, name, color);
            TopLeft(r, x, y, w, h);
            var img = r.GetComponent<Image>();
            img.raycastTarget = false;
            return img;
        }
    }
}
