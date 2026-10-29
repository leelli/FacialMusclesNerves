using UnityEngine;
using UnityEditor;

namespace maleric
{
    /// <summary>
    /// Regenerates the demo materials to use the correct shader for the active pipeline.
    /// Run before exporting so the package works in both Built-in RP and URP.
    /// </summary>
    public static class DemoMaterialFixer
    {
        [MenuItem("Tools/MeshMerge/Fix Demo Materials", false, 0)]
        public static void FixAllDemoMaterials()
        {
            string[] matPaths = new string[]
            {
                "Assets/MeshMerge/Examples/FloorA.mat",
                "Assets/MeshMerge/Examples/HouseWallsAA.mat",
                "Assets/MeshMerge/Examples/HouseWallsAB.mat",
                "Assets/MeshMerge/Examples/RoofA.mat"
            };

            bool isURP = PipelineUtils.IsURP();
            Shader targetShader = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
            if (targetShader == null) targetShader = Shader.Find("Standard");
            if (targetShader == null)
            {
                Debug.LogError("DemoMaterialFixer: No lit shader found.");
                return;
            }

            int fixedCount = 0;
            foreach (var path in matPaths)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    Debug.LogWarning("DemoMaterialFixer: Material not found: " + path);
                    continue;
                }

                Texture tex = mat.GetTexture("_BaseMap") ?? mat.GetTexture("_MainTex");
                Color color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.GetColor("_Color");
                float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
                float smooth = mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : 0.5f;

                mat.shader = targetShader;

                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smooth);

                EditorUtility.SetDirty(mat);
                fixedCount++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("DemoMaterialFixer: Fixed " + fixedCount + " materials to shader '" + targetShader.name + "'.");
        }
    }
}