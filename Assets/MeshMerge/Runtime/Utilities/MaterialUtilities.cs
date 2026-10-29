using System;
using System.Collections.Generic;
using UnityEngine;

namespace maleric
{

    public static class MaterialUtilities
    {

        public static IBakeableMaterialRule GetRule(string materialGuid)
        {
            var instance = BakeableMaterialRulesDefinition.Instance;
            if (instance == null) return null;
            return instance.GetRule(materialGuid, true);
        }

        public static Dictionary<string, IBakeableMaterialRule> GetMergeRules(List<string> materialGuids, out List<Material> uniqueMaterials, bool isSplittable = false, MeshObjectFactoryLodLevel lodLevel = MeshObjectFactoryLodLevel.Level0)
        {
            uniqueMaterials = new List<Material>();
            var instance = BakeableMaterialRulesDefinition.Instance;
            if (instance == null) return new Dictionary<string, IBakeableMaterialRule>();

            Dictionary<string, IBakeableMaterialRule> materialRulePairs;
            if (isSplittable)
                materialRulePairs = instance.GetDefaultRules(materialGuids);
            else
                materialRulePairs = instance.GetRules(materialGuids, true, lodLevel);

            foreach (var kvp in materialRulePairs)
            {
                var material = kvp.Value.GetBakeableMaterial();
                if (!uniqueMaterials.Contains(material))
                    uniqueMaterials.Add(material);
            }

            return materialRulePairs;
        }

        public static List<string> GetOriginalMaterialGuids(MeshObjectFactoryInputData[] inputData)
        {
            List<string> originalMaterialGuids = new List<string>();

            for (int i = 0; i < inputData.Length; i++)
            {
                var originalMeshMaterials = inputData[i].Materials;

                foreach (string material in originalMeshMaterials)
                    if (!originalMaterialGuids.Contains(material))
                        originalMaterialGuids.Add(material);
            }
            return originalMaterialGuids;
        }

        public static Dictionary<string, IBakeableMaterialRule> GetMaterialRules(MeshObjectFactoryInputData[] inputData, out List<Material> uniqueMaterials, bool isSplittable = false, MeshObjectFactoryLodLevel lodLevel = MeshObjectFactoryLodLevel.Level0)
        {
            return GetMergeRules(GetOriginalMaterialGuids(inputData), out uniqueMaterials, isSplittable, lodLevel);
        }

        public static MeshObjectFactoryProcessData[] GroupMeshesPerMaterial(MeshObjectFactoryInputData[] inputData, Dictionary<string, IBakeableMaterialRule> rules, List<Material> uniqueMaterials, MeshObjectFactoryLodLevel lodLevel = MeshObjectFactoryLodLevel.Level0)
        {
            MeshObjectFactoryProcessData[] processData = null;
            if (rules != null && rules.Count > 0)
            {
                processData = new MeshObjectFactoryProcessData[inputData.Length];
                for (int i = 0; i < inputData.Length; i++)
                {
                    int[] newSubmeshIndexes = new int[inputData[i].Materials.Length];
                    IBakeableMaterialRule[] submeshRules = new IBakeableMaterialRule[inputData[i].Materials.Length];
                    for (int j = 0; j < inputData[i].Materials.Length; j++)
                    {
                        var rule = rules[inputData[i].Materials[j]];
                        submeshRules[j] = rule;
                        newSubmeshIndexes[j] = uniqueMaterials.IndexOf(rule.GetBakeableMaterial());
                    }

                    processData[i] = new MeshObjectFactoryProcessData()
                    {
                        Input = inputData[i],
                        NewSubmeshIndexes = newSubmeshIndexes,
                        SubmeshConversionRules = submeshRules
                    };
                }
            }
            return processData;
        }

        public static MeshObjectFactoryProcessData[] GroupMeshesPerMaterial(MeshObjectFactoryInputData[] inputData, MeshObjectFactoryLodLevel lodLevel = MeshObjectFactoryLodLevel.Level0)
        {
            var rules = GetMergeRules(GetOriginalMaterialGuids(inputData), out List<Material> uniqueMaterials, false, lodLevel);
            return GroupMeshesPerMaterial(inputData, rules, uniqueMaterials, lodLevel);





        }
        
        public static bool GetMaterialGUID(Material material, out string guid, out long localId)
        {
#if UNITY_EDITOR
            bool isFound = UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material, out guid, out localId);

            if (!isFound)
            {
                var materialPaths = System.IO.Directory.GetFiles(Application.dataPath, "*.mat", System.IO.SearchOption.AllDirectories);
                foreach (var matPath in materialPaths)
                {
                    string assetPath = "Assets" + matPath.Replace(Application.dataPath, "").Replace('\\', '/');
                    var mat = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, typeof(Material));
                    if (mat != null && mat.name.Equals(material.name))
                    {
                        material = (Material)mat;
                        isFound = UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material, out guid, out localId);
                        break;
                    }
                }

                if (isFound)
                    Debug.LogWarning("MaterialUtilities: Material " + material.name + " is instantiated material. Original material was found");
                else
                    Debug.LogWarning("MaterialUtilities: Material " + material.name + " is instantiated material. Original material was not found — it will fall back to Standard during merge.");
            }
            return isFound;
#else
            guid = string.Empty;
            localId = 0;
            return false;
#endif
        }

        public static string[] GetMaterialGuids(this MeshRenderer renderer)
        {
            string[] materialguids = new string[renderer.sharedMaterials.Length];
            for (int j = 0; j < renderer.sharedMaterials.Length; j++)
            {
                if (renderer.sharedMaterials != null)
                {
                    string guid;
                    long localId;

                    MaterialUtilities.GetMaterialGUID(renderer.sharedMaterials[j], out guid, out localId);

                    materialguids[j] = guid;
                }
            }
            return materialguids;
        }


        public static void UpdateMeshOwnerMaterialsWithoutRules(this IMeshOwner meshOwner)
        {
            var instance = BakeableMaterialRulesDefinition.Instance;
            if (instance == null) return;
            instance.UpdateMaterialsWithoutRules(meshOwner, meshOwner.GetMeshOwnerOriginalMaterials(),
                meshOwner.GetDefaultFactory() is SplitMeshObjectFactory);
        }

        public static void RemoveMeshOwnerFromMaterialRules(this IMeshOwner meshOwner)
        {
            var instance = BakeableMaterialRulesDefinition.Instance;
            if (instance == null) return;
            instance.UpdateMaterialsWithoutRules(meshOwner, null);
        }

        public static List<string> GetMeshOwnerOriginalMaterials(this IMeshOwner meshOwner)
        {
            var originalMaterials = new List<string>();

            if (meshOwner.MeshObjectInputData != null)
            {
                for (int i = 0; i < meshOwner.MeshObjectInputData.Length; i++)
                {
                    var originalMeshMaterials = meshOwner.MeshObjectInputData[i].Materials;

                    foreach (string material in originalMeshMaterials)
                        if (!originalMaterials.Contains(material))
                            originalMaterials.Add(material);
                }
            }
            return originalMaterials;
        }

    }
}