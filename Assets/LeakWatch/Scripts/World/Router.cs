using UnityEngine;

namespace LeakWatch
{
    /// Wi-Fi router / access point on a building wall: white box, two antennas, blinking LEDs.
    public class Router : MonoBehaviour
    {
        Material[] leds;
        float flash;
        float phase;

        public Vector3 AntennaPoint => transform.position + new Vector3(0f, 1.0f, 0.1f);

        public void Blink() { flash = 1f; }

        public static Router Create(Transform parent, Vector3 pos)
        {
            var go = new GameObject("WifiRouter");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var r = go.AddComponent<Router>();

            var white = MaterialFactory.CreateOpaque(new Color(0.94f, 0.95f, 0.97f), 0.1f, 0.5f);
            var dark = MaterialFactory.CreateOpaque(new Color(0.15f, 0.16f, 0.18f), 0.4f, 0.4f);
            Prim.Box(go.transform, Vector3.zero, new Vector3(1.1f, 0.6f, 0.3f), white, "RouterBody");
            foreach (float sx in new[] { -0.36f, 0.36f })
            {
                var a = Prim.Make(PrimitiveType.Cylinder, go.transform, new Vector3(sx, 0.62f, 0f), new Vector3(0.06f, 0.35f, 0.06f), dark, "RouterAntenna");
                a.transform.localRotation = Quaternion.Euler(0f, 0f, sx < 0 ? 8f : -8f);
                Prim.Make(PrimitiveType.Sphere, go.transform, new Vector3(sx * 1.08f, 0.97f, 0f), Vector3.one * 0.1f, dark, "RouterAntennaTip");
            }

            r.leds = new Material[3];
            for (int i = 0; i < 3; i++)
            {
                var m = MaterialFactory.CreateOpaque(new Color(0.1f, 0.3f, 0.15f));
                Prim.Make(PrimitiveType.Sphere, go.transform, new Vector3(-0.3f + i * 0.3f, -0.1f, 0.16f), Vector3.one * 0.11f, m, "RouterLed");
                r.leds[i] = m;
            }
            return r;
        }

        void Update()
        {
            if (leds == null) return;
            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 2.5f);
            phase += Time.deltaTime;
            for (int i = 0; i < leds.Length; i++)
            {
                float idle = 0.25f + 0.25f * Mathf.Sin(phase * (2.2f + i * 1.3f) + i * 1.7f);
                float level = Mathf.Max(idle, flash);
                leds[i].color = Color.Lerp(new Color(0.1f, 0.3f, 0.15f), new Color(0.3f, 1f, 0.45f), level);
                MaterialFactory.SetEmission(leds[i], new Color(0.2f, 1f, 0.4f), level * 2.5f);
            }
        }
    }
}
