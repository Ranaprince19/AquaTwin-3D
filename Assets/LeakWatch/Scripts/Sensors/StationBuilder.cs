using UnityEngine;

namespace LeakWatch
{
    public static class StationBuilder
    {
        public static Station Build(Transform parent, int index, float xS, string tag, Router router)
        {
            var root = new GameObject("Station_" + tag);
            root.transform.SetParent(parent, false);
            var st = root.AddComponent<Station>();
            st.Index = index; st.Tag = tag; st.X = xS; st.Router = router;

            float py = NetworkLayout.PipeY;
            float R = NetworkLayout.PipeR;
            float top = py + R;

            var blue = MaterialFactory.CreateOpaque(ExhibitView.PressureColor * 0.9f, 0.5f, 0.6f);
            var green = MaterialFactory.CreateOpaque(ExhibitView.FlowColor * 0.9f, 0.4f, 0.55f);
            var orange = MaterialFactory.CreateOpaque(ExhibitView.VibrationColor, 0.3f, 0.5f);
            var purple = MaterialFactory.CreateOpaque(ExhibitView.LocationColor, 0.2f, 0.6f);
            MaterialFactory.SetEmission(purple, ExhibitView.LocationColor, 0.8f);
            var dark = MaterialFactory.CreateOpaque(new Color(0.12f, 0.13f, 0.15f), 0.6f, 0.4f);
            var steel = MaterialFactory.CreateOpaque(new Color(0.7f, 0.72f, 0.78f), 0.85f, 0.6f);
            st.VibrationMat = orange;
            st.SetSensorMaterials(new[] { blue, green, orange, purple });

            // Pressure transducer (blue, threaded cylinder on top)
            float px = xS + NetworkLayout.SensorOffsets[0];
            Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(px, top + 0.1f, 0f), new Vector3(1.0f, 0.12f, 1.0f), steel, "PressureNut");
            Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(px, top + 0.44f, 0f), new Vector3(0.6f, 0.2f, 0.6f), steel, "PressureNeck");
            Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(px, top + 1.0f, 0f), new Vector3(0.85f, 0.55f, 0.85f), blue, "PressureBody");
            Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(px, top + 1.7f, 0f), new Vector3(0.45f, 0.15f, 0.45f), dark, "PressureCap");
            st.DeviceTops[0] = new Vector3(px, top + 1.88f, 0f);

            // Flow meter (green, inline flanged body, translucent so the turbine is visible)
            float fx = xS + NetworkLayout.SensorOffsets[1];
            var housing = Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(fx, py, 0f), new Vector3(2.9f, 0.9f, 2.9f),
                ExhibitView.Casing(ExhibitView.FlowColor), "FlowHousing");
            housing.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            foreach (float dx in new[] { -0.85f, 0.85f })
                Prim.MeshObject(root.transform, TubeMesh.Ring(new Vector3(fx + dx, py, 0f), Vector3.right, R * 0.99f, 1.6f, 0.24f, "FlowFlange"), green, "FlowFlange");
            Prim.Box(root.transform, new Vector3(fx, py + 1.45f + 0.4f, 0f), new Vector3(0.9f, 0.8f, 0.9f), green, "FlowHead");
            Prim.Box(root.transform, new Vector3(fx, py + 1.45f + 0.45f, 0.46f), new Vector3(0.5f, 0.3f, 0.04f), dark, "FlowDisplay");
            var rotor = new GameObject("TurbineRotor");
            rotor.transform.SetParent(root.transform, false);
            rotor.transform.localPosition = new Vector3(fx, py, 0f);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f, rad = a * Mathf.Deg2Rad;
                var blade = Prim.Make(PrimitiveType.Cube, rotor.transform, new Vector3(0f, Mathf.Cos(rad) * 0.4f, Mathf.Sin(rad) * 0.4f), new Vector3(0.09f, 0.55f, 0.3f), steel, "TurbineBlade");
                blade.transform.localRotation = Quaternion.Euler(a, 0f, 0f);
            }
            var hub = Prim.Make(PrimitiveType.Cylinder, rotor.transform, Vector3.zero, new Vector3(0.22f, 0.18f, 0.22f), steel, "TurbineHub");
            hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            st.FlowRotor = rotor.transform;
            st.DeviceTops[1] = new Vector3(fx, py + 1.45f + 0.8f, 0f);

            // Vibration sensor (orange, small box strapped to the wall)
            float vx = xS + NetworkLayout.SensorOffsets[2];
            Prim.MeshObject(root.transform, TubeMesh.Ring(new Vector3(vx, py, 0f), Vector3.right, R * 0.99f, R * 1.1f, 0.55f, "VibStrap"),
                MaterialFactory.CreateOpaque(new Color(0.5f, 0.25f, 0.05f), 0.6f, 0.4f), "VibStrap");
            Prim.Box(root.transform, new Vector3(vx, top + 0.5f, 0f), new Vector3(1.0f, 0.75f, 1.0f), orange, "VibrationBox");
            foreach (float bz in new[] { -0.3f, 0.3f })
                Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(vx - 0.35f, top + 0.88f, bz), new Vector3(0.14f, 0.05f, 0.14f), steel, "VibBolt");
            st.DeviceTops[2] = new Vector3(vx, top + 0.95f, 0f);

            // Location tag (purple map pin on a post)
            float lx = xS + NetworkLayout.SensorOffsets[3];
            Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(lx, top + 0.08f, 0f), new Vector3(0.8f, 0.08f, 0.8f), steel, "LocationBase");
            Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(lx, top + 0.7f, 0f), new Vector3(0.12f, 0.5f, 0.12f), steel, "LocationPost");
            Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(lx, top + 1.5f, 0f), Vector3.one * 0.72f, purple, "PinHead");
            var tip = Prim.Make(PrimitiveType.Cube, root.transform, new Vector3(lx, top + 1.12f, 0f), Vector3.one * 0.32f, purple, "PinTip");
            tip.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);
            Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(lx, top + 1.55f, 0.3f), Vector3.one * 0.24f, MaterialFactory.CreateOpaque(Color.white), "PinDot");
            st.DeviceTops[3] = new Vector3(lx, top + 1.86f, 0f);

            // Wi-Fi module + battery pack beside every sensor
            float[] modDx = { 1.4f, 2.3f, 1.6f, 1.5f };
            for (int k = 0; k < 4; k++)
                BuildModule(root.transform, st, k, st.DeviceTops[k].x + modDx[k], top);

            // Plastic lid at ground level with a short antenna (clear path above the soil)
            var lidMat = MaterialFactory.CreateOpaque(new Color(0.22f, 0.25f, 0.3f), 0f, 0.5f);
            Prim.Box(root.transform, new Vector3(xS, 0.07f, -1.3f), new Vector3(4.2f, 0.14f, 2.6f), lidMat, "ChamberLid");
            Prim.Box(root.transform, new Vector3(xS, 0.03f, -1.3f), new Vector3(4.5f, 0.06f, 2.9f), MaterialFactory.CreateOpaque(new Color(0.5f, 0.52f, 0.55f)), "LidRim");
            Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(xS, 0.7f, -1.3f), new Vector3(0.1f, 0.55f, 0.1f), dark, "LidAntenna");
            Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(xS, 1.28f, -1.3f), Vector3.one * 0.16f, dark, "LidAntennaTip");
            st.AntennaTip = new Vector3(xS, 1.3f, -1.3f);

            var lightGo = new GameObject("ChamberLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.position = new Vector3(xS, py + 1.2f, 3.4f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 26f; light.intensity = 6f;
            light.color = new Color(1f, 0.96f, 0.9f);

            st.TagLabel = LabelLayer.Create(tag, 30, Color.white, new Color(0.38f, 0.2f, 0.62f, 0.95f), st.AntennaTip + new Vector3(0f, 0.9f, 0f), Vector2.zero);
            st.TagLabel.SetVisible(true);

            for (int k = 0; k < 4; k++)
            {
                var exGo = new GameObject("Exhibit_" + (SensorType)k);
                exGo.transform.SetParent(root.transform, false);
                var ex = exGo.AddComponent<ExhibitView>();
                ex.Build((SensorType)k, new Vector3(st.DeviceTops[k].x, 0.9f, 3.4f), tag, st.DeviceTops[k]);
                st.Exhibits[k] = ex;
            }
            return st;
        }

        static void BuildModule(Transform parent, Station st, int k, float mx, float top)
        {
            var pcb = MaterialFactory.CreateOpaque(new Color(0.1f, 0.5f, 0.2f), 0.2f, 0.5f);
            var shield = MaterialFactory.CreateOpaque(new Color(0.8f, 0.82f, 0.86f), 0.9f, 0.7f);
            var dark = MaterialFactory.CreateOpaque(new Color(0.12f, 0.13f, 0.15f), 0.4f, 0.4f);
            float y = top + 0.12f;
            Prim.Box(parent, new Vector3(mx, y, 0f), new Vector3(0.95f, 0.09f, 0.6f), pcb, "WifiModulePcb");
            Prim.Box(parent, new Vector3(mx - 0.15f, y + 0.12f, 0f), new Vector3(0.4f, 0.13f, 0.38f), shield, "WifiModuleShield");
            Prim.Make(PrimitiveType.Cylinder, parent, new Vector3(mx + 0.38f, y + 0.4f, -0.2f), new Vector3(0.04f, 0.3f, 0.04f), dark, "WifiModuleAntenna");
            var led = MaterialFactory.CreateOpaque(new Color(0.1f, 0.25f, 0.12f));
            Prim.Make(PrimitiveType.Sphere, parent, new Vector3(mx + 0.3f, y + 0.1f, 0.18f), Vector3.one * 0.12f, led, "WifiModuleLed");
            st.ModuleLeds[k] = led;
            Prim.Box(parent, new Vector3(mx + 1.0f, y + 0.16f, 0f), new Vector3(0.7f, 0.4f, 0.4f), dark, "BatteryPack");
            Prim.Box(parent, new Vector3(mx + 1.0f, y + 0.22f, 0.21f), new Vector3(0.5f, 0.12f, 0.02f), MaterialFactory.CreateOpaque(new Color(1f, 0.8f, 0.1f)), "BatteryStripe");
            st.ModuleTops[k] = new Vector3(mx + 0.5f, y + 0.6f, 0f);
        }
    }
}
