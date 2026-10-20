using UnityEngine;

namespace LeakWatch
{
    /// Small helper for creating primitives with a material already assigned.
    public static class Prim
    {
        public static GameObject Make(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, string name = null)
        {
            var go = GameObject.CreatePrimitive(type);
            if (name != null) go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        public static GameObject Box(Transform parent, Vector3 center, Vector3 size, Material mat, string name = null)
            => Make(PrimitiveType.Cube, parent, center, size, mat, name);

        /// Box spanning world-style bounds [min,max] in the parent's space.
        public static GameObject BoxMinMax(Transform parent, Vector3 min, Vector3 max, Material mat, string name = null)
            => Make(PrimitiveType.Cube, parent, (min + max) * 0.5f, max - min, mat, name);

        public static GameObject MeshObject(Transform parent, Mesh mesh, Material mat, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        public static void SetRenderQueue(GameObject go, int queue)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null) r.sharedMaterial.renderQueue = queue;
        }
    }
}
