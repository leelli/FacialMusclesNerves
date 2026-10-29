using System;
using System.Collections.Generic;
using UnityEngine;

namespace maleric
{
    [AddComponentMenu("maleric/MeshMerge/Runtime Mesh Combiner")]
    [HelpURL("https://github.com/maleric/MeshMerge")]
    public class RuntimeMergableMeshOwner : MergeableMeshOwner
    {
        protected MeshObjectFactoryInputData[] _currentInputData;

        public virtual void UpdateRuntimeInputData(List<MeshObjectFactoryInputData> runtimeBakeableDataList)
        {
            List<MeshObjectFactoryInputData> bakeableDataList = _inputData != null
                ? new List<MeshObjectFactoryInputData>(_inputData)
                : new List<MeshObjectFactoryInputData>();
            bakeableDataList.AddRange(runtimeBakeableDataList);
            _currentInputData = bakeableDataList.ToArray();

            this.Create();
        }

        protected override MeshObjectFactoryInputData[] GetMeshesData()
        {
            return _currentInputData;
        }
    }
}

