using System;
using System.Collections.Generic;
using UnityEngine;

namespace maleric
{
    public interface IBakeableMaterialRule
    {
        /// <summary>
        /// Returns material that should be used on the new mesh
        /// </summary>
        /// <returns>Returns material</returns>
        Material GetBakeableMaterial();

        MeshObjectFactoryLodLevel GetPreferedLodLevel();

        /// <summary>
        /// Modifies mesh and applies conversion from old material to the new one
        /// </summary>
        /// <param name="mesh"></param>
        void ModifyMesh(ref Mesh mesh, int submeshIndex = -1);
    }
}
