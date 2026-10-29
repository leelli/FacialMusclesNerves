using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.IO;

namespace maleric
{
    /// <summary>
    /// Detects the active render pipeline and fixes demo materials so they
    /// never render pink — regardless of whether the user is on Built-in RP or URP.
    /// </summary>
    public static class PipelineUtils
    {
        public static bool IsURP()
        {
            var rp = GraphicsSettings.currentRenderPipeline;
            return rp != null && rp.GetType().Name.Contains("UniversalRenderPipeline");
        }

        /// <summary>
        /// Returns the best-lit shader name for the active pipeline.
        /// </summary>
        public static string GetLitShaderName()
        {
            if (IsURP())
            {
                Shader urp = Shader.Find("Universal Render Pipeline/Lit");
                if (urp != null) return "Universal Render Pipeline/Lit";
            }
            Shader standard = Shader.Find("Standard");
            if (standard != null) return "Standard";
            return "Standard";
        }

        /// <summary>
        /// Reassigns all renderers under the root to materials using the correct
        /// pipeline shader. Converted materials are saved as assets (never left as
        /// unsaved instanced materials), so merging them cannot produce GUID errors.
        /// </summary>
        public static void FixMaterialsForPipeline(GameObject root)
        {
            string shaderName = GetLitShaderName();
            Shader shader = Shader.Find(shaderName);
            if (shader == null) return;

            string outDir = MeshMergeSettings.Instance.GeneratedAssetsPath;
            Directory.CreateDirectory(outDir);

            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0) continue;

                var newMats = new Material[renderer.sharedMaterials.Length];
                bool anyChanged = false;
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    Material src = renderer.sharedMaterials[i];
                    if (src == null)
                    {
                        newMats[i] = CreateSavedMaterial(shader, outDir, renderer.name + "_" + i);
                        anyChanged = true;
                        continue;
                    }

                    string srcShader = src.shader != null ? src.shader.name : "";
                    if (srcShader.StartsWith("maleric/TextureArray/"))
                    {
                        newMats[i] = src;
                        continue;
                    }

                    if (srcShader == shaderName || (srcShader == "Universal Render Pipeline/Lit" && shaderName == "Universal Render Pipeline/Lit"))
                    {
                        newMats[i] = src;
                        continue;
                    }

                    var mat = CreateSavedMaterial(shader, outDir, renderer.name + "_" + i);
                    CopyMaterialProperties(src, mat);
                    newMats[i] = mat;
                    anyChanged = true;
                }

                if (anyChanged)
                    renderer.sharedMaterials = newMats;
            }

            AssetDatabase.SaveAssets();
        }

        private static Material CreateSavedMaterial(Shader shader, string outDir, string name)
        {
            var mat = new Material(shader);
            mat.name = name;
            string path = AssetDatabase.GenerateUniqueAssetPath(outDir + "/" + name + ".mat");
            AssetDatabase.CreateAsset(mat, path);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        /// <summary>
        /// Copies the main texture, color, metallic and smoothness from src to dst.
        /// </summary>
        private static void CopyMaterialProperties(Material src, Material dst)
        {
            Texture tex = src.GetTexture("_BaseMap") ?? src.GetTexture("_MainTex");
            if (tex != null)
            {
                dst.SetTexture("_MainTex", tex);
                if (dst.HasProperty("_BaseMap")) dst.SetTexture("_BaseMap", tex);
            }

            if (src.HasProperty("_BaseColor"))
            {
                Color c = src.GetColor("_BaseColor");
                dst.SetColor("_Color", c);
                if (dst.HasProperty("_BaseColor")) dst.SetColor("_BaseColor", c);
            }
            else if (src.HasProperty("_Color"))
            {
                Color c = src.GetColor("_Color");
                dst.SetColor("_Color", c);
                if (dst.HasProperty("_BaseColor")) dst.SetColor("_BaseColor", c);
            }

            if (src.HasProperty("_Metallic") && dst.HasProperty("_Metallic"))
                dst.SetFloat("_Metallic", src.GetFloat("_Metallic"));

            float smooth = 0.5f;
            if (src.HasProperty("_Smoothness")) smooth = src.GetFloat("_Smoothness");
            else if (src.HasProperty("_Glossiness")) smooth = src.GetFloat("_Glossiness");
            if (dst.HasProperty("_Smoothness")) dst.SetFloat("_Smoothness", smooth);
            if (dst.HasProperty("_Glossiness")) dst.SetFloat("_Glossiness", smooth);
        }
    }
}