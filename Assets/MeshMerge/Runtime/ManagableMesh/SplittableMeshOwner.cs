using System;
using UnityEngine;

namespace maleric
{
    [AddComponentMenu("maleric/MeshMerge/Mesh Splitter")]
    [HelpURL("https://github.com/maleric/MeshMerge")]
    public class SplittableMeshOwner : AMeshOwner
    {
        public override bool UseLod => false;
    }
}


