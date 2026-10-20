using System.Collections.Generic;
using UnityEngine;

namespace LeakWatch
{
    /// Working mechanism of ONE sensor, always visible and animating while shown.
    /// The active sensor is enlarged and highlighted; the others stay small. Casing is translucent.
    public class ExhibitView : MonoBehaviour
    {
        public static readonly Color PressureColor = new Color(0.2f, 0.5f, 1f);
        public static readonly Color FlowColor = new Color(0.25f, 0.85f, 0.35f);
        public static readonly Color VibrationColor = new Color(1f, 0.58f, 0.12f);
        public static readonly Color LocationColor = new Color(0.68f, 0.35f, 0.95f);

        public static Color ColorOf(SensorType t)
        {
            switch (t)
            {
                case SensorType.Pressure: return PressureColor;
                case SensorType.Flow: return FlowColor;
                case SensorType.Vibration: return VibrationColor;
                default: return LocationColor;
            }
        }

        public static string NameOf(SensorType t)
        {
            switch (t)
            {
                case SensorType.Pressure: return "Pressure";
                case SensorType.Flow: return "Flow Rate";
                case SensorType.Vibration: return "Vibration";
                default: return "Location";
            }
        }

        const float IdleScale = 0.55f, ActiveScale = 0.9f;

        SensorType type;
        string stationTag;
        Vector3 center;
        Vector3 deviceTop;
        GameObject group;
        LineRenderer beam;
        ScreenLabel valueLabel, nameLabel;
        float emphasis, appear;
        bool shown;

        // pressure
        Mesh diaphragmMesh; Vector3[] diaphragmBase; int ringCount = 6, segCount = 20;
        Transform strainBars; Material strainMat; Transform[] arrows = new Transform[3];
        // flow
        Transform rotor; Material hallMat; float rotorAngle; float hallFlash;
        List<Transform> tubeStreaks = new List<Transform>(); List<float> streakX = new List<float>();
        // vibration
        Transform mass, crystal; Material crystalMat;
        // location
        Transform pin; LineRenderer[] pulseRings = new LineRenderer[2];

        public static Material Casing(Color c) { var m = MaterialFactory.CreateTransparent(new Color(c.r, c.g, c.b, 0.2f), 0.2f, 0.9f); m.renderQueue = 3010; return m; }
        static float Round1(float v) => Mathf.Round(v * 10f) / 10f;

        public void Build(SensorType sensor, Vector3 centerWorld, string tag, Vector3 deviceTopWorld)
        {
            type = sensor; center = centerWorld; stationTag = tag; deviceTop = deviceTopWorld;
            transform.position = centerWorld;

            group = new GameObject("Mechanism_" + sensor);
            group.transform.SetParent(transform, false);
            switch (sensor)
            {
                case SensorType.Pressure: BuildPressure(group.transform); break;
                case SensorType.Flow: BuildFlow(group.transform); break;
                case SensorType.Vibration: BuildVibration(group.transform); break;
                default: BuildLocation(group.transform); break;
            }

            Color col = ColorOf(sensor);
            var dark = new Color(col.r * 0.5f, col.g * 0.5f, col.b * 0.5f, 0.93f);
            valueLabel = LabelLayer.Create("--", 44, Color.white, dark, centerWorld, Vector2.zero);
            nameLabel = LabelLayer.Create(NameOf(sensor), 30, Color.white, dark, centerWorld, Vector2.zero);

            var beamGo = new GameObject("Beam");
            beamGo.transform.SetParent(transform, false);
            beam = beamGo.AddComponent<LineRenderer>();
            beam.useWorldSpace = true; beam.positionCount = 2;
            beam.startWidth = 0.05f; beam.endWidth = 0.05f;
            beam.material = MaterialFactory.LineMaterial();
            beam.enabled = false;

            group.SetActive(false);
        }

        // ---------- Pressure ----------
        void BuildPressure(Transform tr)
        {
            var metal = MaterialFactory.CreateOpaque(new Color(0.25f, 0.28f, 0.34f), 0.7f, 0.5f);
            Prim.Make(PrimitiveType.Cylinder, tr, new Vector3(0f, 0.2f, 0f), new Vector3(2f, 1.2f, 2f), Casing(PressureColor), "Casing");
            Prim.Make(PrimitiveType.Cylinder, tr, new Vector3(0f, 1.42f, 0f), new Vector3(2.1f, 0.05f, 2.1f), metal, "RingTop");
            Prim.Make(PrimitiveType.Cylinder, tr, new Vector3(0f, -1.02f, 0f), new Vector3(2.1f, 0.05f, 2.1f), metal, "RingBottom");
            var water = MaterialFactory.CreateTransparent(new Color(0.2f, 0.55f, 1f, 0.55f), 0f, 0.9f);
            water.renderQueue = 3005;
            Prim.Make(PrimitiveType.Cylinder, tr, new Vector3(0f, -0.78f, 0f), new Vector3(1.9f, 0.22f, 1.9f), water, "Water");

            var arrowMat = MaterialFactory.CreateOpaque(new Color(0.85f, 0.95f, 1f));
            MaterialFactory.SetEmission(arrowMat, new Color(0.5f, 0.8f, 1f), 1.5f);
            for (int i = 0; i < 3; i++)
                arrows[i] = Prim.Make(PrimitiveType.Cylinder, tr, new Vector3((i - 1) * 0.5f, -0.75f, 0f), new Vector3(0.12f, 0.2f, 0.12f), arrowMat, "Arrow").transform;

            diaphragmMesh = new Mesh { name = "Diaphragm" };
            diaphragmMesh.MarkDynamic();
            var verts = new List<Vector3> { Vector3.zero };
            for (int r = 1; r <= ringCount; r++)
                for (int s = 0; s < segCount; s++)
                {
                    float a = s * Mathf.PI * 2f / segCount;
                    float rad = 0.92f * r / ringCount;
                    verts.Add(new Vector3(Mathf.Cos(a) * rad, 0f, Mathf.Sin(a) * rad));
                }
            diaphragmBase = verts.ToArray();
            var tris = new List<int>();
            for (int s = 0; s < segCount; s++) { tris.Add(0); tris.Add(1 + (s + 1) % segCount); tris.Add(1 + s); }
            for (int r = 1; r < ringCount; r++)
                for (int s = 0; s < segCount; s++)
                {
                    int a0 = 1 + (r - 1) * segCount + s, a1 = 1 + (r - 1) * segCount + (s + 1) % segCount;
                    int b0 = 1 + r * segCount + s, b1 = 1 + r * segCount + (s + 1) % segCount;
                    tris.Add(a0); tris.Add(a1); tris.Add(b1);
                    tris.Add(a0); tris.Add(b1); tris.Add(b0);
                }
            diaphragmMesh.SetVertices(verts);
            diaphragmMesh.SetTriangles(tris, 0);
            diaphragmMesh.RecalculateNormals();
            var diaMat = MaterialFactory.CreateOpaque(new Color(0.78f, 0.8f, 0.85f), 0.8f, 0.6f);
            if (diaMat.HasProperty("_Cull")) diaMat.SetFloat("_Cull", 0f);
            var diaGo = Prim.MeshObject(tr, diaphragmMesh, diaMat, "Diaphragm");
            diaGo.transform.localPosition = new Vector3(0f, -0.55f, 0f);

            strainMat = MaterialFactory.CreateOpaque(Color.green);
            var bars = new GameObject("StrainGauge");
            bars.transform.SetParent(diaGo.transform, false);
            for (int i = 0; i < 3; i++)
                Prim.Make(PrimitiveType.Cube, bars.transform, new Vector3(0f, 0.03f, (i - 1) * 0.14f), new Vector3(0.8f, 0.03f, 0.06f), strainMat, "GaugeBar");
            strainBars = bars.transform;
        }

        void TickPressure(StationReading r, float t)
        {
            float deflect = Mathf.Clamp(r.pressure * 0.1f, 0.05f, 0.7f) + Mathf.Sin(t * 9f) * 0.008f;
            var v = new Vector3[diaphragmBase.Length];
            for (int i = 0; i < v.Length; i++)
            {
                var b = diaphragmBase[i];
                float rr = Mathf.Sqrt(b.x * b.x + b.z * b.z) / 0.92f;
                v[i] = new Vector3(b.x, deflect * (1f - rr * rr), b.z);
            }
            diaphragmMesh.vertices = v;
            diaphragmMesh.RecalculateNormals();
            diaphragmMesh.RecalculateBounds();
            strainBars.localPosition = new Vector3(0f, deflect, 0f);
            float strain = Mathf.InverseLerp(0.1f, 0.5f, deflect);
            Color c = Color.Lerp(new Color(0.2f, 0.9f, 0.3f), new Color(1f, 0.25f, 0.1f), strain);
            strainMat.color = c;
            MaterialFactory.SetEmission(strainMat, c, 1.2f);
            for (int i = 0; i < 3; i++)
            {
                float len = 0.15f + r.pressure * 0.03f + Mathf.Sin(t * 5f + i) * 0.02f;
                arrows[i].localScale = new Vector3(0.12f, len, 0.12f);
                arrows[i].localPosition = new Vector3((i - 1) * 0.5f, -0.95f + len, 0f);
            }
        }

        // ---------- Flow ----------
        void BuildFlow(Transform tr)
        {
            var metal = MaterialFactory.CreateOpaque(new Color(0.2f, 0.3f, 0.22f), 0.7f, 0.5f);
            var tube = Prim.Make(PrimitiveType.Cylinder, tr, Vector3.zero, new Vector3(2f, 1.7f, 2f), Casing(FlowColor), "Casing");
            tube.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            foreach (float sx in new[] { -1.7f, 1.7f })
                Prim.MeshObject(tr, TubeMesh.Ring(new Vector3(sx, 0f, 0f), Vector3.right, 0.97f, 1.18f, 0.22f, "Ring"), metal, "Ring");

            var rotorGo = new GameObject("Rotor");
            rotorGo.transform.SetParent(tr, false);
            rotor = rotorGo.transform;
            var steel = MaterialFactory.CreateOpaque(new Color(0.8f, 0.82f, 0.88f), 0.8f, 0.6f);
            var hub = Prim.Make(PrimitiveType.Cylinder, rotor, Vector3.zero, new Vector3(0.32f, 0.25f, 0.32f), steel, "Hub");
            hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f, rad = a * Mathf.Deg2Rad;
                var blade = Prim.Make(PrimitiveType.Cube, rotor, new Vector3(0f, Mathf.Cos(rad) * 0.5f, Mathf.Sin(rad) * 0.5f), new Vector3(0.16f, 0.75f, 0.5f), steel, "Blade");
                blade.transform.localRotation = Quaternion.Euler(a, 0f, 0f);
            }
            var magnetMat = MaterialFactory.CreateOpaque(new Color(0.9f, 0.1f, 0.1f));
            MaterialFactory.SetEmission(magnetMat, new Color(1f, 0.2f, 0.2f), 1.5f);
            Prim.Make(PrimitiveType.Cube, rotor, new Vector3(0f, 0.92f, 0f), new Vector3(0.22f, 0.2f, 0.22f), magnetMat, "Magnet");

            Prim.Make(PrimitiveType.Cube, tr, new Vector3(0f, 1.25f, 0f), new Vector3(0.7f, 0.4f, 0.7f), MaterialFactory.CreateOpaque(new Color(0.12f, 0.14f, 0.16f), 0.4f, 0.5f), "HallPickup");
            hallMat = MaterialFactory.CreateOpaque(new Color(0.3f, 0.3f, 0.3f));
            Prim.Make(PrimitiveType.Sphere, tr, new Vector3(0f, 1.5f, 0f), Vector3.one * 0.25f, hallMat, "HallLed");

            var streakMat = MaterialFactory.CreateUnlitTransparent(new Color(0.85f, 0.97f, 1f, 0.7f));
            streakMat.renderQueue = 3006;
            for (int i = 0; i < 9; i++)
            {
                float sx = -1.5f + i * 0.375f;
                var s = Prim.Make(PrimitiveType.Sphere, tr, new Vector3(sx, Random.Range(-0.7f, 0.7f), Random.Range(-0.7f, 0.7f)), new Vector3(0.5f, 0.1f, 0.1f), streakMat, "Streak");
                tubeStreaks.Add(s.transform); streakX.Add(sx);
            }
        }

        void TickFlow(StationReading r, float dt)
        {
            float omega = r.flow / 12f * 300f;
            float prev = rotorAngle;
            rotorAngle += omega * dt;
            rotor.localRotation = Quaternion.Euler(rotorAngle, 0f, 0f);
            if (Mathf.Floor(rotorAngle / 360f) > Mathf.Floor(prev / 360f)) hallFlash = 1f;
            hallFlash = Mathf.MoveTowards(hallFlash, 0f, dt * 4f);
            hallMat.color = Color.Lerp(new Color(0.3f, 0.3f, 0.3f), new Color(1f, 0.95f, 0.3f), hallFlash);
            MaterialFactory.SetEmission(hallMat, new Color(1f, 0.9f, 0.2f), hallFlash * 3f);

            for (int i = 0; i < tubeStreaks.Count; i++)
            {
                streakX[i] += r.flow / 12f * 1.6f * dt;
                if (streakX[i] > 1.5f) streakX[i] = -1.5f;
                var p = tubeStreaks[i].localPosition; p.x = streakX[i]; tubeStreaks[i].localPosition = p;
            }
        }

        // ---------- Vibration ----------
        void BuildVibration(Transform tr)
        {
            Prim.Make(PrimitiveType.Cube, tr, new Vector3(0f, -0.9f, 0f), new Vector3(3.6f, 0.3f, 2.2f), MaterialFactory.CreateOpaque(new Color(0.18f, 0.25f, 0.36f), 0.65f, 0.5f), "PipeWall");
            var water = MaterialFactory.CreateTransparent(new Color(0.2f, 0.55f, 1f, 0.5f), 0f, 0.9f);
            water.renderQueue = 3005;
            Prim.Make(PrimitiveType.Cube, tr, new Vector3(0f, -1.3f, 0f), new Vector3(3.4f, 0.5f, 2f), water, "Water");
            Prim.Make(PrimitiveType.Cube, tr, new Vector3(0f, -0.7f, 0f), new Vector3(2.1f, 0.12f, 2.1f), MaterialFactory.CreateOpaque(new Color(0.2f, 0.2f, 0.22f), 0.6f, 0.4f), "BasePlate");
            Prim.Make(PrimitiveType.Cube, tr, new Vector3(0f, 0.3f, 0f), new Vector3(1.95f, 1.9f, 1.95f), Casing(VibrationColor), "Casing");

            crystalMat = MaterialFactory.CreateOpaque(new Color(1f, 0.85f, 0.25f), 0.2f, 0.8f);
            MaterialFactory.SetEmission(crystalMat, new Color(1f, 0.7f, 0.1f), 0.8f);
            crystal = Prim.Make(PrimitiveType.Cube, tr, new Vector3(0f, -0.45f, 0f), new Vector3(1.3f, 0.22f, 1.3f), crystalMat, "PiezoCrystal").transform;
            mass = Prim.Make(PrimitiveType.Cube, tr, new Vector3(0f, 0.1f, 0f), new Vector3(0.95f, 0.55f, 0.95f), MaterialFactory.CreateOpaque(new Color(0.55f, 0.57f, 0.62f), 0.85f, 0.6f), "Mass").transform;
        }

        void TickVibration(StationReading r, float t)
        {
            float amp = 0.04f + r.vibration * 0.35f;
            float s = Mathf.Sin(t * 26f) + 0.3f * Mathf.Sin(t * 41f);
            mass.localPosition = new Vector3(0f, 0.1f + amp * s * 0.8f, 0f);
            float squeeze = 1f - 0.35f * Mathf.Clamp(amp * s * 2f, -0.6f, 0.6f);
            crystal.localScale = new Vector3(1.3f, 0.22f * squeeze, 1.3f);
            MaterialFactory.SetEmission(crystalMat, new Color(1f, 0.7f, 0.1f), 0.5f + Mathf.Abs(s) * r.vibration * 2.5f);
        }

        // ---------- Location ----------
        void BuildLocation(Transform tr)
        {
            Prim.Make(PrimitiveType.Cylinder, tr, new Vector3(0f, -0.9f, 0f), new Vector3(3.4f, 0.06f, 3.4f), MaterialFactory.CreateOpaque(new Color(0.1f, 0.24f, 0.3f), 0.1f, 0.5f), "MapDisc");
            Prim.Make(PrimitiveType.Cube, tr, new Vector3(0f, -0.82f, 0f), new Vector3(3.0f, 0.04f, 0.14f), MaterialFactory.CreateOpaque(new Color(0.3f, 0.6f, 0.95f)), "MapPipe");
            var pinMat = MaterialFactory.CreateOpaque(LocationColor, 0.2f, 0.6f);
            MaterialFactory.SetEmission(pinMat, LocationColor, 1.2f);
            var pinRoot = new GameObject("Pin");
            pinRoot.transform.SetParent(tr, false);
            Prim.Make(PrimitiveType.Sphere, pinRoot.transform, new Vector3(0f, 0.55f, 0f), Vector3.one * 0.85f, pinMat, "PinHead");
            var tip = Prim.Make(PrimitiveType.Cube, pinRoot.transform, Vector3.zero, new Vector3(0.45f, 0.45f, 0.45f), pinMat, "PinTip");
            tip.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);
            Prim.Make(PrimitiveType.Sphere, pinRoot.transform, new Vector3(0f, 0.55f, -0.34f), Vector3.one * 0.3f, MaterialFactory.CreateOpaque(Color.white), "PinDot");
            pin = pinRoot.transform;
            pin.localPosition = new Vector3(0f, -0.4f, 0f);

            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("PulseRing");
                go.transform.SetParent(tr, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = false; lr.loop = true; lr.positionCount = 40;
                lr.startWidth = 0.07f; lr.endWidth = 0.07f;
                lr.material = MaterialFactory.LineMaterial();
                pulseRings[i] = lr;
            }
        }

        void TickLocation(float t)
        {
            pin.localPosition = new Vector3(0f, -0.4f + Mathf.Abs(Mathf.Sin(t * 3f)) * 0.18f, 0f);
            for (int i = 0; i < 2; i++)
            {
                float ph = Mathf.Repeat(t * 0.7f + i * 0.5f, 1f);
                float rad = Mathf.Lerp(0.2f, 1.5f, ph);
                for (int k = 0; k < 40; k++)
                {
                    float a = k * Mathf.PI * 2f / 40f;
                    pulseRings[i].SetPosition(k, new Vector3(Mathf.Cos(a) * rad, -0.82f, Mathf.Sin(a) * rad));
                }
                var c = LocationColor; c.a = 1f - ph;
                pulseRings[i].startColor = c; pulseRings[i].endColor = c;
            }
        }

        // ---------- Control ----------
        public void SetShown(bool on)
        {
            if (shown == on) return;
            shown = on;
            if (on) { group.SetActive(true); appear = 0f; }
            else
            {
                group.SetActive(false);
                valueLabel.SetVisible(false); nameLabel.SetVisible(false);
                beam.enabled = false;
            }
        }

        /// targetEmphasis 1 = active (enlarged), lower values = idle (small, still animating).
        public void Tick(StationReading truth, float targetEmphasis, bool showValue, float popT)
        {
            if (!shown) return;
            float dt = Time.deltaTime, t = Time.time;
            emphasis = Mathf.MoveTowards(emphasis, targetEmphasis, dt * 2.5f);
            appear = Mathf.MoveTowards(appear, 1f, dt * 2.5f);

            float s = Mathf.Lerp(IdleScale, ActiveScale, emphasis) * Mathf.SmoothStep(0f, 1f, appear);
            group.transform.localScale = Vector3.one * Mathf.Max(0.001f, s);

            switch (type)
            {
                case SensorType.Pressure: TickPressure(truth, t); break;
                case SensorType.Flow: TickFlow(truth, dt); break;
                case SensorType.Vibration: TickVibration(truth, t); break;
                default: TickLocation(t); break;
            }

            float sLab = Mathf.Lerp(IdleScale, ActiveScale, emphasis);
            valueLabel.World = center + new Vector3(0f, 1.9f * sLab + 0.35f, 0f);
            nameLabel.World = center - new Vector3(0f, 1.5f * sLab + 0.3f, 0f);
            nameLabel.SetVisible(appear > 0.6f);

            Color col = ColorOf(type);
            if (showValue)
            {
                string txt = type == SensorType.Pressure ? Round1(truth.pressure).ToString("0.0") + " bar"
                    : type == SensorType.Flow ? Round1(truth.flow).ToString("0.0") + " L/s"
                    : type == SensorType.Vibration ? Round1(truth.vibration).ToString("0.0") + " mm/s" : stationTag;
                valueLabel.SetText(txt);
                float pt = Mathf.Clamp01(popT / 0.35f);
                valueLabel.SetScale(Mathf.Lerp(0.4f, 1f, Mathf.SmoothStep(0f, 1f, pt)) * (1f + 0.15f * Mathf.Sin(pt * Mathf.PI)) * Mathf.Lerp(0.72f, 1f, emphasis));
            }
            valueLabel.SetVisible(showValue && appear > 0.6f);

            beam.enabled = emphasis > 0.5f;
            if (beam.enabled)
            {
                beam.SetPosition(0, deviceTop);
                beam.SetPosition(1, center + new Vector3(0f, -0.9f * s, 0f));
                var c2 = col; c2.a = 0.7f;
                beam.startColor = c2; beam.endColor = new Color(col.r, col.g, col.b, 0.1f);
            }
        }
    }
}
