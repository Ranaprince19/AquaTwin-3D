using UnityEngine;

namespace LeakWatch
{
    public static class MaterialFactory
    {
        static Shader _lit;
        static Shader _unlit;

        static Shader LitShader()
        {
            if (_lit == null)
            {
                _lit = Shader.Find("Universal Render Pipeline/Lit");
                if (_lit == null) _lit = Shader.Find("Standard");
            }
            return _lit;
        }

        static Shader UnlitShader()
        {
            if (_unlit == null)
            {
                _unlit = Shader.Find("Universal Render Pipeline/Unlit");
                if (_unlit == null) _unlit = Shader.Find("Unlit/Transparent");
            }
            return _unlit;
        }

        public static Material CreateOpaque(Color color, float metallic = 0f, float smoothness = 0.3f)
        {
            var mat = new Material(LitShader());
            mat.color = color;
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }

        public static Material CreateTransparent(Color colorWithAlpha, float metallic = 0f, float smoothness = 0.4f)
        {
            var mat = new Material(LitShader());
            mat.color = colorWithAlpha;

            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return mat;
        }

        public static Material CreateUnlitTransparent(Color colorWithAlpha, Texture texture = null)
        {
            var mat = new Material(UnlitShader());
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", colorWithAlpha);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", colorWithAlpha);
            if (texture != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);
            }

            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return mat;
        }

        public static Material CreateParticleMaterial(Texture texture)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) return CreateUnlitTransparent(Color.white, texture);
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 20;
            return mat;
        }

        public static Material LineMaterial()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) return CreateUnlitTransparent(Color.white);
            var mat = new Material(shader);
            mat.renderQueue = 3100;
            return mat;
        }

        public static void SetEmission(Material mat, Color emissionColor, float intensity = 1f)
        {
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor * intensity);
            }
        }
    }
}
