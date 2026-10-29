using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace maleric
{

    /// <summary>
    /// Mesh Factory Interface that should be used for mesh split / merge / etc
    /// </summary>
    public interface IMeshObjectFactory
    {
        MeshObjectFactoryOutputData[] CreateMeshesOutput(MeshObjectFactoryInputData[] inputData, MeshObjectFactoryLodLevel lodLevel);
    }

    /// <summary>
    /// Template for Mesh Factory
    /// </summary>
    public abstract class AMeshhObjectFactory : IMeshObjectFactory
    {
        public MeshObjectFactoryOutputData[] CreateMeshesOutput(MeshObjectFactoryInputData[] inputData, MeshObjectFactoryLodLevel lodLevel)
        {
            ProcessInput(inputData, lodLevel);
            return CreateOutput();
        }

        protected abstract void ProcessInput(MeshObjectFactoryInputData[] inputData, MeshObjectFactoryLodLevel lodLevel);



        protected abstract MeshObjectFactoryOutputData[] CreateOutput();
    }

    public class MergeMeshObjectFactory : AMeshhObjectFactory
    {
        protected MeshObjectFactoryLodLevel _lodLevel;
        protected List<Material> _materials;
        protected Dictionary<string, IBakeableMaterialRule> _rulesMap;
        protected MeshObjectFactoryProcessData[] _processData;
        private MeshObjectFactoryInputData[] _originalInputData;

        protected override void ProcessInput(MeshObjectFactoryInputData[] inputData, MeshObjectFactoryLodLevel lodLevel)
        {
            _lodLevel = lodLevel;
            _originalInputData = inputData;
            _rulesMap = MaterialUtilities.GetMaterialRules(inputData, out _materials, false, lodLevel);

            if (_rulesMap != null && _rulesMap.Count > 0 && _materials != null && _materials.Count > 0)
            {
                _processData = MaterialUtilities.GroupMeshesPerMaterial(inputData, _rulesMap, _materials, lodLevel);
            }
            else
            {
                _materials = new List<Material>();
                var materialGuidToIndex = new Dictionary<string, int>();
                var processDataList = new System.Collections.Generic.List<MeshObjectFactoryProcessData>();

                for (int i = 0; i < inputData.Length; i++)
                {
                    if (inputData[i].Mesh == null)
                    {
                        Debug.LogWarning("MergeMeshObjectFactory: Skipping input data '" + inputData[i].Name + "' — mesh is null");
                        continue;
                    }

                    if (inputData[i].Materials == null || inputData[i].Materials.Length == 0)
                    {
                        Debug.LogWarning("MergeMeshObjectFactory: Skipping input data '" + inputData[i].Name + "' — no materials");
                        continue;
                    }

                    int submeshCount = inputData[i].Mesh.subMeshCount;
                    if (submeshCount == 0)
                    {
                        Debug.LogWarning("MergeMeshObjectFactory: Skipping input data '" + inputData[i].Name + "' — mesh has no submeshes");
                        continue;
                    }

                    int[] newSubmeshIndexes = new int[inputData[i].Materials.Length];
                    var rules = new IBakeableMaterialRule[inputData[i].Materials.Length];

                    for (int j = 0; j < inputData[i].Materials.Length; j++)
                    {
                        string guid = inputData[i].Materials[j];
                        if (string.IsNullOrEmpty(guid))
                        {
                            guid = "__missing_" + j;
                        }
                        if (!materialGuidToIndex.ContainsKey(guid))
                        {
                            Material mat = null;
                            if (!guid.StartsWith("__missing_"))
                            {
#if UNITY_EDITOR
                                mat = ReferenceAttributeDrawer.GetObjectByGuid<Material>(guid);
#endif
                            }
                            if (mat == null)
                                mat = new Material(Shader.Find("Standard"));
                            materialGuidToIndex[guid] = _materials.Count;
                            _materials.Add(mat);
                        }
                        newSubmeshIndexes[j] = materialGuidToIndex[guid];
                        rules[j] = null;
                    }

                    processDataList.Add(new MeshObjectFactoryProcessData()
                    {
                        Input = inputData[i],
                        NewSubmeshIndexes = newSubmeshIndexes,
                        SubmeshConversionRules = rules
                    });
                }

                _processData = processDataList.ToArray();
                Debug.Log("MergeMeshObjectFactory: Processing " + _processData.Length + " meshes with " + _materials.Count + " unique materials (no rules)");
            }
        }

        protected override MeshObjectFactoryOutputData[] CreateOutput()
        {
            var mesh = MeshUtilities.MergeMeshes(_processData);
            if (mesh == null)
            {
                Debug.LogWarning("MergeMeshObjectFactory: Merge produced no mesh. Input data may be empty or have no valid meshes.");
                return new MeshObjectFactoryOutputData[0];
            }
            var outputData = new MeshObjectFactoryOutputData[1]
            {
                new MeshObjectFactoryOutputData()
                {
                    Mesh = mesh,
                    Materials = _materials.ToArray()
                }
            };
            return outputData;
        }
    }


    public class SplitMeshObjectFactory : AMeshhObjectFactory
    {
        MeshObjectFactoryInputData[] _inputData;
        protected override void ProcessInput(MeshObjectFactoryInputData[] inputData, MeshObjectFactoryLodLevel lodLevel)
        {
            _inputData = inputData;
        }

        protected override MeshObjectFactoryOutputData[] CreateOutput()
        {
            var meshes = _inputData.SplitSubmeshes(out int[] countPerInput);
            MeshObjectFactoryOutputData[] output = new MeshObjectFactoryOutputData[meshes.Length];

            int inputDataIndex = 0;
            int inputDataSubmeshIndex = 0;
            for (int i = 0; i < meshes.Length; i++)
            {
                while (countPerInput[inputDataIndex] <= i && inputDataIndex < _inputData.Length - 1)
                {
                    inputDataIndex++;
                    inputDataSubmeshIndex = 0;
                }

                string guid = _inputData[inputDataIndex].Materials[
                    inputDataSubmeshIndex % _inputData[inputDataIndex].Materials.Length];
                inputDataSubmeshIndex++;

                Material mat = null;
                if (!string.IsNullOrEmpty(guid))
                {
#if UNITY_EDITOR
                    mat = ReferenceAttributeDrawer.GetObjectByGuid<Material>(guid);
#endif
                }
#if UNITY_EDITOR
                if (mat == null)
                {
                    string meshName = _inputData[inputDataIndex].Name;
                    if (!string.IsNullOrEmpty(meshName))
                    {
                        string[] guids2 = UnityEditor.AssetDatabase.FindAssets(meshName + " t:Material");
                        if (guids2 != null && guids2.Length > 0)
                            mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                                UnityEditor.AssetDatabase.GUIDToAssetPath(guids2[0]));
                    }
                }
#endif
                if (mat == null)
                    mat = new Material(Shader.Find("Standard"));

                output[i] = new MeshObjectFactoryOutputData()
                {
                    Mesh = meshes[i],
                    Materials = new Material[] { mat },
                    ApplyTransformMatrix = true,
                    TransformToApply = _inputData[inputDataIndex].Transform
                };
            }
            return output;
        }
    }
}