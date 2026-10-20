using UnityEngine;

namespace LeakWatch
{
    public static class SkyBuilder
    {
        public static void Build(Transform parent, Camera cam)
        {
            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader);
                sky.SetFloat("_SunSize", 0.04f);
                sky.SetFloat("_SunSizeConvergence", 5f);
                sky.SetFloat("_AtmosphereThickness", 1.1f);
                sky.SetColor("_SkyTint", new Color(0.45f, 0.62f, 0.9f));
                sky.SetColor("_GroundColor", new Color(0.62f, 0.7f, 0.78f));
                sky.SetFloat("_Exposure", 1.15f);
                RenderSettings.skybox = sky;
                cam.clearFlags = CameraClearFlags.Skybox;
            }
            else
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.72f, 0.92f);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.74f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.58f, 0.62f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.32f, 0.28f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.72f, 0.82f, 0.92f);
            RenderSettings.fogStartDistance = 200f;
            RenderSettings.fogEndDistance = 620f;

            var lightGo = new GameObject("Sun");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.rotation = Quaternion.Euler(48f, 160f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;
            RenderSettings.sun = light;

            foreach (var other in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (other != light && other.type == LightType.Directional) other.enabled = false;

            var plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plane.name = "FarGround";
            plane.transform.SetParent(parent, false);
            plane.transform.position = new Vector3(0f, NetworkLayout.GroundBottomY - 0.5f, -60f);
            plane.transform.localScale = new Vector3(1600f, 1f, 1600f);
            plane.GetComponent<Renderer>().sharedMaterial = MaterialFactory.CreateOpaque(new Color(0.42f, 0.5f, 0.38f), 0f, 0.05f);
        }
    }
}
