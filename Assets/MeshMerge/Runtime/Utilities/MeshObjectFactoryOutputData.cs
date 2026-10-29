using UnityEngine;

namespace maleric
{
    public struct MeshObjectFactoryOutputData
    {
        public Mesh Mesh;
        public Material[] Materials;

        public bool ApplyTransformMatrix;
        public Matrix4x4 TransformToApply;
    }
}