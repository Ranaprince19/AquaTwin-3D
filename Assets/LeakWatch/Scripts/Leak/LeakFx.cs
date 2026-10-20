using System.Collections.Generic;
using UnityEngine;

namespace LeakWatch
{
    /// The small leak inside UB's basement: lower front wall cut away, hairline crack,
    /// thin spray with drips, growing puddle, and vibration ripples along the pipe.
    public class LeakFx : MonoBehaviour
    {
        class Ripple { public LineRenderer lr; public float x; public int dir; public float strength; public bool pulsed; }

        Transform lowerWallRoot;
        GameObject upperWall;
        LineRenderer crack;
        ParticleSystem spray, drips;
        Transform puddle;
        Material puddleMat;
        readonly List<Ripple> ripples = new List<Ripple>();
        readonly List<Ripple> pool = new List<Ripple>();

        bool active;
        float tLeak;
        float rippleTimer;
        Material rippleMat;

        static readonly Vector3 CrackPos = new Vector3(NetworkLayout.LeakX, NetworkLayout.PipeY - 0.55f, 0.95f);
        const float FloorY = -6.35f;

        public void Build(Transform parent)
        {
            transform.SetParent(parent, false);
            var concrete = MaterialFactory.CreateOpaque(new Color(0.6f, 0.6f, 0.63f), 0f, 0.15f);
            var b = NetworkLayout.Basement;
            float cx = (b.x0 + b.x1) * 0.5f;
            float w = b.x1 - b.x0;
            float z = NetworkLayout.BasementWallZ;

            float slotBottom = NetworkLayout.PipeY - NetworkLayout.PipeR - 0.25f;
            float slotTop = NetworkLayout.PipeY + NetworkLayout.PipeR + 0.25f;

            upperWall = Prim.BoxMinMax(transform, new Vector3(b.x0, slotTop, z), new Vector3(b.x1, b.y1, z + 0.2f), concrete, "BasementWallUpper");

            var lowerRoot = new GameObject("BasementWallLower");
            lowerRoot.transform.SetParent(transform, false);
            lowerRoot.transform.position = new Vector3(cx, FloorY, z + 0.1f);
            Prim.Box(lowerRoot.transform, new Vector3(0f, (slotBottom - FloorY) * 0.5f, 0f), new Vector3(w, slotBottom - FloorY, 0.2f), concrete, "WallBox");
            lowerWallRoot = lowerRoot.transform;

            var crackGo = new GameObject("Crack");
            crackGo.transform.SetParent(transform, false);
            crack = crackGo.AddComponent<LineRenderer>();
            crack.useWorldSpace = true;
            crack.positionCount = 5;
            crack.startWidth = 0.035f; crack.endWidth = 0.02f;
            crack.material = MaterialFactory.LineMaterial();
            crack.startColor = new Color(0.04f, 0.04f, 0.05f); crack.endColor = new Color(0.04f, 0.04f, 0.05f);
            Vector3 c = CrackPos;
            crack.SetPositions(new[]
            {
                c + new Vector3(-0.22f, 0.32f, 0.04f), c + new Vector3(-0.05f, 0.12f, 0.02f), c,
                c + new Vector3(0.1f, -0.15f, -0.05f), c + new Vector3(0.0f, -0.32f, -0.12f)
            });
            crack.enabled = false;

            var tex = TextureFactory.SoftCircle();
            spray = BuildParticles("Spray", c, new Vector3(0f, -0.15f, 1f), 18f, 0.9f, 1.0f, 0.06f, 0.9f, 1f);
            drips = BuildParticles("Drips", c + new Vector3(0f, -0.05f, 0.05f), new Vector3(0f, -1f, 0.3f), 20f, 0.3f, 1.3f, 0.11f, 1.6f, 1f);
            var sr = spray.GetComponent<ParticleSystemRenderer>(); sr.sharedMaterial = MaterialFactory.CreateParticleMaterial(tex);
            var dr = drips.GetComponent<ParticleSystemRenderer>(); dr.sharedMaterial = MaterialFactory.CreateParticleMaterial(tex);

            puddleMat = MaterialFactory.CreateTransparent(new Color(0.25f, 0.5f, 0.8f, 0.7f), 0f, 0.95f);
            puddleMat.renderQueue = 3008;
            puddle = Prim.Make(PrimitiveType.Cylinder, transform, new Vector3(NetworkLayout.LeakX, FloorY + 0.03f, 1.55f), new Vector3(0.01f, 0.02f, 0.01f), puddleMat, "Puddle").transform;
            puddle.gameObject.SetActive(false);

            rippleMat = MaterialFactory.LineMaterial();
        }

        ParticleSystem BuildParticles(string name, Vector3 pos, Vector3 dir, float angle, float speed, float life, float size, float gravity, float rate)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(dir.normalized);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = life;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.8f, speed * 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size * 1.2f);
            main.startColor = new Color(0.8f, 0.92f, 1f, 0.9f);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = angle * 0.3f;
            shape.radius = 0.01f;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.7f, 0.85f, 1f), 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            return ps;
        }

        void SetRate(ParticleSystem ps, float rate)
        {
            var e = ps.emission;
            e.rateOverTime = rate;
            if (rate > 0.01f && !ps.isPlaying) ps.Play();
            if (rate <= 0.01f && ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        public void Snap() { tLeak = Mathf.Max(tLeak, 8f); }

        void Update()
        {
            if (Ctx.Sim == null) return;
            bool leaking = Ctx.Sim.IsLeaking;

            if (leaking && !active) { active = true; tLeak = 0f; rippleTimer = 0.2f; }
            if (!leaking && active) { active = false; ClearAll(); }

            if (active)
            {
                tLeak += Time.deltaTime;

                float wall = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tLeak / 1.5f));
                lowerWallRoot.localScale = new Vector3(1f, Mathf.Max(0.001f, 1f - wall), 1f);

                crack.enabled = tLeak > 0.8f;
                float sprayT = Mathf.Clamp01((tLeak - 1.4f) / 1.0f);
                SetRate(spray, 55f * sprayT);
                SetRate(drips, 5f * sprayT);

                if (tLeak > 2.2f)
                {
                    puddle.gameObject.SetActive(true);
                    float rad = Mathf.Lerp(0.15f, 1.0f, Mathf.Clamp01((tLeak - 2.2f) / 25f));
                    puddle.localScale = new Vector3(rad * 2.2f, 0.02f, rad * 1.6f);
                }

                rippleTimer -= Time.deltaTime;
                if (rippleTimer <= 0f)
                {
                    rippleTimer = 1.0f;
                    SpawnRipple(-1, 0.6f);
                    SpawnRipple(+1, 0.9f);
                }
            }

            UpdateRipples();
        }

        void ClearAll()
        {
            lowerWallRoot.localScale = Vector3.one;
            crack.enabled = false;
            SetRate(spray, 0f); SetRate(drips, 0f);
            spray.Clear(); drips.Clear();
            puddle.gameObject.SetActive(false);
            foreach (var r in ripples) { r.lr.enabled = false; pool.Add(r); }
            ripples.Clear();
        }

        void SpawnRipple(int dir, float strength)
        {
            Ripple r;
            if (pool.Count > 0) { r = pool[pool.Count - 1]; pool.RemoveAt(pool.Count - 1); }
            else
            {
                var go = new GameObject("VibrationRipple");
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true; lr.loop = true; lr.positionCount = 36;
                lr.material = rippleMat;
                r = new Ripple { lr = lr };
            }
            r.x = NetworkLayout.LeakX; r.dir = dir; r.strength = strength; r.pulsed = false;
            r.lr.enabled = true;
            ripples.Add(r);
        }

        void UpdateRipples()
        {
            for (int i = ripples.Count - 1; i >= 0; i--)
            {
                var r = ripples[i];
                r.x += r.dir * 8f * Time.deltaTime;

                float stationX = r.dir < 0 ? NetworkLayout.StationAX : NetworkLayout.StationBX;
                if (!r.pulsed && ((r.dir < 0 && r.x <= stationX) || (r.dir > 0 && r.x >= stationX)))
                {
                    r.pulsed = true;
                    int idx = r.dir < 0 ? 0 : 1;
                    if (Ctx.Stations[idx] != null) Ctx.Stations[idx].VibrationPulse(r.strength);
                }

                float endX = r.dir < 0 ? NetworkLayout.StationAX - 6f : NetworkLayout.StationBX + 6f;
                bool done = (r.dir < 0 && r.x < endX) || (r.dir > 0 && r.x > endX);
                if (done) { r.lr.enabled = false; pool.Add(r); ripples.RemoveAt(i); continue; }

                float dist = Mathf.Abs(r.x - NetworkLayout.LeakX);
                float a = r.strength * Mathf.Clamp01(1f - dist / 26f);
                var col = new Color(1f, 0.6f, 0.15f, Mathf.Clamp01(a));
                r.lr.startColor = col; r.lr.endColor = col;
                r.lr.startWidth = r.lr.endWidth = 0.05f + 0.1f * r.strength;
                float rad = NetworkLayout.PipeR * 1.2f;
                for (int k = 0; k < 36; k++)
                {
                    float ang = k * Mathf.PI * 2f / 36f;
                    r.lr.SetPosition(k, new Vector3(r.x, NetworkLayout.PipeY + Mathf.Cos(ang) * rad, Mathf.Sin(ang) * rad));
                }
            }
        }
    }
}
