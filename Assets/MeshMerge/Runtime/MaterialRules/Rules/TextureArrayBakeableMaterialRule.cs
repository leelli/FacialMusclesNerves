using System;
using System.Collections.Generic;
using UnityEngine;

namespace maleric
{
    [Serializable]
    public class TextureArrayBakeableMaterialRule : BakeableMaterialRule
    {
        public int IndexInTextureArray;
        public Vector2 UvTiling = Vector2.one;
        public Vector2 UvOffset = Vector2.zero;

        public override void ModifyMesh(ref Mesh mesh, int submeshIndex = -1)
        {
            int[] subMeshTris = mesh.GetTriangles(submeshIndex);

            List<Vector3> uvs = new List<Vector3>();
            mesh.GetUVs(0, uvs);

            var processed = new HashSet<int>();

            for (int j = 0; j < subMeshTris.Length; j++)
            {
                int index = subMeshTris[j];
                if (processed.Contains(index)) continue;
                processed.Add(index);

                uvs[index] = new Vector3(
                    uvs[index].x * UvTiling.x + UvOffset.x,
                    uvs[index].y * UvTiling.y + UvOffset.y,
                    IndexInTextureArray);
            }
            mesh.SetUVs(0, uvs);
        }
    }
}