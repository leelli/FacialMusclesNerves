using System;
using System.Collections.Generic;
using UnityEngine;

namespace maleric
{
    /// <summary>
    /// Combination of Texture Array and Atlas where we have layer and offset
    /// </summary>
    [Serializable]
    public class TextureArrayAtlasBakeableMaterialRule : BakeableMaterialRule
    {
        public Vector2 UvOffset;
        public int IndexInTextureArray;

        public override void ModifyMesh(ref Mesh mesh, int submeshIndex = -1)
        {
            int[] subMeshTris = mesh.GetTriangles(submeshIndex);

            List<Vector3> uvs = new List<Vector3>();
            mesh.GetUVs(0, uvs);

            for (int j = 0; j < subMeshTris.Length; j++)
            {
                int index = subMeshTris[j];
                uvs[index] = new Vector3(uvs[index].x + UvOffset.x, uvs[index].y + UvOffset.y, IndexInTextureArray);
            }
            mesh.SetUVs(0, uvs);
        }
    }
}
