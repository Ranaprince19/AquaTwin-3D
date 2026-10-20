using UnityEngine;

namespace LeakWatch
{
    public static class TextureFactory
    {
        static Texture2D softCircle;

        /// A soft round white-alpha dot, procedurally generated (no imported assets),
        /// used as the sprite for the leak-jet particle system.
        public static Texture2D SoftCircle()
        {
            if (softCircle != null) return softCircle;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxDist = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float t = Mathf.Clamp01(dist / maxDist);
                    float alpha = Mathf.SmoothStep(1f, 0f, t);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false);
            softCircle = tex;
            return tex;
        }
    }
}
