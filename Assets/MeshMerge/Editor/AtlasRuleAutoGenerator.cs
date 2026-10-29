using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace maleric
{
    public static class AtlasRuleAutoGenerator
    {
        public static bool GenerateRules(
            Material[] sourceMaterials,
            Material atlasMaterial,
            Vector2[] uvOffsets,
            Vector2[] uvScales,
            string outputPath = null)
        {
            if (sourceMaterials == null || atlasMaterial == null || uvOffsets == null || uvScales == null)
            {
                Debug.LogError("AtlasRuleAutoGenerator: Invalid input parameters.");
                return false;
            }

            if (string.IsNullOrEmpty(outputPath))
                outputPath = AtlasMaterialCreator.DefaultOutputPath + "/Rules";
            Directory.CreateDirectory(outputPath);

            var rulesDB = BakeableMaterialRulesDefinition.Instance;
            if (rulesDB == null)
            {
                string dbPath = outputPath + "/BakeableMaterialRulesDefinition.asset";
                dbPath = AssetDatabase.GenerateUniqueAssetPath(dbPath);
                rulesDB = ScriptableObject.CreateInstance<BakeableMaterialRulesDefinition>();
                AssetDatabase.CreateAsset(rulesDB, dbPath);
                AssetDatabase.SaveAssets();
                rulesDB = BakeableMaterialRulesDefinition.Instance;
            }

            if (rulesDB == null)
            {
                Debug.LogError("AtlasRuleAutoGenerator: Could not find or create BakeableMaterialRulesDefinition.");
                return false;
            }

            int createdCount = 0;
            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                if (sourceMaterials[i] == null) continue;

                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sourceMaterials[i], out string srcGuid, out long _))
                {
                    Debug.LogWarning("AtlasRuleAutoGenerator: Could not get GUID for '" + sourceMaterials[i].name + "'. Skipping.");
                    continue;
                }

                string rulePath = outputPath + "/" + sourceMaterials[i].name + "_AtlasRule.asset";
                var oldRule = AssetDatabase.LoadAssetAtPath<AtlasBakeableMaterialRuleDefinition>(rulePath);
                if (oldRule != null)
                    AssetDatabase.DeleteAsset(rulePath);

                var ruleDef = ScriptableObject.CreateInstance<AtlasBakeableMaterialRuleDefinition>();
                ruleDef.SetMaterialGUID(srcGuid);

                Vector2 uvTiling = Vector2.one;
                Vector2 uvMatOffset = Vector2.zero;
                if (sourceMaterials[i].HasProperty("_BaseMap_ST"))
                {
                    var st = sourceMaterials[i].GetVector("_BaseMap_ST");
                    uvTiling = new Vector2(st.x, st.y);
                    uvMatOffset = new Vector2(st.z, st.w);
                }
                else if (sourceMaterials[i].HasProperty("_MainTex_ST"))
                {
                    var st = sourceMaterials[i].GetVector("_MainTex_ST");
                    uvTiling = new Vector2(st.x, st.y);
                    uvMatOffset = new Vector2(st.z, st.w);
                }

                ruleDef.RuleData = new AtlasBakeableMaterialRule()
                {
                    Material = atlasMaterial,
                    UvOffset = uvOffsets[i],
                    UvScale = uvScales[i],
                    UvTiling = uvTiling,
                    UvMaterialOffset = uvMatOffset,
                    PreferedMaxLodLevel = MeshObjectFactoryLodLevel.Level2
                };

                AssetDatabase.CreateAsset(ruleDef, rulePath);
                createdCount++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("AtlasRuleAutoGenerator: Created " + createdCount + " atlas rule definitions.");
            return true;
        }
    }
}
