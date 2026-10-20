using UnityEngine;

namespace LeakWatch
{
    /// Runtime state of one sensor station (pressure, flow, vibration, location, each with a Wi-Fi module).
    public class Station : MonoBehaviour
    {
        public int Index;
        public string Tag;
        public float X;

        public Vector3[] DeviceTops = new Vector3[4];
        public Vector3[] ModuleTops = new Vector3[4];
        public Material[] ModuleLeds = new Material[4];
        public Vector3 AntennaTip;
        public Router Router;

        public ExhibitView[] Exhibits = new ExhibitView[4];
        public ScreenLabel TagLabel;

        public Transform FlowRotor;
        public Material VibrationMat;

        readonly float[] ledOn = new float[4];
        float vibGlow;
        float rotorAngle;
        Material[] sensorMats;
        Color[] sensorBase;
        readonly float[] dim = new float[4];
        int focus = -1;

        public void BlinkModule(int k) { ledOn[k] = 1f; }
        public void VibrationPulse(float strength) { vibGlow = Mathf.Max(vibGlow, strength); }

        public void SetSensorMaterials(Material[] mats)
        {
            sensorMats = mats;
            sensorBase = new Color[4];
            for (int i = 0; i < 4; i++) sensorBase[i] = mats[i].color;
        }

        /// -1 = nothing focused; otherwise the other three sensors are dimmed slightly.
        public void SetFocus(int index) { focus = index; }

        public void SetExhibitsShown(bool on)
        {
            foreach (var e in Exhibits) e.SetShown(on);
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (FlowRotor != null && Ctx.Sim != null)
            {
                rotorAngle += Ctx.Sim.Truth(Index).flow / 12f * 300f * dt;
                FlowRotor.localRotation = Quaternion.Euler(rotorAngle, 0f, 0f);
            }

            for (int k = 0; k < 4; k++)
            {
                ledOn[k] = Mathf.MoveTowards(ledOn[k], 0f, dt * 2.2f);
                if (ModuleLeds[k] == null) continue;
                ModuleLeds[k].color = Color.Lerp(new Color(0.1f, 0.25f, 0.12f), new Color(0.3f, 1f, 0.4f), ledOn[k]);
                MaterialFactory.SetEmission(ModuleLeds[k], new Color(0.2f, 1f, 0.4f), ledOn[k] * 3f);
            }

            if (sensorMats != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    float target = (focus >= 0 && focus != i) ? 1f : 0f;
                    dim[i] = Mathf.MoveTowards(dim[i], target, dt * 3f);
                    sensorMats[i].color = Color.Lerp(sensorBase[i], new Color(0.3f, 0.3f, 0.32f), dim[i] * 0.35f);
                    if (i == 3) MaterialFactory.SetEmission(sensorMats[i], ExhibitView.LocationColor, 0.8f * (1f - dim[i] * 0.5f));
                }
            }

            vibGlow = Mathf.MoveTowards(vibGlow, 0f, dt * 1.2f);
            if (VibrationMat != null)
                MaterialFactory.SetEmission(VibrationMat, new Color(1f, 0.55f, 0.1f), vibGlow * 3f);
        }
    }
}
