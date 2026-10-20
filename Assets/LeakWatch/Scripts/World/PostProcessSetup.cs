using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LeakWatch
{
    public static class PostProcessSetup
    {
        /// Mild bloom + ACES tonemapping. Any failure is swallowed so the scene still runs.
        public static void Build(Transform parent, Camera cam)
        {
            try
            {
                var data = cam.GetUniversalAdditionalCameraData();
                if (data != null) data.renderPostProcessing = true;

                foreach (var other in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None)) other.enabled = false;

                var profile = ScriptableObject.CreateInstance<VolumeProfile>();

                var bloom = profile.Add<Bloom>(true);
                bloom.threshold.Override(1.0f);
                bloom.intensity.Override(0.35f);
                bloom.scatter.Override(0.6f);

                var tone = profile.Add<Tonemapping>(true);
                tone.mode.Override(TonemappingMode.ACES);

                var go = new GameObject("PostProcessVolume");
                go.transform.SetParent(parent, false);
                var vol = go.AddComponent<Volume>();
                vol.isGlobal = true;
                vol.priority = 1f;
                vol.sharedProfile = profile;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("LeakWatch: post-processing unavailable, continuing without it. " + e.Message);
            }
        }
    }
}
