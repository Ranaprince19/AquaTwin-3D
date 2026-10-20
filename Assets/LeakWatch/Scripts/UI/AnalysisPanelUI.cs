using UnityEngine;
using UnityEngine.UI;

namespace LeakWatch
{
    /// Station A vs Station B: bar pairs, a difference marker inside a green normal band
    /// (pressure and flow), then a simple line map that pins the leak on UB.
    public class AnalysisPanelUI : MonoBehaviour
    {
        const float PanelW = 1180f, PanelH = 450f, Baseline = 236f, MaxBar = 120f;

        CanvasGroup group;
        bool wantVisible;
        float visible;

        readonly Image[] barA = new Image[3], barB = new Image[3];
        readonly Text[] valA = new Text[3], valB = new Text[3];
        readonly float[] dispA = new float[3], dispB = new float[3];
        readonly Image[] marker = new Image[2];
        readonly RectTransform[] track = new RectTransform[2];
        float[] markerPos = new float[2];

        Image segmentHighlight, pin;
        RectTransform pinRect;
        float pinDrop;

        static readonly float[] BarMax = { 5f, 14f, 1.2f };
        static readonly Color ColA = new Color(0.45f, 0.78f, 1f);
        static readonly Color ColB = new Color(0.98f, 0.8f, 0.4f);
        static readonly Color Good = new Color(0.25f, 0.85f, 0.4f);
        static readonly Color Bad = new Color(1f, 0.25f, 0.2f);

        public void Build()
        {
            var panel = UIFactory.Panel(Ctx.SafeRoot, "AnalysisPanel", new Color(0.05f, 0.07f, 0.1f, 0.94f));
            UIFactory.Place(panel, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(PanelW, PanelH));
            panel.GetComponent<Image>().raycastTarget = false;
            group = panel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            Ctx.BlockRects.Add(panel);

            var title = UIFactory.Label(panel, "Station A vs Station B", 30, Color.white, TextAnchor.MiddleLeft, true);
            UIFactory.TopLeft(title.GetComponent<RectTransform>(), 22f, 8f, 540f, 42f);
            LegendChip(panel, 640f, ColA, "A  UB-IN");
            LegendChip(panel, 880f, ColB, "B  TP2-IN");

            string[] names = { "Pressure", "Flow", "Vibration" };
            Color[] cols = { ExhibitView.PressureColor, ExhibitView.FlowColor, ExhibitView.VibrationColor };
            for (int i = 0; i < 3; i++)
            {
                float x0 = 20f + i * 390f;
                var h = UIFactory.Label(panel, names[i], 28, cols[i], TextAnchor.MiddleCenter, true);
                UIFactory.TopLeft(h.GetComponent<RectTransform>(), x0, 54f, 360f, 40f);

                barA[i] = Bar(panel, x0 + 60f, ColA); barB[i] = Bar(panel, x0 + 190f, ColB);
                valA[i] = Number(panel, x0 + 60f); valB[i] = Number(panel, x0 + 190f);
                Letter(panel, x0 + 60f, "A"); Letter(panel, x0 + 190f, "B");

                if (i < 2) BuildTrack(panel, i, x0 + 10f);
            }

            var diff = UIFactory.Label(panel, "Difference A - B", 22, new Color(0.7f, 0.78f, 0.86f), TextAnchor.MiddleLeft);
            UIFactory.TopLeft(diff.GetComponent<RectTransform>(), 24f, 342f, 300f, 28f);

            BuildMap(panel);
        }

        void LegendChip(Transform parent, float x, Color c, string text)
        {
            var chip = UIFactory.Block(parent, "Chip", c, x, 16f, 22f, 22f);
            var t = UIFactory.Label(parent, text, 24, Color.white, TextAnchor.MiddleLeft);
            UIFactory.TopLeft(t.GetComponent<RectTransform>(), x + 32f, 8f, 200f, 38f);
        }

        Image Bar(Transform parent, float x, Color c)
        {
            var img = UIFactory.Block(parent, "Bar", c, x, Baseline, 96f, 4f);
            return img;
        }

        Text Number(Transform parent, float x)
        {
            var t = UIFactory.Label(parent, "0.0", 26, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.TopLeft(t.GetComponent<RectTransform>(), x - 10f, Baseline + 4f, 116f, 32f);
            return t;
        }

        void Letter(Transform parent, float x, string s)
        {
            var t = UIFactory.Label(parent, s, 22, new Color(0.7f, 0.78f, 0.86f), TextAnchor.MiddleCenter);
            UIFactory.TopLeft(t.GetComponent<RectTransform>(), x - 10f, Baseline + 34f, 116f, 28f);
        }

        void BuildTrack(Transform parent, int i, float x)
        {
            var tr = UIFactory.Panel(parent, "Track", new Color(0.14f, 0.17f, 0.22f, 1f));
            UIFactory.TopLeft(tr, x, 306f, 340f, 26f);
            tr.GetComponent<Image>().raycastTarget = false;
            track[i] = tr;

            float[] bandLo = { 0.15f, 0.3f }, bandHi = { 0.45f, 0.7f }, range = { 1.0f, 2.4f };
            var band = UIFactory.Panel(tr, "NormalBand", new Color(0.2f, 0.6f, 0.3f, 0.75f));
            band.anchorMin = new Vector2(bandLo[i] / range[i], 0f);
            band.anchorMax = new Vector2(bandHi[i] / range[i], 1f);
            band.offsetMin = band.offsetMax = Vector2.zero;
            band.GetComponent<Image>().raycastTarget = false;

            var m = UIFactory.Panel(tr, "Marker", Good);
            m.anchorMin = m.anchorMax = new Vector2(0f, 0.5f);
            m.pivot = new Vector2(0.5f, 0.5f);
            m.sizeDelta = new Vector2(14f, 44f);
            m.GetComponent<Image>().raycastTarget = false;
            marker[i] = m.GetComponent<Image>();
        }

        void BuildMap(Transform parent)
        {
            float y = 398f;
            float[] xs = { 100f, 350f, 600f, 850f, 1080f };
            string[] names = { "Pump", "A", "UB", "B", "TP2" };

            for (int i = 0; i < 4; i++)
                UIFactory.Block(parent, "MapLine", new Color(0.4f, 0.46f, 0.55f), xs[i], y - 3f, xs[i + 1] - xs[i], 6f);
            segmentHighlight = UIFactory.Block(parent, "MapHighlight", Bad, xs[1], y - 5f, xs[3] - xs[1], 10f);
            segmentHighlight.enabled = false;

            for (int i = 0; i < 5; i++)
            {
                Color c = (i == 1 || i == 3) ? ExhibitView.LocationColor : new Color(0.8f, 0.85f, 0.92f);
                UIFactory.Block(parent, "MapNode", c, xs[i] - 12f, y - 12f, 24f, 24f);
                var t = UIFactory.Label(parent, names[i], 24, Color.white, TextAnchor.MiddleCenter, true);
                UIFactory.TopLeft(t.GetComponent<RectTransform>(), xs[i] - 50f, y + 16f, 100f, 30f);
            }

            var pinR = UIFactory.Panel(parent, "Pin", ExhibitView.LocationColor);
            UIFactory.TopLeft(pinR, xs[2] - 17f, y - 52f, 34f, 34f);
            pin = pinR.GetComponent<Image>(); pin.raycastTarget = false;
            pinRect = pinR;
            pinX = xs[2] - 17f; pinBaseY = y - 52f;
            pin.enabled = false;
        }

        float pinX, pinBaseY;

        public void SetVisible(bool v) { wantVisible = v; if (!v) pinDrop = 0f; }

        void Update()
        {
            visible = Mathf.MoveTowards(visible, wantVisible ? 1f : 0f, Time.unscaledDeltaTime * 2.5f);
            group.alpha = visible;
            if (visible < 0.01f) return;

            float t = Ctx.Story != null ? Ctx.Story.SceneTime : 0f;
            StationReading a, b;
            if (t < 3f || !Ctx.Received.HasBoth)
            {
                bool leakNow = t >= 3f;
                a = leakNow ? WaterSimulation.LeakA : WaterSimulation.NormalA;
                b = leakNow ? WaterSimulation.LeakB : WaterSimulation.NormalB;
            }
            else { a = Ctx.Received.latest[0]; b = Ctx.Received.latest[1]; }

            float[] ta = { a.pressure, a.flow, a.vibration };
            float[] tb = { b.pressure, b.flow, b.vibration };
            float k = Time.unscaledDeltaTime * 3.5f;
            for (int i = 0; i < 3; i++)
            {
                dispA[i] = Mathf.Lerp(dispA[i], ta[i], k);
                dispB[i] = Mathf.Lerp(dispB[i], tb[i], k);
                SetBar(barA[i], dispA[i] / BarMax[i]);
                SetBar(barB[i], dispB[i] / BarMax[i]);
                valA[i].text = dispA[i].ToString("0.0");
                valB[i].text = dispB[i].ToString("0.0");
            }

            float[] diffs = { dispA[0] - dispB[0], dispA[1] - dispB[1] };
            float[] lo = { 0.15f, 0.3f }, hi = { 0.45f, 0.7f }, range = { 1.0f, 2.4f };
            for (int i = 0; i < 2; i++)
            {
                markerPos[i] = Mathf.Lerp(markerPos[i], Mathf.Clamp01(diffs[i] / range[i]), k);
                var mr = marker[i].rectTransform;
                mr.anchoredPosition = new Vector2(markerPos[i] * 340f, 0f);
                bool inBand = diffs[i] >= lo[i] - 0.001f && diffs[i] <= hi[i] + 0.001f;
                marker[i].color = inBand ? Good : Bad;
                barB[i].color = inBand ? ColB : Bad;
            }

            bool alert = Ctx.Story != null && Ctx.Story.AlertsEnabled && Ctx.Detector.Detected;
            segmentHighlight.enabled = alert;
            pin.enabled = alert && t > 5.4f;
            if (pin.enabled)
            {
                pinDrop = Mathf.MoveTowards(pinDrop, 1f, Time.unscaledDeltaTime * 2.2f);
                float bounce = Mathf.Abs(Mathf.Sin(pinDrop * Mathf.PI * 1.5f)) * (1f - pinDrop) * 30f;
                UIFactory.TopLeft(pinRect, pinX, pinBaseY - (1f - pinDrop) * 70f - bounce, 34f, 34f);
            }
            else pinDrop = 0f;
        }

        void SetBar(Image img, float frac)
        {
            float h = Mathf.Max(3f, Mathf.Clamp01(frac) * MaxBar);
            var r = img.rectTransform;
            r.sizeDelta = new Vector2(96f, h);
            r.anchoredPosition = new Vector2(r.anchoredPosition.x, -(Baseline - h));
        }
    }
}
