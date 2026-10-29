using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace maleric
{
    public static class TextureArrayRuleAutoGenerator
    {
        public static bool GenerateRules(
            Material[] sourceMaterials,
            Material arrayMaterial,
            int[] sliceIndices,
            string outputPath = null)
        {
            if (sourceMaterials == null || arrayMaterial == null || sliceIndices == null)
            {
                Debug.LogError("TextureArrayRuleAutoGenerator: Invalid input parameters.");
                return false;
            }

            if (string.IsNullOrEmpty(outputPath))
                outputPath = TextureArrayMaterialCreator.DefaultOutputPath + "/Rules";
            Directory.CreateDirectory(outputPath);

            var rulesDB = BakeableMaterialRulesDefinition.Instance;
            if (rulesDB == null)
            {
                string dbPath = outputPath + "/BakeableMaterialRulesDefinition.asset";
                dbPath = AssetDatabase.GenerateUniqueAssetPath(dbPath);

                rulesDB = ScriptableObject.CreateInstance<BakeableMaterialRulesDefinition>();
                AssetDatabase.CreateAsset(rulesDB, dbPath);
                AssetDatabase.SaveAssets();
                Debug.Log("TextureArrayRuleAutoGenerator: Created BakeableMaterialRulesDefinition at " + dbPath);

                rulesDB = BakeableMaterialRulesDefinition.Instance;
            }

            if (rulesDB == null)
            {
                Debug.LogError("TextureArrayRuleAutoGenerator: Could not find or create BakeableMaterialRulesDefinition.");
                return false;
            }

            string arrayMatGuid = null;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(arrayMaterial, out string matGuid, out long _))
                arrayMatGuid = matGuid;

            if (string.IsNullOrEmpty(arrayMatGuid))
            {
                Debug.LogError("TextureArrayRuleAutoGenerator: Could not get GUID for array material.");
                return false;
            }

            int createdCount = 0;
            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                if (sourceMaterials[i] == null) continue;

                string sourceGuid = null;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sourceMaterials[i], out string srcGuid, out long _))
                    sourceGuid = srcGuid;

                if (string.IsNullOrEmpty(sourceGuid))
                {
                    Debug.LogWarning("TextureArrayRuleAutoGenerator: Could not get GUID for material '" + sourceMaterials[i].name + "'. Skipping.");
                    continue;
                }

                string rulePath = outputPath + "/" + sourceMaterials[i].name + "_TexArrayRule.asset";
                var existing = AssetDatabase.LoadAssetAtPath<TextureArrayBakeableMaterialRuleDefinition>(rulePath);
                if (existing != null)
                    AssetDatabase.DeleteAsset(rulePath);

                var ruleDef = ScriptableObject.CreateInstance<TextureArrayBakeableMaterialRuleDefinition>();
                ruleDef.SetMaterialGUID(sourceGuid);

                Vector2 uvTiling = Vector2.one;
                Vector2 uvOffset = Vector2.zero;
                if (sourceMaterials[i].HasProperty("_BaseMap_ST"))
                {
                    var st = sourceMaterials[i].GetVector("_BaseMap_ST");
                    uvTiling = new Vector2(st.x, st.y);
                    uvOffset = new Vector2(st.z, st.w);
                }
                else if (sourceMaterials[i].HasProperty("_MainTex_ST"))
                {
                    var st = sourceMaterials[i].GetVector("_MainTex_ST");
                    uvTiling = new Vector2(st.x, st.y);
                    uvOffset = new Vector2(st.z, st.w);
                }

                ruleDef.RuleData = new TextureArrayBakeableMaterialRule()
                {
                    Material = arrayMaterial,
                    IndexInTextureArray = sliceIndices[i],
                    UvTiling = uvTiling,
                    UvOffset = uvOffset,
                    PreferedMaxLodLevel = MeshObjectFactoryLodLevel.Level2
                };

                AssetDatabase.CreateAsset(ruleDef, rulePath);
                createdCount++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("TextureArrayRuleAutoGenerator: Created " + createdCount + " rule definitions for " + sourceMaterials.Length + " materials.");
            return true;
        }
    }
}
