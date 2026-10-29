using System;
using System.Collections.Generic;
using UnityEngine;

namespace maleric
{
    /// <summary>
    /// A base clas for the material database that returns new material without any additional modification
    /// </summary>
    [Serializable]
    public class BakeableMaterialRule : IBakeableMaterialRule
    {
        public Material Material;
        [Tooltip("Optional to use more simple materials for objects far away. Lod Level 1 means it will be always used on second LOD. If only one lod is defined it will be always used for bake system.")]
        public MeshObjectFactoryLodLevel PreferedMaxLodLevel = MeshObjectFactoryLodLevel.Level2;

        public Material GetBakeableMaterial()
        {
            return Material;
        }

        public MeshObjectFactoryLodLevel GetPreferedLodLevel()
        {
            return PreferedMaxLodLevel;
        }

        public virtual void ModifyMesh(ref Mesh mesh, int submeshIndex = -1) { }
    }

}