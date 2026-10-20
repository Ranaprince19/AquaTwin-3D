using System;
using System.Collections.Generic;

namespace LeakWatch
{
    /// What the monitoring dashboard has actually received. Updated only when a packet arrives.
    public class ReceivedData
    {
        public readonly StationReading[] latest = new StationReading[2];
        public readonly bool[] hasData = new bool[2];
        public readonly Queue<float>[] pressureHistory = { new Queue<float>(), new Queue<float>() };
        public readonly Queue<float>[] flowHistory = { new Queue<float>(), new Queue<float>() };
        public readonly Queue<float>[] vibrationHistory = { new Queue<float>(), new Queue<float>() };

        public event Action<int> OnArrived;
        public event Action<int, int> OnFieldArrived;
        readonly int[] mask = new int[2];
        public event Action OnCleared;

        public const int HistoryLength = 40;

        public bool HasBoth => hasData[0] && hasData[1];

        public void Receive(int station, StationReading r)
        {
            latest[station] = r;
            mask[station] = 7;
            hasData[station] = true;
            Push(pressureHistory[station], r.pressure);
            Push(flowHistory[station], r.flow);
            Push(vibrationHistory[station], r.vibration);
            OnArrived?.Invoke(station);
        }

        /// One sensor's value reaches the monitor (k: 0 pressure, 1 flow, 2 vibration).
        public void ReceiveField(int station, int k, StationReading r)
        {
            var cur = latest[station];
            cur.tag = r.tag;
            if (k == 0) { cur.pressure = r.pressure; Push(pressureHistory[station], r.pressure); }
            else if (k == 1) { cur.flow = r.flow; Push(flowHistory[station], r.flow); }
            else { cur.vibration = r.vibration; Push(vibrationHistory[station], r.vibration); }
            latest[station] = cur;
            mask[station] |= 1 << k;
            hasData[station] = mask[station] == 7;
            OnFieldArrived?.Invoke(station, k);
        }

        public void Clear()
        {
            for (int i = 0; i < 2; i++)
            {
                mask[i] = 0;
                hasData[i] = false;
                pressureHistory[i].Clear();
                flowHistory[i].Clear();
                vibrationHistory[i].Clear();
            }
            OnCleared?.Invoke();
        }

        static void Push(Queue<float> q, float v)
        {
            q.Enqueue(v);
            while (q.Count > HistoryLength) q.Dequeue();
        }
    }
}
