using System.Collections.Generic;
using UnityEngine;

namespace LeakWatch
{
    /// Long ground block cut vertically along the front: grass, topsoil, subsoil, with a
    /// trench along the pipe and open chambers so the underground is always visible.
    public static class GroundBuilder
    {
        static readonly VoidRect[] Voids =
        {
            NetworkLayout.ChamberA, NetworkLayout.ChamberB, NetworkLayout.Basement, NetworkLayout.ValvePit
        };

        public static void Build(Transform parent, CurvePath pipe)
        {
            var root = new GameObject("Ground");
            root.transform.SetParent(parent, false);

            var grassMat = MaterialFactory.CreateOpaque(new Color(0.28f, 0.52f, 0.2f), 0f, 0.1f);
            var topsoilMat = MaterialFactory.CreateOpaque(new Color(0.27f, 0.18f, 0.12f), 0f, 0.08f);
            var subsoilMat = MaterialFactory.CreateOpaque(new Color(0.58f, 0.43f, 0.28f), 0f, 0.08f);
            var sandMat = MaterialFactory.CreateOpaque(new Color(0.85f, 0.74f, 0.52f), 0f, 0.05f);
            var concrete = MaterialFactory.CreateOpaque(new Color(0.62f, 0.62f, 0.64f), 0f, 0.15f);

            var xs = new SortedSet<float> { NetworkLayout.GroundMinX, NetworkLayout.GroundMaxX };
            foreach (var v in Voids) { xs.Add(v.x0); xs.Add(v.x1); }
            for (float x = -36f; x <= -25f; x += 1f) xs.Add(x);
            for (float x = NetworkLayout.PipeEndX - 7f; x <= NetworkLayout.PipeEndX + 3f; x += 1f) xs.Add(x);

            var xList = new List<float>(xs);
            float R = NetworkLayout.PipeR;
            float slabZ = -NetworkLayout.FrontSlabDepth;

            for (int c = 0; c < xList.Count - 1; c++)
            {
                float xa = xList[c], xb = xList[c + 1];
                if (xb - xa < 0.05f) continue;

                var removals = new List<Vector2>();
                float yMin, yMax;
                if (!pipe.RangeY(xa, xb, out yMin, out yMax)) { yMin = yMax = NetworkLayout.PipeY; }
                float lo = yMin - R - 0.3f;
                float hi = Mathf.Min(0f, yMax + R + 0.3f);
                removals.Add(new Vector2(lo, hi));

                bool inVoid = false;
                foreach (var v in Voids)
                {
                    if (xa >= v.x0 - 0.001f && xb <= v.x1 + 0.001f)
                    {
                        removals.Add(new Vector2(v.y0, v.y1));
                        inVoid = true;
                    }
                }

                var breaks = new SortedSet<float>(Comparer<float>.Create((p, q) => q.CompareTo(p)))
                {
                    0f, NetworkLayout.GrassBottomY, NetworkLayout.TopsoilBottomY, NetworkLayout.GroundBottomY
                };
                foreach (var r in removals)
                {
                    breaks.Add(Mathf.Clamp(r.x, NetworkLayout.GroundBottomY, 0f));
                    breaks.Add(Mathf.Clamp(r.y, NetworkLayout.GroundBottomY, 0f));
                }
                var yList = new List<float>(breaks);

                for (int k = 0; k < yList.Count - 1; k++)
                {
                    float yTop = yList[k], yBot = yList[k + 1];
                    if (yTop - yBot < 0.01f) continue;
                    float yc = (yTop + yBot) * 0.5f;

                    Material mat = yc > NetworkLayout.GrassBottomY ? grassMat
                        : (yc > NetworkLayout.TopsoilBottomY ? topsoilMat : subsoilMat);

                    bool removed = false;
                    foreach (var r in removals) if (yc > r.x && yc < r.y) removed = true;

                    if (!removed)
                        Prim.BoxMinMax(root.transform, new Vector3(xa, yBot, slabZ), new Vector3(xb, yTop, 0f), mat, "Front");
                    Prim.BoxMinMax(root.transform, new Vector3(xa, yBot, NetworkLayout.GroundBackZ), new Vector3(xb, yTop, slabZ), mat, "Back");
                }

                if (!inVoid && yMax - yMin < 0.4f)
                {
                    float center = (yMin + yMax) * 0.5f;
                    Prim.BoxMinMax(root.transform, new Vector3(xa, lo, slabZ), new Vector3(xb, center - 0.5f, 0.55f), sandMat, "SandBedding");
                }
            }

            foreach (var v in Voids) BuildLining(root.transform, v, concrete);
            BuildRocks(root.transform);
        }

        static void BuildLining(Transform parent, VoidRect v, Material concrete)
        {
            bool basement = v.x0 == NetworkLayout.Basement.x0 && v.y0 == NetworkLayout.Basement.y0;
            float zFront = basement ? NetworkLayout.BasementWallZ : 1.6f;
            float zBack = -NetworkLayout.FrontSlabDepth;

            Prim.BoxMinMax(parent, new Vector3(v.x0, v.y0, zBack), new Vector3(v.x1, v.y1, zBack + 0.1f), concrete, "ChamberBack");
            Prim.BoxMinMax(parent, new Vector3(v.x0, v.y0, zBack), new Vector3(v.x1, v.y0 + 0.25f, zFront), concrete, "ChamberFloor");
            Prim.BoxMinMax(parent, new Vector3(v.x0, v.y1 - 0.25f, zBack), new Vector3(v.x1, v.y1, zFront), concrete, "ChamberCeiling");
            Prim.BoxMinMax(parent, new Vector3(v.x0, v.y0, zBack), new Vector3(v.x0 + 0.4f, v.y1, zFront), concrete, "ChamberPostL");
            Prim.BoxMinMax(parent, new Vector3(v.x1 - 0.4f, v.y0, zBack), new Vector3(v.x1, v.y1, zFront), concrete, "ChamberPostR");
        }

        static void BuildRocks(Transform parent)
        {
            var rng = new System.Random(7);
            var xRanges = new[] { new Vector2(-39f, -36.5f), new Vector2(54f, 57f) };

            for (int i = 0; i < 16; i++)
            {
                float x, y;
                if (i < 4)
                {
                    var r = xRanges[i % xRanges.Length];
                    x = Mathf.Lerp(r.x, r.y, (float)rng.NextDouble());
                    y = Mathf.Lerp(-1.0f, -2.2f, (float)rng.NextDouble());
                }
                else
                {
                    x = Mathf.Lerp(-38f, 56f, (float)rng.NextDouble());
                    y = Mathf.Lerp(-7.4f, -8.5f, (float)rng.NextDouble());
                }

                float s = Mathf.Lerp(0.4f, 1.0f, (float)rng.NextDouble());
                float g = Mathf.Lerp(0.38f, 0.58f, (float)rng.NextDouble());
                var rock = Prim.Make(PrimitiveType.Sphere, parent, new Vector3(x, y, 0.05f),
                    new Vector3(s * 1.3f, s, s * 0.8f), MaterialFactory.CreateOpaque(new Color(g, g, g * 0.96f), 0f, 0.1f), "Rock");
                rock.transform.rotation = Quaternion.Euler(0f, 0f, (float)rng.NextDouble() * 40f);
            }
        }
    }
}
