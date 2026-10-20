using UnityEngine;

namespace LeakWatch
{
    /// Plays the six-scene story (supply, measure, collect, send, leak, analyze) with smooth camera moves.
    public class StoryController : MonoBehaviour
    {
        public int SceneIndex { get; private set; } = -1;
        public float SceneTime { get; private set; }
        public bool Autoplay { get; set; } = true;
        public bool CaptureMode { get; set; }
        public bool AlertsEnabled => manualLeak || (SceneIndex == 5 && SceneTime > 4.2f);

        bool manualLeak;

        public const int SceneCount = 6;
        public static readonly string[] Captions =
        {
            "Pump supplies water to UB and TP2",
            "Four sensors measure the pipe",
            "Each sensor packs its value with location",
            "Wi-Fi carries packets to the monitor",
            "A small leak starts inside UB",
            "Comparing both stations locates the leak"
        };
        static readonly float[] Durations = { 9f, 16f, 11f, 13f, 12f, 13f };
        const float SensorTurn = 3.0f;

        void Update()
        {
            if (SceneIndex < 0) return;

            if (InputHelper.KeyDown(LWKey.Space)) GoTo(Mathf.Min(SceneIndex + 1, SceneCount - 1));
            if (InputHelper.KeyDown(LWKey.B)) GoTo(Mathf.Max(SceneIndex - 1, 0));
            if (InputHelper.KeyDown(LWKey.A)) Autoplay = !Autoplay;
            if (InputHelper.KeyDown(LWKey.L)) { Ctx.Sim.SetLeak(true); manualLeak = true; }
            if (InputHelper.KeyDown(LWKey.R)) { Ctx.Sim.SetLeak(false); manualLeak = false; }

            SceneTime += Time.deltaTime;

            if (Autoplay && !CaptureMode)
            {
                if (SceneIndex < SceneCount - 1 && SceneTime >= Durations[SceneIndex]) GoTo(SceneIndex + 1);
                else if (SceneIndex == SceneCount - 1 && SceneTime >= Durations[SceneIndex] + 3f) GoTo(0);
            }

            UpdateScene();
        }

        public void GoTo(int i, bool jump = false)
        {
            i = Mathf.Clamp(i, 0, SceneCount - 1);
            SceneIndex = i;
            SceneTime = 0f;
            manualLeak = false;
            Ctx.Hud.SetCaption(Captions[i]);

            if (i >= 4)
            {
                bool snap = jump && i == 5;
                Ctx.Sim.SetLeak(true, snap);
                if (snap) Ctx.Leak.Snap();
            }
            else Ctx.Sim.SetLeak(false, true);

            if (i <= 3)
            {
                Ctx.Received.Clear();
                Ctx.Detector.Reset();
            }
            else if (!Ctx.Received.HasBoth)
            {
                Ctx.Received.Receive(0, Ctx.Sim.Sample(0));
                Ctx.Received.Receive(1, Ctx.Sim.Sample(1));
                Ctx.Detector.Evaluate(Ctx.Received);
                Ctx.Detector.Evaluate(Ctx.Received);
            }

            Ctx.Packets.ClearAll();
            Ctx.Packets.CurrentMode = i <= 1 ? PacketPipeline.Mode.Off : (i == 2 ? PacketPipeline.Mode.CollectOnly : PacketPipeline.Mode.Full);

            Ctx.Dashboard.SetVisible(i >= 3);
            Ctx.Analysis.SetVisible(i == 5);

            bool showExhibits = i == 1 || i == 2;
            foreach (var st in Ctx.Stations) st.SetExhibitsShown(showExhibits);
            Ctx.Stations[0].SetFocus(-1);

            Ctx.Rig.SetShot(ShotFor(i, 0f), jump);
        }

        void UpdateScene()
        {
            if (SceneIndex == 1 || SceneIndex == 2) TickExhibits();
            Ctx.Rig.SetShot(ShotFor(SceneIndex, SceneTime), false);
        }

        void TickExhibits()
        {
            bool measure = SceneIndex == 1;
            bool wide = measure && SceneTime >= 4f * SensorTurn;
            int stage = measure && !wide ? Mathf.Min(3, Mathf.FloorToInt(SceneTime / SensorTurn)) : -1;
            Ctx.Stations[0].SetFocus(stage);

            for (int s = 0; s < 2; s++)
            {
                var st = Ctx.Stations[s];
                var truth = Ctx.Sim.Truth(s);
                for (int k = 0; k < 4; k++)
                {
                    float start = k * SensorTurn + 1.3f;
                    bool value = !measure || SceneTime >= start;
                    float pop = measure ? SceneTime - start : 9f;
                    float emph = measure ? ((s == 0 && k == stage) ? 1f : (wide ? 0.5f : 0f)) : 0.5f;
                    st.Exhibits[k].Tick(truth, emph, value, pop);
                }
            }
        }

        Shot ShotFor(int i, float t)
        {
            switch (i)
            {
                case 0:
                    return new Shot(new Vector3(9f, -1.5f, 0f), 165f + 8f * Mathf.Clamp01(t / Durations[0]), 13f, 104f, Vector2.zero);
                case 1:
                    {
                        var st = Ctx.Stations[0];
                        if (t >= 4f * SensorTurn) return new Shot(new Vector3(st.X, 0.3f, 2.4f), 168f, 10f, 28f, Vector2.zero);
                        int stage = Mathf.Clamp(Mathf.FloorToInt(t / SensorTurn), 0, 3);
                        return new Shot(new Vector3(st.DeviceTops[stage].x, 0.3f, 2.4f), 168f, 9f, 13f, Vector2.zero);
                    }
                case 2:
                    return new Shot(new Vector3(NetworkLayout.StationAX, 0.0f, 1.5f), 170f, 10f, 26f, Vector2.zero);
                case 3:
                    return new Shot(new Vector3(-5.5f, 3.5f, 0f), 165f, 9f, 28f, new Vector2(-0.167f, 0f));
                case 4:
                    if (t < 5.5f) return new Shot(new Vector3(NetworkLayout.LeakX, -4.0f, 1.4f), 172f, 8f, 13.5f, Vector2.zero);
                    return new Shot(new Vector3(6f, -1f, 0f), 168f, 11f, 96f, new Vector2(-0.167f, 0f));
                default:
                    return new Shot(new Vector3(6f, 2f, 0f), 168f, 12f, 96f, new Vector2(-0.167f, 0.32f));
            }
        }
    }
}
