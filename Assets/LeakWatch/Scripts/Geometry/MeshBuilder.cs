using System.Collections.Generic;
using UnityEngine;

namespace LeakWatch
{
    /// Accumulates triangles; winding is corrected automatically from the supplied normals.
    public class MeshBuilder
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<int> t = new List<int>();

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 na, Vector3 nb, Vector3 nc, Vector3 nd)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            n.Add(na); n.Add(nb); n.Add(nc); n.Add(nd);
            Vector3 avg = na + nb + nc + nd;
            Vector3 face = Vector3.Cross(b - a, c - a) + Vector3.Cross(c - a, d - a);
            if (Vector3.Dot(face, avg) >= 0f)
            {
                t.Add(i); t.Add(i + 1); t.Add(i + 2);
                t.Add(i); t.Add(i + 2); t.Add(i + 3);
            }
            else
            {
                t.Add(i); t.Add(i + 2); t.Add(i + 1);
                t.Add(i); t.Add(i + 3); t.Add(i + 2);
            }
        }

        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            n.Add(normal); n.Add(normal); n.Add(normal);
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) >= 0f)
            { t.Add(i); t.Add(i + 1); t.Add(i + 2); }
            else
            { t.Add(i); t.Add(i + 2); t.Add(i + 1); }
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name };
            if (v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }
    }

    public static class TubeMesh
    {
        /// Builds a tube (or arc of a tube) around a path. r(theta) = N cos + B sin, where B
        /// starts as the world Z axis, so theta in (0,pi) is the +Z (camera-facing) half.
        public static Mesh Build(CurvePath path, float step, float outerR, float innerR,
            float arcStart, float arcEnd, int radialSeg, bool outer, bool inner, bool edges, bool caps, string name)
        {
            path.SampleEvenly(step, out var pos, out var tan);
            int rings = pos.Count;

            var Ns = new Vector3[rings];
            var Bs = new Vector3[rings];
            Vector3 t0 = tan[0];
            Vector3 refUp = Vector3.forward;
            if (Mathf.Abs(Vector3.Dot(t0, refUp)) > 0.95f) refUp = Vector3.up;
            Vector3 b = (refUp - t0 * Vector3.Dot(refUp, t0)).normalized;
            Vector3 nrm = Vector3.Cross(t0, b);
            Ns[0] = nrm; Bs[0] = b;
            for (int i = 1; i < rings; i++)
            {
                Quaternion q = Quaternion.FromToRotation(tan[i - 1], tan[i]);
                Ns[i] = q * Ns[i - 1];
                Bs[i] = q * Bs[i - 1];
            }

            var mb = new MeshBuilder();
            float span = arcEnd - arcStart;
            bool fullCircle = Mathf.Abs(span) >= Mathf.PI * 2f - 0.001f;

            for (int i = 0; i < rings - 1; i++)
            {
                for (int j = 0; j < radialSeg; j++)
                {
                    float th0 = arcStart + span * j / radialSeg;
                    float th1 = arcStart + span * (j + 1) / radialSeg;
                    Vector3 r00 = Ns[i] * Mathf.Cos(th0) + Bs[i] * Mathf.Sin(th0);
                    Vector3 r01 = Ns[i] * Mathf.Cos(th1) + Bs[i] * Mathf.Sin(th1);
                    Vector3 r10 = Ns[i + 1] * Mathf.Cos(th0) + Bs[i + 1] * Mathf.Sin(th0);
                    Vector3 r11 = Ns[i + 1] * Mathf.Cos(th1) + Bs[i + 1] * Mathf.Sin(th1);

                    if (outer)
                        mb.Quad(pos[i] + r00 * outerR, pos[i] + r01 * outerR, pos[i + 1] + r11 * outerR, pos[i + 1] + r10 * outerR,
                            r00, r01, r11, r10);
                    if (inner && innerR > 0f)
                        mb.Quad(pos[i] + r00 * innerR, pos[i] + r01 * innerR, pos[i + 1] + r11 * innerR, pos[i + 1] + r10 * innerR,
                            -r00, -r01, -r11, -r10);
                }

                if (edges && !fullCircle && innerR > 0f)
                {
                    foreach (int end in new[] { 0, 1 })
                    {
                        float th = end == 0 ? arcStart : arcEnd;
                        Vector3 ra = Ns[i] * Mathf.Cos(th) + Bs[i] * Mathf.Sin(th);
                        Vector3 rb = Ns[i + 1] * Mathf.Cos(th) + Bs[i + 1] * Mathf.Sin(th);
                        Vector3 tth = -Ns[i] * Mathf.Sin(th) + Bs[i] * Mathf.Cos(th);
                        Vector3 nn = end == 0 ? -tth : tth;
                        mb.Quad(pos[i] + ra * outerR, pos[i] + ra * innerR, pos[i + 1] + rb * innerR, pos[i + 1] + rb * outerR, nn, nn, nn, nn);
                    }
                }
            }

            if (caps)
            {
                foreach (int endIdx in new[] { 0, rings - 1 })
                {
                    Vector3 nn = endIdx == 0 ? -tan[0] : tan[rings - 1];
                    for (int j = 0; j < radialSeg; j++)
                    {
                        float th0 = arcStart + span * j / radialSeg;
                        float th1 = arcStart + span * (j + 1) / radialSeg;
                        Vector3 r0 = Ns[endIdx] * Mathf.Cos(th0) + Bs[endIdx] * Mathf.Sin(th0);
                        Vector3 r1 = Ns[endIdx] * Mathf.Cos(th1) + Bs[endIdx] * Mathf.Sin(th1);
                        if (innerR > 0f)
                            mb.Quad(pos[endIdx] + r0 * outerR, pos[endIdx] + r1 * outerR, pos[endIdx] + r1 * innerR, pos[endIdx] + r0 * innerR, nn, nn, nn, nn);
                        else
                            mb.Tri(pos[endIdx], pos[endIdx] + r0 * outerR, pos[endIdx] + r1 * outerR, nn);
                    }
                }
            }

            return mb.ToMesh(name);
        }

        /// Short ring (flange) centred on a point, oriented along a tangent.
        public static Mesh Ring(Vector3 center, Vector3 tangent, float innerR, float outerR, float length, string name)
        {
            var path = new CurvePath(new[] { center - tangent * (length * 0.5f), center + tangent * (length * 0.5f) }, 1);
            return Build(path, length, outerR, innerR, 0f, Mathf.PI * 2f, 28, true, true, false, true, name);
        }
    }
}
