using UnityEngine;
using UnityEditor;

namespace maleric
{
    public static class MeshMergeDemoSetup
    {
        [MenuItem("GameObject/MeshMerge/Create Demo Scene", false, 50)]
        public static void CreateDemoScene()
        {
            var house1 = AssetDatabase.LoadAssetAtPath<GameObject>(MeshMergeSettings.Instance.DefaultDemoHousePrefab);
            var house2 = AssetDatabase.LoadAssetAtPath<GameObject>(MeshMergeSettings.Instance.DefaultDemoHousePrefab2);
            var house3 = AssetDatabase.LoadAssetAtPath<GameObject>(MeshMergeSettings.Instance.DefaultDemoHousePrefab3);

            if (house1 == null && house2 == null && house3 == null)
            {
                EditorUtility.DisplayDialog("MeshMerge Demo",
                    "Could not find example Prefab files in Assets/MeshMerge/Examples/.\nPlease import the example assets first.", "OK");
                return;
            }

            var settings = MeshMergeSettings.Instance;
            var rotFix = Quaternion.Euler(settings.PrefabRotationFix);

            var root = new GameObject("MeshMerge Demo");

            if (house1 != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(house1, root.transform);
                inst.transform.position = new Vector3(0, 0, 0);
                inst.transform.rotation = rotFix;
            }

            if (house2 != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(house2, root.transform);
                inst.transform.position = new Vector3(0, 0, 8.25f);
                inst.transform.rotation = rotFix;
            }

            if (house3 != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(house3, root.transform);
                inst.transform.position = new Vector3(-1.75f, 0, -15.5f);
                inst.transform.rotation = rotFix;
            }

            PipelineUtils.FixMaterialsForPipeline(root);

            var combiner = root.AddComponent<MergeableMeshOwner>();
            combiner.UseNewInstance = true;
            combiner.UpdateMeshObjectInputData();

            Undo.RegisterCreatedObjectUndo(root, "Create MeshMerge Demo");
            Selection.activeGameObject = root;

            int beforeCalls = CountDrawCalls(root);
            EditorUtility.DisplayDialog("MeshMerge Demo",
                "Demo scene created!\n\n" +
                "Current: " + beforeCalls + " draw calls\n\n" +
                "NEXT STEPS:\n" +
                "1. Select '" + root.name + "' in the Hierarchy\n" +
                "2. Click 'Merge Now' → meshes combine into 1\n" +
                "3. Click 'Bake to 1 Material (Texture Array)' → 1 draw call\n\n" +
                "After merging, child renderers are automatically disabled.",
                "OK");
        }

        private static int CountDrawCalls(GameObject go)
        {
            int count = 0;
            var renderers = go.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
                count += r.sharedMaterials.Length;
            return count;
        }
    }
}


