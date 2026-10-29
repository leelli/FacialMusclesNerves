using UnityEngine;
using UnityEditor;

namespace maleric
{
    public static class BakeableMaterialRulesDefinitionCreator
    {
        [MenuItem("Tools/MeshMerge/Ensure Default Rules Asset", false, 200)]
        public static void EnsureDefaultAsset()
        {
            var existing = Resources.Load<BakeableMaterialRulesDefinition>("BakeableMaterialRulesDefinition");
            if (existing != null)
            {
                Debug.Log("MeshMerge: BakeableMaterialRulesDefinition already exists at: " + AssetDatabase.GetAssetPath(existing));
                return;
            }

            var guids = AssetDatabase.FindAssets("t:BakeableMaterialRulesDefinition");
            if (guids.Length > 0)
            {
                Debug.Log("MeshMerge: Found existing BakeableMaterialRulesDefinition at: " + AssetDatabase.GUIDToAssetPath(guids[0]));
                return;
            }

            var path = "Assets/MeshMerge/Resources/BakeableMaterialRulesDefinition.asset";
            System.IO.Directory.CreateDirectory("Assets/MeshMerge/Resources");

            var asset = ScriptableObject.CreateInstance<BakeableMaterialRulesDefinition>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Debug.Log("MeshMerge: Created default BakeableMaterialRulesDefinition at " + path);
        }
    }
}

