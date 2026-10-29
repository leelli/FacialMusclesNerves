using UnityEngine;

namespace maleric
{
    [AddComponentMenu("maleric/MeshMerge/Mesh Combiner")]
    [HelpURL("https://github.com/maleric/MeshMerge")]
    public class MergeableMeshOwner : AMeshOwner
    {
        [HideInInspector] [SerializeField] protected bool _useLod;
        public override bool UseLod => _useLod;
    }
}

