using UnityEngine;

namespace LeakWatch
{
    /// Rule-based detection from the data the dashboard has received. Compares the
    /// A-to-B difference with the normal baseline difference (no ML model involved).
    public class LeakDetector
    {
        public const float BaselinePressureDrop = 0.3f;
        public const float BaselineFlowDrop = 0.5f;

        public bool Detected { get; private set; }
        int agreeStreak;
        int clearStreak;

        public static float PressureDrop(StationReading a, StationReading b) => a.pressure - b.pressure;
        public static float FlowDrop(StationReading a, StationReading b) => a.flow - b.flow;

        public void Evaluate(ReceivedData data)
        {
            if (!data.HasBoth) { Reset(); return; }

            var a = data.latest[0];
            var b = data.latest[1];

            bool pressureAgrees = PressureDrop(a, b) - BaselinePressureDrop > 0.2f;
            bool flowAgrees = FlowDrop(a, b) - BaselineFlowDrop > 0.6f;
            bool vibrationAgrees = Mathf.Max(a.vibration, b.vibration) > 0.4f;

            if (pressureAgrees && flowAgrees && vibrationAgrees)
            {
                agreeStreak++; clearStreak = 0;
            }
            else
            {
                clearStreak++; agreeStreak = 0;
            }

            if (agreeStreak >= 2) Detected = true;
            if (clearStreak >= 2) Detected = false;
        }

        public void Reset()
        {
            Detected = false;
            agreeStreak = 0;
            clearStreak = 0;
        }
    }
}
