using UnityEngine;
using UnityEngine.UI;

namespace LeakWatch
{
    /// Monitoring screen on the right. Values change only when a packet has arrived.
    public class DashboardUI : MonoBehaviour
    {
        class Card
        {
            public Image bg;
            public RectTransform rect;
            public Text pressure, flow, vibration;
            public UILineGraph gP, gF, gV;
            public float flash;
            public Image[] rowBg = new Image[3];
            public float[] rowFlash = new float[3];
            public Image chipBg;
            public float chipFlash, wifiPulse;
            public RawImage wifi;
        }

        const float PanelW = 600f;
        readonly Card[] cards = new Card[2];
        CanvasGroup group;
        Image alertBg;
        Text alertText;
        RectTransform panel;
        float visible;
        bool wantVisible;

        static readonly Color CardNormal = new Color(0.1f, 0.19f, 0.27f, 1f);
        static readonly Color CardLeak = new Color(0.4f, 0.09f, 0.09f, 1f);
        static readonly Color CardFlash = new Color(0.22f, 0.5f, 0.66f, 1f);

        public void Build()
        {
            panel = UIFactory.Panel(Ctx.SafeRoot, "Dashboard", new Color(0.05f, 0.06f, 0.08f, 0.96f));
            UIFactory.Place(panel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(PanelW, 790f));
            group = panel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            Ctx.BlockRects.Add(panel);

            var screen = UIFactory.Panel(panel, "Screen", new Color(0.04f, 0.09f, 0.13f, 1f));
            UIFactory.Place(screen, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            screen.offsetMin = new Vector2(12f, 12f); screen.offsetMax = new Vector2(-12f, -12f);
            screen.GetComponent<Image>().raycastTarget = false;

            var title = UIFactory.Label(screen, "Monitoring Dashboard", 30, new Color(0.8f, 0.9f, 1f), TextAnchor.MiddleLeft, true);
            UIFactory.TopLeft(title.GetComponent<RectTransform>(), 18f, 8f, 480f, 44f);

            var alert = UIFactory.Panel(screen, "Alert", new Color(0.1f, 0.35f, 0.2f, 1f));
            UIFactory.TopLeft(alert, 14f, 60f, PanelW - 24f - 28f, 84f);
            alertBg = alert.GetComponent<Image>(); alertBg.raycastTarget = false;
            alertText = UIFactory.Label(alert, "System normal", 34, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Fill(alertText.GetComponent<RectTransform>());

            string[] tags = { "UB-IN", "TP2-IN" };
            for (int i = 0; i < 2; i++) cards[i] = BuildCard(screen, tags[i], 158f + i * 262f);

            var foot = UIFactory.Label(screen, "Simulated data - rule-based detection", 22, new Color(0.6f, 0.68f, 0.76f), TextAnchor.MiddleLeft);
            UIFactory.TopLeft(foot.GetComponent<RectTransform>(), 18f, 158f + 2 * 262f + 4f, 540f, 34f);

            Ctx.Received.OnArrived += OnArrived;
            Ctx.Received.OnFieldArrived += OnFieldArrived;
            Ctx.Received.OnCleared += OnCleared;
        }

        Card BuildCard(Transform parent, string tag, float y)
        {
            var c = new Card();
            var rect = UIFactory.Panel(parent, "Card_" + tag, CardNormal);
            UIFactory.TopLeft(rect, 14f, y, PanelW - 24f - 28f, 250f);
            c.rect = rect; c.bg = rect.GetComponent<Image>(); c.bg.raycastTarget = false;

            var chip = UIFactory.Panel(rect, "TagChip", new Color(0.38f, 0.2f, 0.62f, 1f));
            UIFactory.TopLeft(chip, 14f, 10f, 190f, 44f);
            c.chipBg = chip.GetComponent<Image>();
            c.chipBg.raycastTarget = false;
            var wifiGo = new GameObject("WifiStatus", typeof(RectTransform), typeof(RawImage));
            wifiGo.transform.SetParent(rect, false);
            c.wifi = wifiGo.GetComponent<RawImage>();
            c.wifi.texture = UIFactory.WifiTexture();
            c.wifi.raycastTarget = false;
            UIFactory.TopLeft(wifiGo.GetComponent<RectTransform>(), PanelW - 24f - 28f - 74f, 4f, 56f, 56f);
            var chipText = UIFactory.Label(chip, tag, 30, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Fill(chipText.GetComponent<RectTransform>());

            c.pressure = Row(rect, "Pressure", 64f, ExhibitView.PressureColor, out c.gP, out c.rowBg[0]);
            c.flow = Row(rect, "Flow", 120f, ExhibitView.FlowColor, out c.gF, out c.rowBg[1]);
            c.vibration = Row(rect, "Vibration", 176f, ExhibitView.VibrationColor, out c.gV, out c.rowBg[2]);
            return c;
        }

        Text Row(Transform parent, string label, float y, Color color, out UILineGraph graph, out Image rowBg)
        {
            rowBg = UIFactory.Block(parent, "RowFlash", new Color(color.r, color.g, color.b, 0f), 6f, y - 2f, PanelW - 24f - 28f - 12f, 52f);
            var l = UIFactory.Label(parent, label, 26, color, TextAnchor.MiddleLeft, true);
            UIFactory.TopLeft(l.GetComponent<RectTransform>(), 16f, y, 170f, 48f);
            var v = UIFactory.Label(parent, "--", 30, Color.white, TextAnchor.MiddleLeft, true);
            UIFactory.TopLeft(v.GetComponent<RectTransform>(), 190f, y, 200f, 48f);

            var rawGo = new GameObject("Graph", typeof(RectTransform), typeof(RawImage));
            rawGo.transform.SetParent(parent, false);
            var raw = rawGo.GetComponent<RawImage>(); raw.raycastTarget = false;
            UIFactory.TopLeft(rawGo.GetComponent<RectTransform>(), 400f, y + 2f, 134f, 44f);
            graph = rawGo.AddComponent<UILineGraph>();
            graph.Init(raw, 67, 22, color, new Color(0.06f, 0.1f, 0.14f, 1f));
            return v;
        }

        public void SetVisible(bool v) { wantVisible = v; }
        public bool IsVisible => visible > 0.5f;

        public Vector2 CardLeftScreenPos(int station)
        {
            var r = cards[station].rect;
            return r.TransformPoint(new Vector3(r.rect.xMin, r.rect.center.y, 0f));
        }

        public Vector2 CardScreenPos(int station)
        {
            var r = cards[station].rect;
            return r.TransformPoint(r.rect.center);
        }

        void OnArrived(int s)
        {
            var d = Ctx.Received;
            var c = cards[s];
            var r = d.latest[s];
            c.pressure.text = r.pressure.ToString("0.0") + " bar";
            c.flow.text = r.flow.ToString("0.0") + " L/s";
            c.vibration.text = r.vibration.ToString("0.0") + " mm/s";
            c.gP.Draw(d.pressureHistory[s], 2f, 5f);
            c.gF.Draw(d.flowHistory[s], 8f, 14f);
            c.gV.Draw(d.vibrationHistory[s], 0f, 1.1f);
            c.flash = 1f;
            for (int i = 0; i < 3; i++) c.rowFlash[i] = 1f;
        }

        public Vector2 TargetScreenPos(int station, int k)
        {
            var c = cards[station];
            RectTransform r = k < 3 ? c.rowBg[k].rectTransform : c.chipBg.rectTransform;
            return r.TransformPoint(r.rect.center);
        }

        public void FlashTag(int station) { cards[station].chipFlash = 1f; cards[station].wifiPulse = 1f; }

        void OnFieldArrived(int s, int k)
        {
            var d = Ctx.Received;
            var c = cards[s];
            var r = d.latest[s];
            if (k == 0) { c.pressure.text = r.pressure.ToString("0.0") + " bar"; c.gP.Draw(d.pressureHistory[s], 2f, 5f); }
            else if (k == 1) { c.flow.text = r.flow.ToString("0.0") + " L/s"; c.gF.Draw(d.flowHistory[s], 8f, 14f); }
            else { c.vibration.text = r.vibration.ToString("0.0") + " mm/s"; c.gV.Draw(d.vibrationHistory[s], 0f, 1.1f); }
            c.rowFlash[k] = 1f;
            c.wifiPulse = 1f;
        }

        void OnCleared()
        {
            foreach (var c in cards)
            {
                c.pressure.text = c.flow.text = c.vibration.text = "--";
                c.gP.Draw(null, 0, 1); c.gF.Draw(null, 0, 1); c.gV.Draw(null, 0, 1);
            }
        }

        void Update()
        {
            visible = Mathf.MoveTowards(visible, wantVisible ? 1f : 0f, Time.unscaledDeltaTime * 2.5f);
            group.alpha = visible;

            bool leak = Ctx.Story != null && Ctx.Story.AlertsEnabled && Ctx.Detector.Detected;
            alertBg.color = leak ? new Color(0.62f, 0.1f, 0.1f, 1f) : new Color(0.1f, 0.35f, 0.2f, 1f);
            alertText.text = leak ? "Leak detected - inside UB" : "System normal";

            foreach (var c in cards)
            {
                c.flash = Mathf.MoveTowards(c.flash, 0f, Time.unscaledDeltaTime * 2.2f);
                Color baseC = leak ? CardLeak : CardNormal;
                c.bg.color = Color.Lerp(baseC, CardFlash, c.flash * 0.5f);
                c.chipFlash = Mathf.MoveTowards(c.chipFlash, 0f, Time.unscaledDeltaTime * 1.6f);
                c.chipBg.color = Color.Lerp(new Color(0.38f, 0.2f, 0.62f, 1f), new Color(0.85f, 0.6f, 1f, 1f), c.chipFlash);
                c.wifiPulse = Mathf.MoveTowards(c.wifiPulse, 0f, Time.unscaledDeltaTime * 2f);
                c.wifi.color = Color.Lerp(new Color(0.4f, 0.5f, 0.55f, 0.8f), new Color(0.5f, 1f, 1f, 1f), c.wifiPulse);
                c.wifi.rectTransform.localScale = Vector3.one * (1f + 0.35f * c.wifiPulse);
                Color[] rc = { ExhibitView.PressureColor, ExhibitView.FlowColor, ExhibitView.VibrationColor };
                for (int i = 0; i < 3; i++)
                {
                    c.rowFlash[i] = Mathf.MoveTowards(c.rowFlash[i], 0f, Time.unscaledDeltaTime * 1.6f);
                    c.rowBg[i].color = new Color(rc[i].r, rc[i].g, rc[i].b, c.rowFlash[i] * 0.7f);
                }
            }
        }
    }
}
