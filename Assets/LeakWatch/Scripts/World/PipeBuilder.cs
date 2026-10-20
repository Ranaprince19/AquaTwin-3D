using UnityEngine;

namespace LeakWatch
{
    public static class PipeBuilder
    {
        public static CurvePath CreatePath() => new CurvePath(NetworkLayout.PipeControlPoints(), 12);

        public static PipeFlow Build(Transform parent, CurvePath path)
        {
            var root = new GameObject("MainPipe");
            root.transform.SetParent(parent, false);
            float R = NetworkLayout.PipeR;

            var metal = MaterialFactory.CreateOpaque(new Color(0.16f, 0.22f, 0.33f), 0.65f, 0.55f);
            var glass = MaterialFactory.CreateTransparent(new Color(0.55f, 0.75f, 0.95f, 0.13f), 0.2f, 0.9f);
            glass.renderQueue = 3003;
            var water = MaterialFactory.CreateTransparent(new Color(0.2f, 0.55f, 1f, 0.5f), 0f, 0.9f);
            water.renderQueue = 3000;

            var back = TubeMesh.Build(path, 0.35f, R, R - 0.14f, Mathf.PI, Mathf.PI * 2f, 14, true, true, true, false, "PipeBackHalf");
            Prim.MeshObject(root.transform, back, metal, "PipeBackHalf");

            var front = TubeMesh.Build(path, 0.35f, R, 0f, 0f, Mathf.PI, 14, true, false, false, false, "PipeFrontHalf");
            Prim.MeshObject(root.transform, front, glass, "PipeFrontGlass");

            var core = TubeMesh.Build(path, 0.35f, R * 0.84f, 0f, 0f, Mathf.PI * 2f, 18, true, false, false, false, "WaterCore");
            Prim.MeshObject(root.transform, core, water, "WaterCore");

            var flangeMat = MaterialFactory.CreateOpaque(new Color(0.12f, 0.17f, 0.26f), 0.7f, 0.5f);
            var boltMat = MaterialFactory.CreateOpaque(new Color(0.08f, 0.08f, 0.1f), 0.8f, 0.4f);

            float py = NetworkLayout.PipeY;
            Vector3[] flangePoints =
            {
                new Vector3(-33.5f, -0.5f, 0f), new Vector3(-30.9f, -2.2f, 0f), new Vector3(-29.1f, -2.2f, 0f),
                new Vector3(-26.0f, py, 0f),
                new Vector3((NetworkLayout.ChamberA.x1 + NetworkLayout.Basement.x0) * 0.5f, py, 0f),
                new Vector3((NetworkLayout.Basement.x1 + NetworkLayout.ChamberB.x0) * 0.5f, py, 0f),
                new Vector3(NetworkLayout.ChamberB.x1 + 1.4f, py, 0f),
                new Vector3(NetworkLayout.PipeEndX, -0.5f, 0f)
            };
            foreach (var fp in flangePoints) BuildFlange(root.transform, path, fp, flangeMat, boltMat);

            BuildValve(root.transform);

            var flowGo = new GameObject("PipeFlow");
            flowGo.transform.SetParent(root.transform, false);
            var flow = flowGo.AddComponent<PipeFlow>();
            flow.Init(path, R);
            return flow;
        }

        public static void BuildFlange(Transform parent, CurvePath path, Vector3 nearPoint, Material flangeMat, Material boltMat)
        {
            float R = NetworkLayout.PipeR;
            float s = path.NearestS(nearPoint);
            Vector3 p = path.Evaluate(s);
            Vector3 t = path.Tangent(s);

            var mesh = TubeMesh.Ring(p, t, R * 0.99f, R * 1.34f, 0.3f, "Flange");
            Prim.MeshObject(parent, mesh, flangeMat, "Flange");

            Vector3 bin = Vector3.forward;
            Vector3 nor = Vector3.Cross(t, bin);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                Vector3 bp = p + (nor * Mathf.Cos(a) + bin * Mathf.Sin(a)) * (R * 1.19f);
                var bolt = Prim.Make(PrimitiveType.Cylinder, parent, bp, new Vector3(0.17f, 0.3f, 0.17f), boltMat, "Bolt");
                bolt.transform.rotation = Quaternion.FromToRotation(Vector3.up, t);
            }
        }

        static void BuildValve(Transform parent)
        {
            float R = NetworkLayout.PipeR;
            var red = MaterialFactory.CreateOpaque(new Color(0.78f, 0.14f, 0.1f), 0.3f, 0.5f);
            var dark = MaterialFactory.CreateOpaque(new Color(0.15f, 0.15f, 0.17f), 0.6f, 0.4f);
            Vector3 c = new Vector3(-30f, -2.2f, 0f);

            var collar = TubeMesh.Ring(c, Vector3.right, R * 0.99f, R * 1.2f, 1.4f, "ValveCollar");
            Prim.MeshObject(parent, collar, red, "ValveCollar");

            Prim.Make(PrimitiveType.Cylinder, parent, c + new Vector3(0f, R + 0.22f, 0f), new Vector3(0.9f, 0.3f, 0.9f), red, "ValveBonnet");
            Prim.Make(PrimitiveType.Cylinder, parent, c + new Vector3(0f, R + 0.55f, 0f), new Vector3(0.16f, 0.2f, 0.16f), dark, "ValveStem");

            var wheel = new GameObject("HandWheel");
            wheel.transform.SetParent(parent, false);
            wheel.transform.position = c + new Vector3(0f, R + 0.8f, 0f);
            for (int i = 0; i < 14; i++)
            {
                float a = i * Mathf.PI * 2f / 14f;
                var seg = Prim.Make(PrimitiveType.Cube, wheel.transform, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.55f, new Vector3(0.28f, 0.1f, 0.12f), red, "WheelRim");
                seg.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            }
            Prim.Make(PrimitiveType.Cube, wheel.transform, Vector3.zero, new Vector3(1.1f, 0.08f, 0.1f), red, "WheelSpokeA");
            Prim.Make(PrimitiveType.Cube, wheel.transform, Vector3.zero, new Vector3(0.1f, 0.08f, 1.1f), red, "WheelSpokeB");
        }
    }
}
