using System.Collections.Generic;
using UnityEngine;

namespace LeakWatch
{
    /// Flow streaks inside the pipe: pooled elongated spheres moved along the pipe path by
    /// script. Speed follows the local flow rate; after UB they are slower and sparser during a leak.
    public class PipeFlow : MonoBehaviour
    {
        class Streak
        {
            public Transform tr;
            public float s, angle, rad, rnd, speedMul, len;
        }

        CurvePath path;
        readonly List<Streak> streaks = new List<Streak>();
        const int Count = 110;
        const float BaseSpeed = 6f;

        public void Init(CurvePath p, float pipeRadius)
        {
            path = p;
            var mat = MaterialFactory.CreateUnlitTransparent(new Color(0.88f, 0.96f, 1f, 0.9f));
            mat.renderQueue = 3002;
            pipeR = pipeRadius;

            for (int i = 0; i < Count; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "FlowStreak";
                go.transform.SetParent(transform, false);
                go.GetComponent<Renderer>().sharedMaterial = mat;
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                var st = new Streak { tr = go.transform, s = Random.value * path.Length };
                Randomize(st);
                streaks.Add(st);
            }
        }

        float pipeR;

        void Randomize(Streak st)
        {
            st.angle = Random.value * Mathf.PI * 2f;
            st.rad = Mathf.Sqrt(Random.value) * 0.62f;
            st.rnd = Random.value;
            st.speedMul = Random.Range(0.8f, 1.2f);
            st.len = Random.Range(0.8f, 1.5f);
        }

        float FlowAtX(float x, StationReading a, StationReading b)
        {
            float t = Mathf.InverseLerp(NetworkLayout.UbX - 7f, NetworkLayout.UbX + 7f, x);
            return Mathf.Lerp(a.flow, b.flow, t);
        }

        void Update()
        {
            if (path == null || Ctx.Sim == null) return;
            var a = Ctx.Sim.Truth(0);
            var b = Ctx.Sim.Truth(1);
            float dt = Time.deltaTime;

            foreach (var st in streaks)
            {
                Vector3 p = path.Evaluate(st.s);
                float flow = FlowAtX(p.x, a, b);
                float density = Mathf.Clamp01(1f - (12f - flow) * 0.25f);

                st.s += BaseSpeed * (flow / 12f) * st.speedMul * dt;
                if (st.s > path.Length)
                {
                    st.s -= path.Length;
                    Randomize(st);
                }

                p = path.Evaluate(st.s);
                Vector3 tan = path.Tangent(st.s);
                Vector3 bin = Vector3.forward;
                Vector3 nor = Vector3.Cross(tan, bin);
                Vector3 off = (nor * Mathf.Cos(st.angle) + bin * Mathf.Sin(st.angle)) * (st.rad * pipeR);

                st.tr.position = p + off;
                st.tr.rotation = Quaternion.FromToRotation(Vector3.right, tan);

                float vis = Mathf.Clamp01((density - st.rnd) * 6f);
                st.tr.localScale = new Vector3(st.len * Mathf.Max(vis, 0.0001f), 0.14f * vis + 0.0001f, 0.14f * vis + 0.0001f);
            }
        }
    }
}
