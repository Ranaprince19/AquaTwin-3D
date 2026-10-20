using UnityEngine;

namespace LeakWatch
{
    public class BuildingInfo
    {
        public Vector3 signPos;
        public Router router;
        public Vector3 roofCenter;
    }

    public static class BuildingBuilder
    {
        const float FloorHeight = 3.2f;

        public static BuildingInfo Build(Transform parent, float centerX, float width, float depth, int floors,
            Color wall, bool withRouter, bool isPumpHouse)
        {
            float height = floors * FloorHeight;
            float frontZ = NetworkLayout.BuildingFrontZ;
            float centerZ = frontZ - depth * 0.5f;

            var root = new GameObject(isPumpHouse ? "PumpHouse" : "Building");
            root.transform.SetParent(parent, false);

            var wallMat = MaterialFactory.CreateOpaque(wall, 0f, 0.15f);
            var trimMat = MaterialFactory.CreateOpaque(Color.Lerp(wall, Color.white, 0.55f), 0f, 0.2f);
            var roofMat = MaterialFactory.CreateOpaque(new Color(0.22f, 0.24f, 0.28f), 0f, 0.2f);
            var plinthMat = MaterialFactory.CreateOpaque(new Color(0.38f, 0.38f, 0.4f), 0f, 0.1f);
            var glassMat = MaterialFactory.CreateOpaque(new Color(0.22f, 0.4f, 0.6f), 0.2f, 0.9f);
            var doorMat = MaterialFactory.CreateOpaque(new Color(0.32f, 0.2f, 0.12f), 0f, 0.2f);
            var signMat = MaterialFactory.CreateOpaque(new Color(0.06f, 0.1f, 0.18f), 0f, 0.3f);

            Prim.Box(root.transform, new Vector3(centerX, height * 0.5f + 0.2f, centerZ), new Vector3(width, height, depth), wallMat, "Walls");
            Prim.Box(root.transform, new Vector3(centerX, 0.2f, centerZ), new Vector3(width + 0.4f, 0.4f, depth + 0.4f), plinthMat, "Plinth");
            Prim.Box(root.transform, new Vector3(centerX, height + 0.2f + 0.2f, centerZ), new Vector3(width + 0.7f, 0.4f, depth + 0.7f), roofMat, "RoofSlab");
            Prim.Box(root.transform, new Vector3(centerX, height + 0.55f, frontZ + 0.25f), new Vector3(width + 0.7f, 0.3f, 0.25f), trimMat, "RoofEdge");
            Prim.Box(root.transform, new Vector3(centerX, FloorHeight + 0.2f, frontZ + 0.03f), new Vector3(width + 0.1f, 0.18f, 0.12f), trimMat, "FloorBand");

            float y0 = 0.4f;
            int cols = Mathf.Max(2, Mathf.FloorToInt(width / 3.0f));
            float colW = width / cols;

            for (int r = 0; r < floors; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    float wx = centerX - width * 0.5f + colW * (c + 0.5f);
                    bool nearDoor = Mathf.Abs(wx - centerX) < 2.2f;
                    if (r == 0 && nearDoor) continue;
                    if (r == 1 && nearDoor && floors > 2) continue;
                    if (isPumpHouse && r == 0 && Mathf.Abs(wx - centerX) < 2.2f) continue;

                    float wy = y0 + r * FloorHeight + FloorHeight * 0.58f;
                    Prim.Box(root.transform, new Vector3(wx, wy, frontZ + 0.02f), new Vector3(1.7f, 1.75f, 0.1f), trimMat, "WindowFrame");
                    Prim.Box(root.transform, new Vector3(wx, wy, frontZ + 0.06f), new Vector3(1.45f, 1.5f, 0.1f), glassMat, "Window");
                    Prim.Box(root.transform, new Vector3(wx, wy, frontZ + 0.12f), new Vector3(0.08f, 1.5f, 0.06f), trimMat, "Mullion");
                    Prim.Box(root.transform, new Vector3(wx, wy, frontZ + 0.12f), new Vector3(1.45f, 0.08f, 0.06f), trimMat, "Mullion");
                }
            }

            Prim.Box(root.transform, new Vector3(centerX, y0 + 1.35f, frontZ + 0.03f), new Vector3(2.1f, 2.8f, 0.14f), trimMat, "DoorFrame");
            Prim.Box(root.transform, new Vector3(centerX, y0 + 1.25f, frontZ + 0.1f), new Vector3(1.7f, 2.6f, 0.12f), doorMat, "Door");
            Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(centerX + 0.6f, y0 + 1.2f, frontZ + 0.2f), Vector3.one * 0.12f,
                MaterialFactory.CreateOpaque(new Color(0.85f, 0.7f, 0.25f), 0.8f, 0.7f), "DoorKnob");

            float signY = isPumpHouse ? y0 + 3.65f : FloorHeight + y0 + 1.7f;
            float signW = isPumpHouse ? 3.6f : 4.8f;
            float signH = isPumpHouse ? 0.95f : 1.5f;
            Prim.Box(root.transform, new Vector3(centerX, signY, frontZ + 0.1f), new Vector3(signW + 0.25f, signH + 0.25f, 0.12f), trimMat, "SignBorder");
            Prim.Box(root.transform, new Vector3(centerX, signY, frontZ + 0.18f), new Vector3(signW, signH, 0.12f), signMat, "SignBoard");

            var info = new BuildingInfo
            {
                signPos = new Vector3(centerX, signY, frontZ + 0.5f),
                roofCenter = new Vector3(centerX, height + 0.6f, centerZ)
            };

            if (isPumpHouse)
            {
                var tankMat = MaterialFactory.CreateOpaque(new Color(0.55f, 0.6f, 0.66f), 0.5f, 0.5f);
                Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(centerX + 1.2f, height + 1.8f, centerZ), new Vector3(2.2f, 1.2f, 2.2f), tankMat, "PumpTank");
                Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(centerX - 1.8f, height + 1.2f, centerZ), new Vector3(0.5f, 0.9f, 0.5f), plinthMat, "Chimney");
            }

            if (withRouter)
                info.router = Router.Create(root.transform, new Vector3(centerX - 4.2f, FloorHeight + 0.2f + 0.75f, frontZ + 0.14f));

            return info;
        }

        public static void BuildTrees(Transform parent)
        {
            var trunk = MaterialFactory.CreateOpaque(new Color(0.3f, 0.2f, 0.12f), 0f, 0.1f);
            var rng = new System.Random(11);
            float[] xs = { -38f, -27f, -18f, -6f, 2f, 17f, 31f, 42f, 54f };
            for (int i = 0; i < xs.Length; i++)
            {
                float h = 3f + (float)rng.NextDouble() * 2.2f;
                float z = -9.5f - (float)rng.NextDouble() * 3f;
                var leaf = MaterialFactory.CreateOpaque(new Color(0.16f + (float)rng.NextDouble() * 0.1f, 0.42f + (float)rng.NextDouble() * 0.14f, 0.18f), 0f, 0.1f);
                Prim.Make(PrimitiveType.Cylinder, parent, new Vector3(xs[i], h * 0.4f, z), new Vector3(0.45f, h * 0.4f, 0.45f), trunk, "TreeTrunk");
                Prim.Make(PrimitiveType.Sphere, parent, new Vector3(xs[i], h * 0.95f, z), new Vector3(h * 0.75f, h * 0.7f, h * 0.75f), leaf, "TreeLeaves");
            }
        }
    }
}
