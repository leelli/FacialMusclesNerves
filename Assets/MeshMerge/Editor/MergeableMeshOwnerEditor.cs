using UnityEditor;

namespace maleric
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MergeableMeshOwner), true)]
    public class MergeableMeshOwnerEditor : AMeshOwnerEditor
    {
    }
}
