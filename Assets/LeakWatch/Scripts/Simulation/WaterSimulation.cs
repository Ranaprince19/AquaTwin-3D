using System;
using UnityEngine;

namespace LeakWatch
{
    /// Simulated truth for Station A (index 0, UB-IN) and Station B (index 1, TP2-IN).
    /// Smooth values drive animations; noisy samples are taken once per second.
    public class WaterSimulation : MonoBehaviour
    {
        public static readonly StationReading NormalA = new StationReading { tag = "UB-IN", pressure = 4.2f, flow = 12.0f, vibration = 0.1f };
        public static readonly StationReading NormalB = new StationReading { tag = "TP2-IN", pressure = 3.9f, flow = 11.5f, vibration = 0.1f };
        public static readonly StationReading LeakA = new StationReading { tag = "UB-IN", pressure = 4.1f, flow = 12.1f, vibration = 0.6f };
        public static readonly StationReading LeakB = new StationReading { tag = "TP2-IN", pressure = 3.4f, flow = 10.3f, vibration = 0.9f };

        public bool IsLeaking { get; private set; }
        public float LeakBlend { get; private set; }

        public event Action<int, StationReading> OnSample;

        float sampleTimer;
        const float SampleInterval = 1f;
        const float BlendTime = 1.2f;

        void Update()
        {
            float target = IsLeaking ? 1f : 0f;
            LeakBlend = Mathf.MoveTowards(LeakBlend, target, Time.deltaTime / BlendTime);

            sampleTimer += Time.deltaTime;
            if (sampleTimer >= SampleInterval)
            {
                sampleTimer -= SampleInterval;
                OnSample?.Invoke(0, Sample(0));
                OnSample?.Invoke(1, Sample(1));
            }
        }

        public StationReading Truth(int station)
        {
            var n = station == 0 ? NormalA : NormalB;
            var l = station == 0 ? LeakA : LeakB;
            return new StationReading
            {
                tag = n.tag,
                pressure = Mathf.Lerp(n.pressure, l.pressure, LeakBlend),
                flow = Mathf.Lerp(n.flow, l.flow, LeakBlend),
                vibration = Mathf.Lerp(n.vibration, l.vibration, LeakBlend)
            };
        }

        public StationReading Sample(int station)
        {
            var r = Truth(station);
            r.pressure = Round1(Mathf.Max(0f, r.pressure + UnityEngine.Random.Range(-0.03f, 0.03f)));
            r.flow = Round1(Mathf.Max(0f, r.flow + UnityEngine.Random.Range(-0.04f, 0.04f)));
            r.vibration = Round1(Mathf.Max(0f, r.vibration + UnityEngine.Random.Range(-0.03f, 0.03f)));
            return r;
        }

        static float Round1(float v) => Mathf.Round(v * 10f) / 10f;

        public void SetLeak(bool leaking, bool snap = false)
        {
            IsLeaking = leaking;
            if (snap) LeakBlend = leaking ? 1f : 0f;
        }
    }
}
