using System.Collections.Generic;
using UnityEngine;

namespace LeakWatch
{
    /// Smooth Catmull-Rom path through control points with arc-length lookup.
    public class CurvePath
    {
        readonly List<Vector3> pts = new List<Vector3>();
        readonly List<float> cum = new List<float>();
        public float Length { get; private set; }
        public IReadOnlyList<Vector3> Samples => pts;

        public CurvePath(IList<Vector3> control, int subdivisions = 12)
        {
            int n = control.Count;
            if (n < 2) { pts.Add(control[0]); cum.Add(0f); return; }

            for (int i = 0; i < n - 1; i++)
            {
                Vector3 p0 = control[Mathf.Max(i - 1, 0)];
                Vector3 p1 = control[i];
                Vector3 p2 = control[i + 1];
                Vector3 p3 = control[Mathf.Min(i + 2, n - 1)];
                for (int k = 0; k < subdivisions; k++)
                {
                    float t = k / (float)subdivisions;
                    pts.Add(CatmullRom(p0, p1, p2, p3, t));
                }
            }
            pts.Add(control[n - 1]);

            cum.Add(0f);
            for (int i = 1; i < pts.Count; i++)
                cum.Add(cum[i - 1] + Vector3.Distance(pts[i - 1], pts[i]));
            Length = cum[cum.Count - 1];
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t +
                           (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                           (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        int IndexAt(float s)
        {
            int lo = 0, hi = cum.Count - 1;
            while (lo < hi - 1)
            {
                int mid = (lo + hi) / 2;
                if (cum[mid] <= s) lo = mid; else hi = mid;
            }
            return lo;
        }

        public Vector3 Evaluate(float s)
        {
            if (pts.Count == 1) return pts[0];
            s = Mathf.Clamp(s, 0f, Length);
            int i = IndexAt(s);
            float seg = cum[i + 1] - cum[i];
            float t = seg > 0.0001f ? (s - cum[i]) / seg : 0f;
            return Vector3.Lerp(pts[i], pts[i + 1], t);
        }

        public Vector3 Tangent(float s)
        {
            if (pts.Count == 1) return Vector3.right;
            s = Mathf.Clamp(s, 0f, Length);
            int i = IndexAt(s);
            Vector3 d = pts[i + 1] - pts[i];
            return d.sqrMagnitude > 1e-8f ? d.normalized : Vector3.right;
        }

        /// Arc-length of the sample point closest to the given world point.
        public float NearestS(Vector3 p)
        {
            float best = float.MaxValue; int bi = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                float d = (pts[i] - p).sqrMagnitude;
                if (d < best) { best = d; bi = i; }
            }
            return cum[bi];
        }

        /// Vertical extent of the path samples whose x lies in [x0,x1].
        public bool RangeY(float x0, float x1, out float yMin, out float yMax)
        {
            yMin = float.MaxValue; yMax = float.MinValue;
            bool any = false;
            for (int i = 0; i < pts.Count; i++)
            {
                if (pts[i].x < x0 - 0.001f || pts[i].x > x1 + 0.001f) continue;
                any = true;
                yMin = Mathf.Min(yMin, pts[i].y);
                yMax = Mathf.Max(yMax, pts[i].y);
            }
            return any;
        }

        public void SampleEvenly(float step, out List<Vector3> pos, out List<Vector3> tan)
        {
            pos = new List<Vector3>(); tan = new List<Vector3>();
            int count = Mathf.Max(2, Mathf.CeilToInt(Length / step) + 1);
            for (int i = 0; i < count; i++)
            {
                float s = Length * i / (count - 1);
                pos.Add(Evaluate(s));
                tan.Add(Tangent(Mathf.Clamp(s, 0.001f, Length - 0.001f)));
            }
        }
    }
}
