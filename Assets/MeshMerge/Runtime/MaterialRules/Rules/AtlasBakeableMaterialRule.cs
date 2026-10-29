using System;
using System.Collections.Generic;
using UnityEngine;

namespace maleric
{
    [Serializable]
    public class AtlasBakeableMaterialRule : BakeableMaterialRule
    {
        public Vector2 UvOffset;
        public Vector2 UvScale = Vector2.one;
        public Vector2 UvTiling = Vector2.one;
        public Vector2 UvMaterialOffset = Vector2.zero;

        private static readonly System.Collections.Generic.HashSet<string> _warnedTiling = new System.Collections.Generic.HashSet<string>();

        public override void ModifyMesh(ref Mesh mesh, int submeshIndex = -1)
        {
            int[] subMeshTris = mesh.GetTriangles(submeshIndex);

            List<Vector3> uvs = new List<Vector3>();
            mesh.GetUVs(0, uvs);

            if (_warnedTiling.Count < 50 && Material != null)
            {
                string warnKey = Material.name;
                if (!_warnedTiling.Contains(warnKey))
                {
                    bool hasTiling = false;
                    for (int j = 0; j < uvs.Count && !hasTiling; j++)
                    {
                        if (uvs[j].x < 0f || uvs[j].x > 1f || uvs[j].y < 0f || uvs[j].y > 1f)
                            hasTiling = true;
                    }
                    if (hasTiling)
                    {
                        _warnedTiling.Add(warnKey);
                        Debug.LogWarning("AtlasBakeableMaterialRule: Material '" + warnKey + "' uses UVs outside [0,1] (tiling). Atlas baking is intended for non-tiling textures — for tiling textures use Texture Array baking instead.");
                    }
                }
            }

            var processed = new HashSet<int>();

            for (int j = 0; j < subMeshTris.Length; j++)
            {
                int index = subMeshTris[j];
                if (processed.Contains(index)) continue;
                processed.Add(index);

                float u = (uvs[index].x * UvTiling.x + UvMaterialOffset.x);
                float v = (uvs[index].y * UvTiling.y + UvMaterialOffset.y);

                uvs[index] = new Vector3(
                    u * UvScale.x + UvOffset.x,
                    v * UvScale.y + UvOffset.y,
                    uvs[index].z);
            }
            mesh.SetUVs(0, uvs);
        }
    }
}