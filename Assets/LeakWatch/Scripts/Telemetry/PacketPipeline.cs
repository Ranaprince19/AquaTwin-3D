using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LeakWatch
{
    /// Wireless data path, once per second per station, one packet per sensor:
    /// Collect: the sensor's Wi-Fi module LED blinks and packs the value with the location tag.
    /// Send (Full mode): three Wi-Fi arcs in the sensor's colour travel from the chamber antenna to the
    /// building router (LEDs blink), then router -> monitor, where that row updates with a flash.
    public class PacketPipeline : MonoBehaviour
    {
        public enum Mode { Off, CollectOnly, Full }
        public Mode CurrentMode = Mode.Off;

        class SensorPacket
        {
            public int station, k;
            public StationReading reading;
            public float age, delay;
            public bool started, wifiStarted, routerBlinked, arrived;
            public GameObject card;
            public LineRenderer[] arcs;
            public RectTransform fly;
        }

        class WifiLink
        {
            public LineRenderer arc;
            public Material arcMat;
            public Transform icon;
            public Vector3 mid;
            public RectTransform monitorLine;
        }

        const float TCollect = 1.2f, TWifi = 1.3f, TFly = 0.9f;
        readonly List<SensorPacket> packets = new List<SensorPacket>();
        Material cardMat;
        readonly ScreenLabel[][] cardLabels = new ScreenLabel[2][];
        readonly WifiLink[] links = new WifiLink[2];
        readonly bool[] wifiActive = new bool[2];
        ScreenLabel wifiLabel;
        Texture2D dashTex;

        static readonly Color[] SensorColors =
        {
            ExhibitView.PressureColor, ExhibitView.FlowColor, ExhibitView.VibrationColor, ExhibitView.LocationColor
        };

        public void Build()
        {
            cardMat = MaterialFactory.CreateOpaque(new Color(0.95f, 0.97f, 1f));
            MaterialFactory.SetEmission(cardMat, new Color(0.7f, 0.85f, 1f), 1.2f);

            dashTex = new Texture2D(8, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            var px = new Color32[8];
            for (int i = 0; i < 8; i++) px[i] = i < 4 ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            dashTex.SetPixels32(px);
            dashTex.Apply(false);

            for (int i = 0; i < 2; i++)
            {
                var st = Ctx.Stations[i];
                cardLabels[i] = new ScreenLabel[4];
                for (int k = 0; k < 4; k++)
                {
                    var c = SensorColors[k];
                    cardLabels[i][k] = LabelLayer.Create("--", 22, Color.white, new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, 0.94f), st.ModuleTops[k], new Vector2(0f, 46f));
                }
                links[i] = BuildLink(st);
            }
            wifiLabel = LabelLayer.Create("Wi-Fi", 22, Color.white, new Color(0.05f, 0.4f, 0.55f, 0.92f), links[0].mid + new Vector3(0f, 2.2f, 0f), Vector2.zero);

            Ctx.Sim.OnSample += OnSample;
        }

        Vector3 ArcPoint(Station st, float t)
        {
            Vector3 a = st.AntennaTip, b = st.Router.AntennaPoint;
            return Vector3.Lerp(a, b, t) + Vector3.up * (3f * 4f * t * (1f - t)) + Vector3.forward * (4f * t * (1f - t));
        }

        WifiLink BuildLink(Station st)
        {
            var link = new WifiLink();
            var go = new GameObject("WifiArc");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 30;
            for (int i = 0; i < 30; i++) lr.SetPosition(i, ArcPoint(st, i / 29f));
            lr.startWidth = lr.endWidth = 0.12f;
            lr.textureMode = LineTextureMode.Tile;
            var mat = MaterialFactory.LineMaterial();
            mat.mainTexture = dashTex;
            float len = Vector3.Distance(st.AntennaTip, st.Router.AntennaPoint) * 1.15f;
            mat.mainTextureScale = new Vector2(len / 0.9f, 1f);
            lr.material = mat;
            lr.startColor = lr.endColor = new Color(0.5f, 0.9f, 1f, 0.4f);
            link.arc = lr; link.arcMat = mat;
            link.mid = ArcPoint(st, 0.5f);

            var iconGo = new GameObject("WifiIcon");
            iconGo.transform.SetParent(transform, false);
            iconGo.transform.position = link.mid + new Vector3(0f, 0.2f, 0f);
            iconGo.transform.localScale = Vector3.one * 1.6f;
            var dot = Prim.Make(PrimitiveType.Sphere, iconGo.transform, new Vector3(0f, -0.35f, 0f), Vector3.one * 0.16f, MaterialFactory.CreateOpaque(new Color(0.5f, 0.95f, 1f)), "WifiDot");
            MaterialFactory.SetEmission(dot.GetComponent<Renderer>().sharedMaterial, new Color(0.4f, 0.9f, 1f), 2f);
            for (int a = 0; a < 3; a++)
            {
                var arcGo = new GameObject("WifiWave");
                arcGo.transform.SetParent(iconGo.transform, false);
                var l = arcGo.AddComponent<LineRenderer>();
                l.useWorldSpace = false; l.positionCount = 14;
                float r = 0.3f + a * 0.26f;
                for (int k = 0; k < 14; k++)
                {
                    float ang = Mathf.Lerp(50f, 130f, k / 13f) * Mathf.Deg2Rad;
                    l.SetPosition(k, new Vector3(Mathf.Cos(ang) * r, -0.35f + Mathf.Sin(ang) * r, 0f));
                }
                l.startWidth = l.endWidth = 0.07f;
                l.material = MaterialFactory.LineMaterial();
                l.startColor = l.endColor = new Color(0.5f, 0.95f, 1f, 1f);
            }
            link.icon = iconGo.transform;

            var line = UIFactory.Panel(Ctx.WorldRect, "MonitorLink", new Color(0.6f, 0.9f, 1f, 0.28f));
            line.anchorMin = line.anchorMax = new Vector2(0f, 0f);
            line.pivot = new Vector2(0.5f, 0.5f);
            line.GetComponent<Image>().raycastTarget = false;
            link.monitorLine = line;
            return link;
        }

        void OnDestroy() { if (Ctx.Sim != null) Ctx.Sim.OnSample -= OnSample; }

        public void ClearAll()
        {
            foreach (var p in packets) Cleanup(p);
            packets.Clear();
            foreach (var arr in cardLabels) foreach (var l in arr) l.SetVisible(false);
        }

        void OnSample(int station, StationReading reading)
        {
            if (CurrentMode == Mode.Off) return;
            for (int k = 0; k < 4; k++)
                packets.Add(new SensorPacket { station = station, k = k, reading = reading, delay = k * 0.3f });
        }

        static string ValueText(StationReading r, int k)
        {
            switch (k)
            {
                case 0: return r.pressure.ToString("0.0") + " bar";
                case 1: return r.flow.ToString("0.0") + " L/s";
                case 2: return r.vibration.ToString("0.0") + " mm/s";
                default: return r.tag;
            }
        }

        void Update()
        {
            wifiActive[0] = wifiActive[1] = false;

            for (int i = packets.Count - 1; i >= 0; i--)
            {
                var p = packets[i];
                p.age += Time.deltaTime;
                float t = p.age - p.delay;
                if (t < 0f) continue;

                var st = Ctx.Stations[p.station];
                bool full = CurrentMode == Mode.Full;
                var label = cardLabels[p.station][p.k];

                if (t < TCollect)
                {
                    if (!p.started)
                    {
                        p.started = true;
                        st.BlinkModule(p.k);
                        p.card = Prim.Make(PrimitiveType.Cube, transform, st.ModuleTops[p.k], new Vector3(0.6f, 0.38f, 0.06f), cardMat, "Packet");
                        label.SetText(p.reading.tag + "\n" + ValueText(p.reading, p.k));
                        label.SetVisible(true);
                    }
                    float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.3f));
                    p.card.transform.localScale = new Vector3(0.6f, 0.38f, 0.06f) * Mathf.Max(0.01f, u);
                    p.card.transform.position = st.ModuleTops[p.k] + Vector3.up * (t * 0.25f);
                    continue;
                }

                if (!full)
                {
                    if (t > TCollect + 0.3f) { label.SetVisible(false); Finish(i); }
                    continue;
                }

                float w = t - TCollect;
                if (w < TWifi)
                {
                    wifiActive[p.station] = true;
                    if (!p.wifiStarted)
                    {
                        p.wifiStarted = true;
                        if (p.card != null) { Destroy(p.card); p.card = null; }
                        label.SetVisible(false);
                        p.arcs = new LineRenderer[3];
                        for (int a = 0; a < 3; a++)
                        {
                            var go = new GameObject("WifiArcWave");
                            go.transform.SetParent(transform, false);
                            var lr = go.AddComponent<LineRenderer>();
                            lr.useWorldSpace = true; lr.positionCount = 14;
                            lr.material = MaterialFactory.LineMaterial();
                            lr.startWidth = lr.endWidth = 0.14f;
                            p.arcs[a] = lr;
                        }
                    }
                    UpdateArcs(p, st, w);
                }
                else if (w < TWifi + TFly)
                {
                    if (!p.routerBlinked) { p.routerBlinked = true; st.Router.Blink(); foreach (var a in p.arcs) a.enabled = false; }
                    if (p.fly == null) StartFly(p);
                    float u = Mathf.SmoothStep(0f, 1f, (w - TWifi) / TFly);
                    Vector2 from = Ctx.Cam.WorldToScreenPoint(st.Router.AntennaPoint);
                    Vector2 to = Ctx.Dashboard != null ? Ctx.Dashboard.TargetScreenPos(p.station, p.k) : from;
                    p.fly.position = Vector2.Lerp(from, to, u);
                    p.fly.localScale = Vector3.one * Mathf.Lerp(1f, 0.8f, u);
                }
                else
                {
                    if (!p.arrived)
                    {
                        p.arrived = true;
                        if (p.k < 3)
                        {
                            Ctx.Received.ReceiveField(p.station, p.k, p.reading);
                            if (p.k == 2) Ctx.Detector.Evaluate(Ctx.Received);
                        }
                        else Ctx.Dashboard?.FlashTag(p.station);
                    }
                    Finish(i);
                }
            }

            for (int s = 0; s < 2; s++) UpdateLink(s);
            wifiLabel.SetVisible(CurrentMode == Mode.Full);
        }

        void UpdateLink(int s)
        {
            var link = links[s];
            bool wifi = CurrentMode == Mode.Full;
            link.arc.enabled = wifi;
            link.icon.gameObject.SetActive(wifi);
            bool monitorOn = wifi && Ctx.Dashboard != null && Ctx.Dashboard.IsVisible;
            link.monitorLine.gameObject.SetActive(monitorOn);
            if (!wifi) return;

            float a = wifiActive[s] ? 0.95f : 0.4f;
            link.arc.startColor = link.arc.endColor = new Color(0.5f, 0.9f, 1f, a);
            link.arcMat.mainTextureOffset += new Vector2(-Time.deltaTime * 1.2f, 0f);
            if (Ctx.Cam != null) link.icon.rotation = Ctx.Cam.transform.rotation;

            if (monitorOn)
            {
                Vector2 from = Ctx.Cam.WorldToScreenPoint(Ctx.Stations[s].Router.AntennaPoint);
                Vector2 to = Ctx.Dashboard.CardLeftScreenPos(s);
                Vector2 d = to - from;
                float scale = Ctx.CanvasRect.lossyScale.x;
                var r = link.monitorLine;
                r.position = (from + to) * 0.5f;
                r.sizeDelta = new Vector2(d.magnitude / Mathf.Max(0.0001f, scale), 5f);
                r.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }
        }

        void UpdateArcs(SensorPacket p, Station st, float w)
        {
            Color col = SensorColors[p.k];
            for (int a = 0; a < 3; a++)
            {
                float prog = (w - a * 0.2f) / (TWifi - 0.45f);
                var lr = p.arcs[a];
                if (prog < 0f || prog > 1f) { lr.enabled = false; continue; }
                lr.enabled = true;
                Vector3 side0 = Vector3.zero;
                Vector3 c = ArcPoint(st, prog);
                Vector3 dir = (ArcPoint(st, Mathf.Min(1f, prog + 0.02f)) - ArcPoint(st, Mathf.Max(0f, prog - 0.02f))).normalized;
                Vector3 side = Vector3.Cross(dir, Vector3.forward).normalized;
                c += side * ((p.k - 1.5f) * 0.45f) + side0;
                float rad = 0.4f + a * 0.15f;
                for (int j = 0; j < 14; j++)
                {
                    float ang = Mathf.Lerp(-60f, 60f, j / 13f) * Mathf.Deg2Rad;
                    lr.SetPosition(j, c + dir * (Mathf.Cos(ang) * rad) + side * (Mathf.Sin(ang) * rad));
                }
                var cc = new Color(col.r, col.g, col.b, 1f - prog * 0.5f);
                lr.startColor = cc; lr.endColor = cc;
            }
        }

        void StartFly(SensorPacket p)
        {
            Color c = SensorColors[p.k];
            var rect = UIFactory.Panel(Ctx.WorldRect, "FlyingPacket", new Color(c.r * 0.7f, c.g * 0.7f, c.b * 0.7f, 0.97f));
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(190f, 52f);
            rect.GetComponent<Image>().raycastTarget = false;
            var t = UIFactory.Label(rect, ValueText(p.reading, p.k), 26, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Fill(t.GetComponent<RectTransform>());
            p.fly = rect;
        }

        void Finish(int i)
        {
            Cleanup(packets[i]);
            packets.RemoveAt(i);
        }

        void Cleanup(SensorPacket p)
        {
            if (p.card != null) Destroy(p.card);
            if (p.arcs != null) foreach (var r in p.arcs) if (r != null) Destroy(r.gameObject);
            if (p.fly != null) Destroy(p.fly.gameObject);
        }
    }
}
