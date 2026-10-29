using System;
using System.Collections.Generic;
using UnityEngine;


namespace maleric
{
    public struct BakeableFactoryMeshData
    {
        public Material NewMaterial;
        public List<BakeableFactorySubmeshesData> Submeshes;
    }

    /// <summary>
    /// This is related to the previus material so we can convert that old material and his submeshes into to new one
    /// </summary>
    public struct BakeableFactorySubmeshesData
    {
        public IBakeableMaterialRule Rule;
        public List<CombineInstance> Submeshes;
    }






    public static class MeshUtilities
    {
        #region Merge Process

        /// <summary>
        /// Returns Rules used for MergeMesh method
        /// </summary>
        /// <param name="processData">Processed Data from Material Utilities </param>
        /// <param name="parentLocalToWorldMatrix">Used to convert current mesh vertices to local position</param>
        /// <param name="combineInstancesArray">CombineInstances that merge meshes</param>
        /// <param name="combineInstancesSubmeshIndex">Used to get index of submeshes of new object</param>
        /// <returns>Returns a list of rules</returns>
        public static List<IBakeableMaterialRule> GetMergeRules(
            this MeshObjectFactoryProcessData[] processData,
            Matrix4x4 parentLocalToWorldMatrix,
            out CombineInstance[] combineInstancesArray,
            out List<int> combineInstancesSubmeshIndex)
        {
            List<CombineInstance> combineInstances = new List<CombineInstance>();
            combineInstancesSubmeshIndex = new List<int>();
            List<IBakeableMaterialRule> combineInstanceSubmeshRule = new List<IBakeableMaterialRule>();

            if (processData != null)
            {
                for (int i = 0; i < processData.Length; i++)
                {
                    if (processData[i].NewSubmeshIndexes != null)
                    {
                        for (int submeshIndex = 0; submeshIndex < processData[i].NewSubmeshIndexes.Length; submeshIndex++)
                        {
                            var combineInstance = new CombineInstance()
                            {
                                transform = Matrix4x4.Inverse(parentLocalToWorldMatrix) * (processData[i].Input.Transform),
                                mesh = processData[i].Input.Mesh,
                                subMeshIndex = submeshIndex
                            };
                            combineInstanceSubmeshRule.Add(processData[i].SubmeshConversionRules[submeshIndex]);
                            combineInstancesSubmeshIndex.Add(processData[i].NewSubmeshIndexes[submeshIndex]);
                            combineInstances.Add(combineInstance);
                        }
                    }
                }
            }
            combineInstancesArray = combineInstances.ToArray();
            return combineInstanceSubmeshRule;
        }

        /// <summary>
        /// Returns Rules used for MergeMesh method
        /// </summary>
        /// <param name="processData">Processed Data from Material Utilities </param>
        /// <param name="combineInstancesArray">CombineInstances that merge meshes</param>
        /// <param name="combineInstancesSubmeshIndex">Used to get index of submeshes of new object</param>
        /// <returns>Returns a list of rules</returns>
        public static List<IBakeableMaterialRule> GetMergeRules(
           this MeshObjectFactoryProcessData[] processData,
           out CombineInstance[] combineInstancesArray,
           out List<int> combineInstancesSubmeshIndex)
        {
            List<CombineInstance> combineInstances = new List<CombineInstance>();
            combineInstancesSubmeshIndex = new List<int>();
            List<IBakeableMaterialRule> combineInstanceSubmeshRule = new List<IBakeableMaterialRule>();

            if (processData != null)
            {
                for (int i = 0; i < processData.Length; i++)
                {
                    if (processData[i].NewSubmeshIndexes != null)
                    {
                        for (int submeshIndex = 0; submeshIndex < processData[i].NewSubmeshIndexes.Length; submeshIndex++)
                        {
                            var combineInstance = new CombineInstance()
                            {
                                transform = processData[i].Input.Transform,
                                mesh = processData[i].Input.Mesh,
                                subMeshIndex = submeshIndex
                            };
                            combineInstanceSubmeshRule.Add(processData[i].SubmeshConversionRules[submeshIndex]);
                            combineInstancesSubmeshIndex.Add(processData[i].NewSubmeshIndexes[submeshIndex]);
                            combineInstances.Add(combineInstance);
                        }
                    }
                }
            }
            combineInstancesArray = combineInstances.ToArray();
            return combineInstanceSubmeshRule;
        }

        public static Mesh MergeMeshes(this MeshObjectFactoryProcessData[] processData, Matrix4x4 parentLocalToWorldMatrix)
        {
            var combineInstanceSubmeshRule = GetMergeRules(processData, parentLocalToWorldMatrix, out CombineInstance[] combineInstancesArray, out List<int> combineInstancesSubmeshIndex);
            return MergeMeshes(combineInstancesArray, combineInstanceSubmeshRule, combineInstancesSubmeshIndex);
        }

        public static Mesh MergeMeshes(this MeshObjectFactoryProcessData[] processData)
        {
            var combineInstanceSubmeshRule = GetMergeRules(processData, out CombineInstance[] combineInstancesArray, out List<int> combineInstancesSubmeshIndex);
            return MergeMeshes(combineInstancesArray, combineInstanceSubmeshRule, combineInstancesSubmeshIndex);
        }

        public static Mesh MergeMeshes(
            CombineInstance[] combineInstancesArray, 
            List<IBakeableMaterialRule> combineInstanceSubmeshRule, 
            List<int> combineInstancesSubmeshIndex)
        {
            Mesh mesh = null;
            if (combineInstancesArray.Length > 0)
            {
                mesh = new Mesh();
                mesh.CombineMeshes(combineInstancesArray, false);

                Dictionary<int, List<int>> submeshTriangles = new Dictionary<int, List<int>>();
                for (int i = 0; i < combineInstancesArray.Length; i++)
                {
                    if (combineInstanceSubmeshRule[i] != null)
                        combineInstanceSubmeshRule[i].ModifyMesh(ref mesh, i);
                    if (!submeshTriangles.ContainsKey(combineInstancesSubmeshIndex[i]))
                        submeshTriangles.Add(combineInstancesSubmeshIndex[i], new List<int>());
                    submeshTriangles[combineInstancesSubmeshIndex[i]].AddRange(mesh.GetTriangles(i));
                }

                foreach (var kvp in submeshTriangles)
                {
                    mesh.SetTriangles(kvp.Value, kvp.Key);
                }
                mesh.subMeshCount = submeshTriangles.Count;
            }
            return mesh;
        }

        #endregion



        #region Split Process

        public static Mesh[] SplitSubmeshes(this MeshObjectFactoryInputData[] meshFactoryInput, out int[] countPerInputData)
        {
            List<Mesh> meshes = new List<Mesh>();
            countPerInputData = new int[meshFactoryInput.Length];
            for (int i = 0; i < meshFactoryInput.Length; i++)
            {
                for (int j = 0; j < meshFactoryInput[i].Mesh.subMeshCount; j++)
                {
                    var mesh = meshFactoryInput[i].Mesh.GetSubmesh(j);
                    meshes.Add(mesh);
                }
                countPerInputData[i] = meshes.Count;
            }
            return meshes.ToArray();
        }

        public static Mesh GetSubmesh(this Mesh aMesh, int aSubMeshIndex)
        {
            if (aSubMeshIndex < 0 || aSubMeshIndex >= aMesh.subMeshCount)
                return null;
            int[] indices = aMesh.GetTriangles(aSubMeshIndex);
            MeshVertices source = new MeshVertices(aMesh);
            MeshVertices dest = new MeshVertices();
            Dictionary<int, int> map = new Dictionary<int, int>();
            int[] newIndices = new int[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                int o = indices[i];
                int n;
                if (!map.TryGetValue(o, out n))
                {
                    n = dest.Add(source, o);
                    map.Add(o, n);
                }
                newIndices[i] = n;
            }
            Mesh m = new Mesh();
            m.name = aMesh.name + "_" + aSubMeshIndex;
            dest.AssignTo(m);
            m.triangles = newIndices;
            return m;
        }

        [Obsolete("Not implemented. Use MergeMeshes with CombineInstance[] instead.")]
        public static Mesh GetMergedMeshes(this Mesh[] aMeshes, params int[][] submeshesIncludedInMesh)
        {
            throw new NotSupportedException("GetMergedMeshes is not implemented. Use MergeMeshes instead.");
        }

        #endregion

        private class MeshVertices
        {
            List<Vector3> verts = null;
            List<Vector2> uv1 = null;
            List<Vector2> uv2 = null;
            List<Vector2> uv3 = null;
            List<Vector2> uv4 = null;
            List<Vector3> normals = null;
            List<Vector4> tangents = null;
            List<Color32> colors = null;
            List<BoneWeight> boneWeights = null;

            public MeshVertices()
            {
                verts = new List<Vector3>();
            }
            public MeshVertices(Mesh aMesh)
            {
                verts = CreateList(aMesh.vertices);
                uv1 = CreateList(aMesh.uv);
                uv2 = CreateList(aMesh.uv2);
                uv3 = CreateList(aMesh.uv3);
                uv4 = CreateList(aMesh.uv4);
                normals = CreateList(aMesh.normals);
                tangents = CreateList(aMesh.tangents);
                colors = CreateList(aMesh.colors32);
                boneWeights = CreateList(aMesh.boneWeights);
            }

            private List<T> CreateList<T>(T[] aSource)
            {
                if (aSource == null || aSource.Length == 0)
                    return null;
                return new List<T>(aSource);
            }
            private void Copy<T>(ref List<T> aDest, List<T> aSource, int aIndex)
            {
                if (aSource == null)
                    return;
                if (aDest == null)
                    aDest = new List<T>();
                aDest.Add(aSource[aIndex]);
            }
            public int Add(MeshVertices aOther, int aIndex)
            {
                int i = verts.Count;
                Copy(ref verts, aOther.verts, aIndex);
                Copy(ref uv1, aOther.uv1, aIndex);
                Copy(ref uv2, aOther.uv2, aIndex);
                Copy(ref uv3, aOther.uv3, aIndex);
                Copy(ref uv4, aOther.uv4, aIndex);
                Copy(ref normals, aOther.normals, aIndex);
                Copy(ref tangents, aOther.tangents, aIndex);
                Copy(ref colors, aOther.colors, aIndex);
                Copy(ref boneWeights, aOther.boneWeights, aIndex);
                return i;
            }
            public void AssignTo(Mesh aTarget)
            {
                if (verts.Count > 65535)
                    aTarget.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                aTarget.SetVertices(verts);
                if (uv1 != null) aTarget.SetUVs(0, uv1);
                if (uv2 != null) aTarget.SetUVs(1, uv2);
                if (uv3 != null) aTarget.SetUVs(2, uv3);
                if (uv4 != null) aTarget.SetUVs(3, uv4);
                if (normals != null) aTarget.SetNormals(normals);
                if (tangents != null) aTarget.SetTangents(tangents);
                if (colors != null) aTarget.SetColors(colors);
                if (boneWeights != null) aTarget.boneWeights = boneWeights.ToArray();
            }
        }

    }
}