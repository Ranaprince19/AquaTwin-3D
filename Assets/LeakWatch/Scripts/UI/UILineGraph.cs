using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LeakWatch
{
    /// Small live graph drawn into a procedural texture (no external assets).
    public class UILineGraph : MonoBehaviour
    {
        Texture2D tex;
        int width, height;
        Color32 line, bg;

        public void Init(RawImage target, int w, int h, Color lineColor, Color background)
        {
            width = w; height = h;
            line = lineColor; bg = background;
            tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            target.texture = tex;
            Draw(null, 0f, 1f);
        }

        public void Draw(Queue<float> values, float min, float max)
        {
            var px = new Color32[width * height];
            for (int i = 0; i < px.Length; i++) px[i] = bg;

            if (values != null && values.Count > 1)
            {
                var arr = values.ToArray();
                int prevY = -1;
                for (int x = 0; x < width; x++)
                {
                    float f = x / (float)(width - 1) * (arr.Length - 1);
                    int i0 = Mathf.Min(arr.Length - 1, Mathf.FloorToInt(f));
                    int i1 = Mathf.Min(arr.Length - 1, i0 + 1);
                    float v = Mathf.Lerp(arr[i0], arr[i1], f - i0);
                    int y = Mathf.Clamp(Mathf.RoundToInt(Mathf.InverseLerp(min, max, v) * (height - 5)) + 2, 1, height - 2);
                    int y0 = prevY < 0 ? y : Mathf.Min(prevY, y);
                    int y1 = prevY < 0 ? y : Mathf.Max(prevY, y);
                    for (int yy = y0; yy <= y1; yy++)
                        for (int t = -1; t <= 0; t++)
                        {
                            int yt = Mathf.Clamp(yy + t, 0, height - 1);
                            px[yt * width + x] = line;
                        }
                    prevY = y;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false);
        }
    }
}
