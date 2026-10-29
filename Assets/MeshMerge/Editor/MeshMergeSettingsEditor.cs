using UnityEngine;
using UnityEditor;

namespace maleric
{
    public static class MeshMergeSettingsEditor
    {
        [MenuItem("Tools/MeshMerge/Settings", false, 0)]
        public static void SelectSettings()
        {
            var instance = MeshMergeSettings.Instance;
            if (instance == null) return;

            if (AssetDatabase.Contains(instance))
            {
                Selection.activeObject = instance;
                EditorGUIUtility.PingObject(instance);
            }
            else
            {
                string path = "Assets/MeshMerge/Resources/MeshMergeSettings.asset";
                System.IO.Directory.CreateDirectory("Assets/MeshMerge/Resources");
                AssetDatabase.CreateAsset(instance, path);
                AssetDatabase.SaveAssets();
                Selection.activeObject = instance;
                EditorGUIUtility.PingObject(instance);
                Debug.Log("MeshMergeSettings: Created settings asset at " + path);
            }
        }

        [MenuItem("Tools/MeshMerge/Settings", true)]
        public static bool ValidateSettings()
        {
            return true;
        }
    }
}


