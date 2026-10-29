using System;
using System.Collections.Generic;
using UnityEngine;

namespace maleric
{
    /// <summary>
    /// Class used for all Bakeable functionalities
    /// </summary>
    public static class MeshObjectUtilities
    {
        public static bool IsSplittable(this IMeshOwner meshOwner)
        {
            return meshOwner is SplittableMeshOwner;
        }

        public static IMeshObjectFactory GetDefaultFactory(this IMeshOwner meshOwner)
        {
            if (meshOwner.IsSplittable())
                return new SplitMeshObjectFactory();

            return new MergeMeshObjectFactory();
        }

        /// <summary>
        /// Returns Mesh Input Data with selected lod level. Note: Problem is that we define level of lod per Mesh instead per submesh
        /// </summary>
        /// <param name="bakeable"></param>
        /// <param name="lodLevel"></param>
        /// <returns></returns>
        public static MeshObjectFactoryInputData[] GetLodMeshInputData(this IMeshOwner meshOwner, MeshObjectFactoryLodLevel lodLevel)
        {
            List<MeshObjectFactoryInputData> bakeableData = new List<MeshObjectFactoryInputData>();
            for (int i = 0; i < meshOwner.MeshObjectInputData.Length; i++)
            {
                if ((int)meshOwner.MeshObjectInputData[i].LodLevel >= (int)lodLevel)
                    bakeableData.Add(meshOwner.MeshObjectInputData[i]);
            }
            return bakeableData.ToArray();
        }


        #region Creation

        private static MeshObjectFactoryOutputData[] PrepareOutput(this IMeshOwner meshOwner, out int[] outputsCountPerLod, out MeshObjectFactoryLodLevel[] lodLevels)
        {
            MeshObjectFactoryOutputData[] outputs = null;

            lodLevels = (MeshObjectFactoryLodLevel[])Enum.GetValues(typeof(MeshObjectFactoryLodLevel));
            outputsCountPerLod = new int[lodLevels.Length];
            if (meshOwner.UseLod)
            {
                List<MeshObjectFactoryOutputData> outputList = new List<MeshObjectFactoryOutputData>();
                for (int i = 0; i < lodLevels.Length; i++)
                {
                    var inputsForLod = meshOwner.GetLodMeshInputData(lodLevels[i]);
                    if (inputsForLod.Length == 0) continue;
                    var outputsForLod = CreateMeshesOutput(inputsForLod, meshOwner.GetMeshObjectFactory(), lodLevels[i]);
                    for (int k = 0; k < outputsForLod.Length; k++)
                        if (outputsForLod[k].Mesh != null)
                            outputsForLod[k].Mesh.name = meshOwner.Name + "_Merged_LOD" + i;
                    outputsCountPerLod[i] = outputsForLod.Length;
                    outputList.AddRange(outputsForLod);
                }
                outputs = outputList.ToArray();
            }
            else
            {

                outputs = CreateMeshesOutput(meshOwner);
            }

            return outputs;
        }

        private static void PrepareLodOutputObjects(int[] outputsCountPerLod, MeshObjectFactoryLodLevel[] lodLevels, MeshRenderer[] outputRenderers, GameObject outputGameObject)
        {
            if (outputRenderers == null || outputRenderers.Length == 0) return;

            List<LOD> lodsList = new List<LOD>();
            int cursor = 0;
            for (int l = 0; l < lodLevels.Length; l++)
            {
                int count = l < outputsCountPerLod.Length ? outputsCountPerLod[l] : 0;
                if (count <= 0 || cursor >= outputRenderers.Length) continue;

                Renderer[] renderersForLod = new Renderer[count];
                int assigned = 0;
                while (assigned < count && cursor < outputRenderers.Length)
                    renderersForLod[assigned++] = outputRenderers[cursor++];

                if (assigned < count)
                {
                    Array.Resize(ref renderersForLod, assigned);
                    count = assigned;
                }

                float screenRelativeHeight = 1.0f / (l + 2);
                lodsList.Add(new LOD(screenRelativeHeight, renderersForLod));
            }

            if (lodsList.Count == 0) return;

            LODGroup lodGroup = outputGameObject.GetOrAddComponent<LODGroup>();
            lodGroup.SetLODs(lodsList.ToArray());
            lodGroup.RecalculateBounds();
        }

        /// <summary>
        /// This method should handle Overwrite / New Instance
        /// If we want to Spawn New Object that different method its not same
        /// </summary>
        /// <param name="meshOwner"></param>
        /// <returns></returns>
        public static void Create(this IMeshOwner meshOwner)
        {

            MeshObjectFactoryOutputData[] outputs = PrepareOutput(meshOwner, out int[] outputsCountPerLod, out MeshObjectFactoryLodLevel[] lodLevels);

            MeshRenderer[] outputRenderers = meshOwner.OutputRenderers;
            GameObject outputGameObject = meshOwner.OutputGameObject;

            if (outputGameObject == null && !meshOwner.UseNewInstance)
            {
                outputGameObject = meshOwner.GameObject;
            }

            if (CreateAndUpdate(meshOwner.InputParentTransformMatrix, outputs, outputRenderers, outputGameObject, out outputRenderers, out outputGameObject,
                meshOwner.GetCreatedObjectName()))
            {
                meshOwner.SetOutputRenderers(outputRenderers);
                meshOwner.SetOutputGameObject(outputGameObject);

                if (meshOwner.UseLod)
                    PrepareLodOutputObjects(outputsCountPerLod, lodLevels, outputRenderers, outputGameObject);
            }
        }

        private static string GetCreatedObjectName(this IMeshOwner meshOwner)
        {
            return meshOwner.Name + (meshOwner.IsSplittable() ? "_Splitted" : "_Merged");
        }

        /// <summary>
        /// Same as Create however it creates mesh on new instance unrelated to this IMeshOwner
        /// </summary>
        /// <param name="meshOwner"></param>
        /// <returns></returns>
        public static GameObject CreateToNewGameObject(this IMeshOwner meshOwner)
        {
            MeshObjectFactoryOutputData[] outputs = PrepareOutput(meshOwner, out int[] outputsCountPerLod, out MeshObjectFactoryLodLevel[] lodLevels);

            if (CreateAndUpdate(meshOwner.InputParentTransformMatrix, outputs, null, null, out MeshRenderer[] outputRenderers, out GameObject outputGameObject, meshOwner.GetCreatedObjectName()))
            {
                if (meshOwner.UseLod)
                    PrepareLodOutputObjects(outputsCountPerLod, lodLevels, outputRenderers, outputGameObject);

                return outputGameObject;
            }
            return null;
        }


        /// <summary>
        /// Returns true if something new was created
        /// </summary>
        /// <param name="outputData"></param>
        /// <param name="renderers"></param>
        /// <param name="meshObject"></param>
        /// <param name="renderers"></param>
        /// <returns>True if something new was created. False if output is only updated</returns>
        public static bool CreateAndUpdate(Matrix4x4 parentMatrixTransform, MeshObjectFactoryOutputData[] outputData, MeshRenderer[] renderers, GameObject meshObject, out MeshRenderer[] outputRenderers, out GameObject outputObject, string name)
        {
            bool isAnyGOCreated = false;
            if (meshObject == null)
            {
                isAnyGOCreated = true;
                outputObject = new GameObject(name);
                outputObject.transform.SetTransformMatrix(parentMatrixTransform);
            }
            else
            {
                outputObject = meshObject;
            }
            if (renderers == null || renderers.Length == 0)
                renderers = new MeshRenderer[outputData.Length];

            outputRenderers = new MeshRenderer[outputData.Length];

            if (outputData.Length == 1)
            {
                outputRenderers[0] = CreateOrUpdateOutputObject(outputData[0], renderers[0], outputObject, true, name);
            }
            else
            {
                for (int i = 0; i < outputData.Length; i++)
                {
                    bool isRendererChanged = false;
                    MeshRenderer renderer = null;
                    if (renderers.Length > i)
                        renderer = renderers[i];
                    else
                        isRendererChanged = true;

                    string childName = outputData[i].Mesh != null && !string.IsNullOrEmpty(outputData[i].Mesh.name)
                        ? outputData[i].Mesh.name
                        : name + "_" + i;
                    outputRenderers[i] = CreateOrUpdateOutputObject(outputData[i], renderer, outputObject, false, childName);

                    if (!isRendererChanged && renderers[i] != outputRenderers[i])
                        isRendererChanged = true;
                    isAnyGOCreated = isAnyGOCreated || isRendererChanged;
                }

                if (renderers.Length > outputRenderers.Length)
                {
                    isAnyGOCreated = true;

                    for (int i = outputRenderers.Length; i < renderers.Length; i++)
                        Destroy(renderers[i]);
                }
            }
            return isAnyGOCreated;
        }

        private static T GetOrAddComponent<T>(this UnityEngine.GameObject obj) where T : Component
        {
            T component = obj.GetComponent<T>();
            if (component == null)
                component = obj.AddComponent<T>();
            return component;
        }

        public static void Destroy(UnityEngine.Object obj)
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(obj);
            else
                UnityEngine.Object.DestroyImmediate(obj);
        }

        private static MeshRenderer CreateOrUpdateOutputObject(MeshObjectFactoryOutputData outputData, MeshRenderer renderer, GameObject parent, bool useParentAsRendererObject, string name)
        {
            GameObject targetGameObject = parent;

            if (!useParentAsRendererObject)
            {
                if (renderer != null && renderer.gameObject != parent)
                {
                    targetGameObject = renderer.gameObject;
                }
                else
                {
                    if (renderer != null && renderer.gameObject == parent)
                        Destroy(renderer);
                    targetGameObject = new GameObject();
                }

                targetGameObject.transform.SetParent(parent.transform, false);
            }
            else
            {
                if (renderer != null && parent != renderer.gameObject)
                {
                    Destroy(renderer);
                }
            }

            targetGameObject.name = name;
            if (renderer == null)
                renderer = targetGameObject.GetOrAddComponent<MeshRenderer>();

            var filter = targetGameObject.GetOrAddComponent<MeshFilter>();

            renderer.sharedMaterials = outputData.Materials;
            filter.sharedMesh = outputData.Mesh;
            if (outputData.ApplyTransformMatrix)
            {
                targetGameObject.transform.SetTransformMatrix(outputData.TransformToApply.ToRendererMatrixOutput(parent.transform.worldToLocalMatrix));
            }
            else if (!useParentAsRendererObject)
            {
                targetGameObject.transform.localPosition = Vector3.zero;
                targetGameObject.transform.localRotation = Quaternion.identity;
                targetGameObject.transform.localScale = Vector3.one;
            }

            return renderer;
        }

        public static MeshObjectFactoryOutputData[] CreateMeshesOutput(this IMeshOwner meshOwner)
        {

            return CreateMeshesOutput(meshOwner.MeshObjectInputData, meshOwner.GetMeshObjectFactory());
        }

        public static MeshObjectFactoryOutputData[] CreateMeshesOutput(this MeshObjectFactoryInputData[] inputData, IMeshObjectFactory factory, MeshObjectFactoryLodLevel lodLevel = MeshObjectFactoryLodLevel.Level0)
        {
            return factory.CreateMeshesOutput(inputData, lodLevel);
        }
        #endregion

        #region Output Data

        /// <summary>
        /// Removes objects that were created by MeshOwner
        /// </summary>
        /// <param name="meshOwner"></param>
        public static void CleanOutputRenderers(this IMeshOwner meshOwner)
        {
            var outputGameObject = meshOwner.OutputGameObject;
            if (outputGameObject != meshOwner.GameObject)
                Destroy(outputGameObject);

            if (meshOwner.OutputRenderers != null)
                for (int i = meshOwner.OutputRenderers.Length - 1; i >= 0; i--)
                    if (meshOwner.OutputRenderers[i] != null && meshOwner.OutputRenderers[i].gameObject != null)
                        Destroy(meshOwner.OutputRenderers[i].gameObject);

            meshOwner.SetOutputRenderers(null);
            meshOwner.SetOutputGameObject(null);
        }

        public static void SetEnableOutputRenderers(this IMeshOwner meshOwner, bool enable)
        {
            if (meshOwner.OutputRenderers != null)
            {
                for (int i = 0; i < meshOwner.OutputRenderers.Length; i++)
                    if (meshOwner.OutputRenderers[i] != null)
                        meshOwner.OutputRenderers[i].enabled = enable;
            }
        }

        #endregion

        #region Input Data

        public static void SetEnableInputRenderers(this IMeshOwner meshOwner, bool enable)
        {
            if (meshOwner.InputRenderers != null)
            {
                for (int i = 0; i < meshOwner.InputRenderers.Length; i++)
                    if (meshOwner.InputRenderers[i] != null)
                        meshOwner.InputRenderers[i].enabled = enable;
            }
        }

        public static bool IsFirstRendererEnabled(this MeshRenderer[] renderers)
        {
            return renderers != null && renderers.Length > 0 && renderers[0] != null && renderers[0].enabled;
        }

        private static void CheckAndRemovePrefabConnection(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            if (UnityEditor.PrefabUtility.IsPartOfAnyPrefab(obj))
            {
                UnityEditor.PrefabUtility.UnpackPrefabInstance(
                        UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(obj),
                        UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);
            }
#endif
        }

        /// <summary>
        /// Removes References to the referenced input renderers
        /// </summary>
        /// <param name="meshOwner"></param>
        public static void CleanInputRenderers(this IMeshOwner meshOwner)
        {
            CheckAndRemovePrefabConnection(meshOwner.GameObject);
            if (meshOwner.InputRenderers != null)
                for (int i = meshOwner.InputRenderers.Length - 1; i >= 0; i--)
                    if (meshOwner.InputRenderers[i] != null && meshOwner.InputRenderers[i].gameObject != null)
                    {
                        CheckAndRemovePrefabConnection(meshOwner.InputRenderers[i].gameObject);

                        if (meshOwner.GameObject != meshOwner.InputRenderers[i].gameObject)
                            Destroy(meshOwner.InputRenderers[i].gameObject);
                        else
                        {
                            var filter = meshOwner.InputRenderers[i].GetComponent<MeshFilter>();
                            if (filter != null)
                                Destroy(filter);
                            Destroy(meshOwner.InputRenderers[i]);
                        }
                    }

            meshOwner.SetInputRenderers(null);
        }

        public static bool ContainsBakeableDataForRendererAndFilter(this IMeshOwner meshOwner, MeshRenderer renderer, MeshFilter filter)
        {
            for (int i = 0; i < meshOwner.MeshObjectInputData.Length; i++)
            {
                var bakeableData = meshOwner.MeshObjectInputData[i];

                int materialsCount = renderer.sharedMaterials != null ? renderer.sharedMaterials.Length : 0;
                int bakeableMaterialsCount = bakeableData.Materials != null ? bakeableData.Materials.Length : 0;
                if (bakeableData.Mesh.name.Equals(filter.sharedMesh.name) && materialsCount == bakeableMaterialsCount)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns Meshes Input Data from child objects including the parent
        /// </summary>
        /// <param name="meshOwner"></param>
        /// <returns></returns>
        private static MeshObjectFactoryInputData[] FetchMeshObjectInputData(this IMeshOwner meshOwner, out MeshRenderer[] renderers)
        {
            GameObject go = meshOwner.GameObject;
            renderers = go.GetComponentsInChildren<MeshRenderer>();
            List<MeshObjectFactoryInputData> inputData = new List<MeshObjectFactoryInputData>();
            var transform = go.transform;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (meshOwner.UseNewInstance || go != renderers[i].gameObject || meshOwner.IsSplittable())
                {
                    string[] materialguids = renderers[i].GetMaterialGuids();

                    inputData.Add(new MeshObjectFactoryInputData()
                    {
                        Name = renderers[i].name,
                        Materials = materialguids,
                        Mesh = renderers[i].GetComponent<MeshFilter>().sharedMesh,
                        Transform = renderers[i].GetRendererMatrix(transform)
                    });
                }

            }
            return inputData.ToArray();
        }

        public static MeshObjectFactoryInputData[] FetchMeshObjectInputData(GameObject go, out MeshRenderer[] renderers)
        {
            renderers = go.GetComponentsInChildren<MeshRenderer>();
            List<MeshObjectFactoryInputData> inputData = new List<MeshObjectFactoryInputData>();
            var transform = go.transform;
            for (int i = 0; i < renderers.Length; i++)
            {
                string[] materialguids = renderers[i].GetMaterialGuids();

                inputData.Add(new MeshObjectFactoryInputData()
                {
                    Name = renderers[i].name,
                    Materials = materialguids,
                    Mesh = renderers[i].GetComponent<MeshFilter>().sharedMesh,
                    Transform = renderers[i].GetRendererMatrix(transform)
                });

            }
            return inputData.ToArray();
        }

        public static MeshObjectFactoryInputData[] GetMeshObjectInputData(this AMeshOwner meshOwner, bool overwriteExistingInputData, out MeshRenderer[] renderers)
        {
            MeshObjectFactoryInputData[] inputData;
            if (overwriteExistingInputData || meshOwner.MeshObjectInputData == null || meshOwner.MeshObjectInputData.Length == 0)
            {
                inputData = meshOwner.FetchMeshObjectInputData(out renderers);
            }
            else
            {
                var newInputData = meshOwner.FetchMeshObjectInputData(out MeshRenderer[] newRenderers);
                List<MeshRenderer> renderersList = new List<MeshRenderer>(meshOwner.InputRenderers ?? Array.Empty<MeshRenderer>());
                List<MeshObjectFactoryInputData> validNewInputDataList = new List<MeshObjectFactoryInputData>(meshOwner.MeshObjectInputData ?? Array.Empty<MeshObjectFactoryInputData>());

                for (int i = 0; i < newRenderers.Length; i++)
                {
                    if (newRenderers[i] == null) continue;

                    int rendererIndex = renderersList.IndexOf(newRenderers[i]);
                    if (rendererIndex < 0)
                    {
                        validNewInputDataList.Add(newInputData[i]);
                        renderersList.Add(newRenderers[i]);
                    }
                    else if (rendererIndex < validNewInputDataList.Count)
                    {
                        validNewInputDataList[rendererIndex] = validNewInputDataList[rendererIndex].UpdateInputData(newRenderers[i], meshOwner.GameObject.transform);
                    }
                }
                inputData = validNewInputDataList.ToArray();
                renderers = renderersList.ToArray();
            }
            return inputData;
        }



        public static MeshObjectFactoryInputData UpdateInputData(this MeshObjectFactoryInputData inputData, MeshRenderer renderer, Transform parent)
        {
            return new MeshObjectFactoryInputData()
            {
                Name = inputData.Name,
                LodLevel = inputData.LodLevel,
                Materials = renderer.GetMaterialGuids(),
                Mesh = renderer.GetComponent<MeshFilter>().sharedMesh,
                Transform = renderer.GetRendererMatrix(parent)
            };
        }

        public static Vector3 GetParentDifferences(this IMeshOwner meshOwner, MeshRenderer[] modifiedRenderers)
        {
            Vector3 offset = Vector3.zero;
            Transform modifiedParent = null;
            Transform parent = meshOwner.GameObject.transform;
            for (int i = 0; i < modifiedRenderers.Length; i++)
            {
                int inputIndex = GetInputRendererIndex(meshOwner, modifiedRenderers[i], i);
                if (inputIndex > -1 && meshOwner.InputRenderers[inputIndex].transform == parent)
                {
                    modifiedParent = modifiedRenderers[i].transform;
                    offset = meshOwner.GameObject.transform.position - modifiedParent.position;
                    break;
                }
            }
            return offset;
        }

        public static int GetInputRendererIndex(this IMeshOwner meshOwner, MeshRenderer modifiedRenderer, int index)
        {
            var filter = modifiedRenderer.GetComponent<MeshFilter>();
            int targetIndex = -1;
            if (meshOwner.InputRenderers != null)
            {
                if (index < meshOwner.InputRenderers.Length && meshOwner.InputRenderers[index].GetComponent<MeshFilter>().sharedMesh.Equals(filter.sharedMesh))
                {
                    targetIndex = index;
                }
                else
                {
                    for (int i = 0; i < meshOwner.InputRenderers.Length; i++)
                    {
                        if (i != targetIndex && meshOwner.InputRenderers[i].GetComponent<MeshFilter>().sharedMesh.Equals(filter.sharedMesh))
                        {
                            targetIndex = i;
                            break;
                        }
                    }
                }
            }
            return targetIndex;
        }

        public static void UpdateInputRenderers(this IMeshOwner meshOwner, MeshRenderer modifiedRenderer, int inputRendererIndex, int inputDataIndex)
        {
            if (meshOwner.InputRenderers[inputRendererIndex].transform == meshOwner.GameObject.transform)
                return;

            if (inputRendererIndex > -1 )
            {
                var matrix = meshOwner.MeshObjectInputData[inputDataIndex].Transform.ToRendererMatrixOutput(meshOwner.GameObject.transform);
                meshOwner.InputRenderers[inputRendererIndex].transform.SetTransformMatrix(matrix);
            }
        }

        /// <summary>
        /// Updates MeshObjectInput data using child objects that includes the parent
        /// </summary>
        /// <param name="meshOwner"></param>
        /// <param name="overwriteExistingInputData">Overwrites existing input data if enabled, othewise it only adds new input data</param>
        public static void UpdateMeshObjectInputData(this AMeshOwner meshOwner, bool overwriteExistingInputData = false)
        {
            meshOwner.SetInputData(GetMeshObjectInputData(meshOwner, overwriteExistingInputData, out MeshRenderer[] renderers));
            meshOwner.SetInputRenderers(renderers);
        }

        /// <summary>
        /// Returns Parent Matrix that is used for parent positioning
        /// </summary>
        /// <param name="meshOwner"></param>
        /// <returns></returns>
        public static Matrix4x4 GetParentMatrix(this IMeshOwner meshOwner)
        {
            return meshOwner.GameObject.transform.localToWorldMatrix;
        }

        /// <summary>
        /// Gets Renderer Matrix used as Matrix saved in Input Data
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static Matrix4x4 GetRendererMatrix(this MeshRenderer renderer, Transform parent)
        {
            return parent.worldToLocalMatrix * renderer.localToWorldMatrix;
        }

        /// <summary>
        /// Gets Renderer Matrix used for output object calculated from previously saved Input Data
        /// </summary>
        /// <param name="localToWorldMatrix"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static Matrix4x4 ToRendererMatrixOutput(this Matrix4x4 localToWorldMatrix, Transform parent)
        {
            return ToRendererMatrixOutput(localToWorldMatrix, parent.worldToLocalMatrix);
        }

        /// <summary>
        /// Gets Renderer Matrix used for output object calculated from previously saved Input Data
        /// </summary>
        /// <param name="localToWorldMatrix"></param>
        /// <param name="worldToLocalParentMatrix"></param>
        /// <returns></returns>
        public static Matrix4x4 ToRendererMatrixOutput(this Matrix4x4 localToWorldMatrix, Matrix4x4 worldToLocalParentMatrix)
        {
            return Matrix4x4.Inverse(worldToLocalParentMatrix) * localToWorldMatrix;
        }




        /// <summary>
        /// Updates the transform matrix
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="transformMatrix"></param>  
        public static void SetTransformMatrix(this Transform transform, Matrix4x4 transformMatrix)
        {
            transform.position = transformMatrix.GetColumn(3);

            transform.rotation = Quaternion.LookRotation(transformMatrix.GetColumn(2), transformMatrix.GetColumn(1));
            transform.localScale = new Vector3(transformMatrix.GetColumn(0).magnitude,
                transformMatrix.GetColumn(1).magnitude, transformMatrix.GetColumn(2).magnitude);
        }

        ///https://www.euclideanspace.com/maths/geometry/rotations/conversions/eulerToMatrix/index.htm
        public static Matrix4x4 UpdateMatrix(this Matrix4x4 transformMatrix, Vector3 position, Vector3 eulerAngles)
        {
            Vector4 currentPosition = transformMatrix.GetColumn(3);
            currentPosition += (Vector4)position;
            transformMatrix = new Matrix4x4(transformMatrix.GetColumn(0), transformMatrix.GetColumn(1), transformMatrix.GetColumn(2), currentPosition);

            double ch = Math.Cos(eulerAngles.x);
            double sh = Math.Sin(eulerAngles.x);
            double ca = Math.Cos(eulerAngles.y);
            double sa = Math.Sin(eulerAngles.y);
            double cb = Math.Cos(eulerAngles.z);
            double sb = Math.Sin(eulerAngles.z);
            transformMatrix[0, 0] += (float)(ch * ca);
            transformMatrix[0, 1] += (float)(sh * sb - ch * sa * cb);
            transformMatrix[0, 2] += (float)(ch * sa * sb + sh * cb);
            transformMatrix[1, 0] += (float)(sa);
            transformMatrix[1, 1] += (float)(ca * cb);
            transformMatrix[1, 2] += (float)(-ca * sb);
            transformMatrix[2, 0] += (float)(-sh * ca);
            transformMatrix[2, 1] += (float)(sh * sa * cb + ch * sb);
            transformMatrix[2, 2] += (float)(-sh * sa * sb + ch * cb);

            return transformMatrix;
        }

        #if UNITY_EDITOR
        public static MeshRenderer ToMeshRendererOnNewObject(this MeshObjectFactoryInputData meshInputData, string namePrefix, Transform transform)
        {
            var obj = new GameObject(namePrefix + meshInputData.Name);
            obj.transform.SetTransformMatrix(meshInputData.Transform.ToRendererMatrixOutput(transform.worldToLocalMatrix));
            obj.transform.SetParent(transform);
            var filter = obj.AddComponent<MeshFilter>();
            var renderer = obj.AddComponent<MeshRenderer>();
            filter.sharedMesh = meshInputData.Mesh;
            var materials = ReferenceAttributeDrawer.GetObjectsByGuids<Material>(meshInputData.Materials);
            renderer.sharedMaterials = materials;
            return renderer;
        }
#endif

        #endregion


        #region Menu Items
#if UNITY_EDITOR


        public static T AddMeshOwnerComponent<T>(this GameObject gameObject) where T : AMeshOwner
        {
            var component = gameObject.AddComponent<T>();
            component.SetInputData(component.FetchMeshObjectInputData(out MeshRenderer[] renderers));
            component.SetInputRenderers(renderers);
            return component;
        }

        public static List<T> AddMeshOwnerComponent<T>(this UnityEngine.Object[] objects, bool autoCreateMesh = false, bool useNewInstance = true) where T : AMeshOwner
        {
            List<T> bakeables = new List<T>();
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] is GameObject)
                {
                    GameObject go = objects[i] as GameObject;
                    T bakeable = go.GetComponent<T>();
                    if (bakeable == null)
                    {
                        bakeable = go.AddMeshOwnerComponent<T>();
                        bakeable.UseNewInstance = useNewInstance;
                        bakeables.Add(bakeable);


                        /// Note: thes this options because we should use it to quickly create large number of bakeable objects
                        /// Maybe add Convert to the different type of bakeable (so it preservers data)
                        /// Maybe add same options into some special window

                        if (autoCreateMesh)
                            bakeable.Create();
                    }
                    else
                    {
                        Debug.LogWarning("Bakeable Utilities: Bakeable component already added for " + go.name + ". New bakeable component is not added.");
                    }
                }
            }
            return bakeables;
        }

        [UnityEditor.MenuItem("GameObject/MeshMerge/Add Mesh Combiner", false, 0)]
        public static void MenuAddMergeComponent()
        {
            var selectedObjects = UnityEditor.Selection.objects;
            selectedObjects.AddMeshOwnerComponent<MergeableMeshOwner>();
        }

        [UnityEditor.MenuItem("GameObject/MeshMerge/Combine to New Object", false, 1)]
        public static void MenuAddMergeComponentAndMerge()
        {
            var selectedObjects = UnityEditor.Selection.objects;
            selectedObjects.AddMeshOwnerComponent<MergeableMeshOwner>(true);
        }

        [UnityEditor.MenuItem("GameObject/MeshMerge/Combine (Overwrite Self)", false, 2)]
        public static void MenuAddMergeComponentAndMergeItself()
        {
            var selectedObjects = UnityEditor.Selection.objects;
            selectedObjects.AddMeshOwnerComponent<MergeableMeshOwner>(true, false);
        }

        [UnityEditor.MenuItem("GameObject/MeshMerge/Add Mesh Splitter", false, 10)]
        public static void MenuAddSplitComponent()
        {
            var selectedObjects = UnityEditor.Selection.objects;
            selectedObjects.AddMeshOwnerComponent<SplittableMeshOwner>(true, false);
        }

        [UnityEditor.MenuItem("GameObject/MeshMerge/Update Combined Mesh", false, 20)]
        public static void MenuDoBake()
        {
            var selectedObjects = UnityEditor.Selection.objects;
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                if (selectedObjects[i] is GameObject)
                {
                    AMeshOwner meshOwner = ((GameObject)selectedObjects[i]).GetComponent<AMeshOwner>();
                    meshOwner.Create();
                }
            }
        }

        [UnityEditor.MenuItem("GameObject/MeshMerge/Update Combined Mesh", true, 20)]
        public static bool MenuIsDoBakeShown()
        {
            var selectedObjects = UnityEditor.Selection.objects;
            bool isBakeable = false;
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                if (selectedObjects[i] is GameObject && ((GameObject)selectedObjects[i]).GetComponent<AMeshOwner>() != null)
                {
                    isBakeable = true;
                    break;
                }
            }
            return isBakeable;
        }

        [UnityEditor.MenuItem("CONTEXT/AMeshOwner/Create Mesh")]
        public static void MenuAutoBake(UnityEditor.MenuCommand menuCommand)
        {
            var meshOwner = menuCommand.context as AMeshOwner;
            meshOwner.Create();
        }

        [UnityEditor.MenuItem("CONTEXT/AMeshOwner/Fetch New Input Data")]
        public static void MenuFetchInputData(UnityEditor.MenuCommand menuCommand)
        {
            var meshOwner = menuCommand.context as AMeshOwner;
            meshOwner.UpdateMeshObjectInputData();
        }

        [UnityEditor.MenuItem("CONTEXT/AMeshOwner/Fetch And Overwrite Input Data")]
        public static void MenuFetchOverwriteInputData(UnityEditor.MenuCommand menuCommand)
        {
            var meshOwner = menuCommand.context as AMeshOwner;
            meshOwner.UpdateMeshObjectInputData(true);
        }

#endif
        #endregion
    }
}
