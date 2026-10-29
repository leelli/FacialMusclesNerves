using UnityEngine;
using UnityEngine.Rendering;

namespace maleric
{
    [CreateAssetMenu(fileName = "MeshMergeSettings", menuName = "maleric/MeshMerge/Settings", order = 100)]
    public class MeshMergeSettings : ScriptableObject
    {
        /// <summary>
        /// Returns the settings asset, resolving it through Resources (with an
        /// editor-only AssetDatabase fallback) instead of caching it in a static
        /// field, so no static state survives between Play mode sessions.
        /// </summary>
        public static MeshMergeSettings Instance
        {
            get
            {
                var instance = Resources.Load<MeshMergeSettings>("MeshMergeSettings");
#if UNITY_EDITOR
                if (instance == null)
                {
                    var guids = UnityEditor.AssetDatabase.FindAssets("t:MeshMergeSettings");
                    if (guids != null && guids.Length > 0)
                        instance = UnityEditor.AssetDatabase.LoadAssetAtPath<MeshMergeSettings>(
                            UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                }
#endif
                if (instance == null)
                {
                    instance = CreateInstance<MeshMergeSettings>();
                    instance.name = "Default";
                }
                return instance;
            }
        }

        [Header("Output Paths")]
        [Tooltip("Where generated texture arrays, atlases, and rules are saved")]
        public string GeneratedAssetsPath = "Assets/MeshMerge/Resources/MeshMerge_Generated";

        [Header("Texture Array Shader")]
        [Tooltip("Primary URP shader for texture array baking")]
        public string TextureArrayShaderName = "maleric/TextureArray/URP";
        [Tooltip("Fallback for Built-in RP")]
        public string TextureArrayShaderFallback = "maleric/TextureArray/Standard";

        [Header("Demo Scene Paths")]
        [Tooltip("First house prefab (center)")]
        public string DefaultDemoHousePrefab = "Assets/MeshMerge/Examples/House 1.prefab";
        [Tooltip("Second house prefab (offset on Z)")]
        public string DefaultDemoHousePrefab2 = "Assets/MeshMerge/Examples/House 2.prefab";
        [Tooltip("Third house prefab (offset on negative Z)")]
        public string DefaultDemoHousePrefab3 = "Assets/MeshMerge/Examples/House 3.prefab";
        [Header("Prefab Import Settings")]
        [Tooltip("Some Prefab files have built-in rotation that needs correction.\nApplied as Euler angles to spawned instances.")]
        public Vector3 PrefabRotationFix = new Vector3(0, 0, 0);

        public string GetTextureArrayShader()
        {
            bool isURP = GraphicsSettings.currentRenderPipeline != null
                && GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("UniversalRenderPipeline");

            if (isURP)
            {
                Shader shader = Shader.Find(TextureArrayShaderName);
                if (shader != null) return TextureArrayShaderName;
            }

            Shader fallback = Shader.Find(TextureArrayShaderFallback);
            if (fallback != null) return TextureArrayShaderFallback;

            return "Standard";
        }

        public string GetAtlasShader()
        {
            if (GraphicsSettings.currentRenderPipeline != null
                && GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("UniversalRenderPipeline"))
                return "Universal Render Pipeline/Lit";
            return "Standard";
        }

        /// <summary>
        /// Creates a default settings asset if none exists.
        /// Called from editor tools on first use.
        /// </summary>
        public static MeshMergeSettings EnsureExists()
        {
            var instance = Instance;
#if UNITY_EDITOR
            if (!UnityEditor.AssetDatabase.Contains(instance))
            {
                string path = "Assets/MeshMerge/Resources/MeshMergeSettings.asset";
                System.IO.Directory.CreateDirectory("Assets/MeshMerge/Resources");
                UnityEditor.AssetDatabase.CreateAsset(instance, path);
                UnityEditor.AssetDatabase.SaveAssets();
                Debug.Log("MeshMergeSettings: Created default settings at " + path);
            }
#endif
            return instance;
        }
    }
}


