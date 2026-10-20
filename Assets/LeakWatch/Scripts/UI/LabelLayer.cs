using UnityEngine;
using UnityEngine.UI;

namespace LeakWatch
{
    public static class LabelLayer
    {
        public static ScreenLabel Create(string text, int fontSize, Color textColor, Color bgColor, Vector3 world, Vector2 pixelOffset)
        {
            var rect = UIFactory.Panel(Ctx.WorldRect, "WorldLabel_" + text, bgColor);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            var bg = rect.GetComponent<Image>();
            bg.raycastTarget = false;

            var t = UIFactory.Label(rect, text, fontSize, textColor, TextAnchor.MiddleCenter, true);
            UIFactory.Fill(t.GetComponent<RectTransform>());

            var label = rect.gameObject.AddComponent<ScreenLabel>();
            label.Init(t, bg);
            label.World = world;
            label.PixelOffset = pixelOffset;
            label.Resize();
            return label;
        }
    }
}
