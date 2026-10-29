using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace maleric
{
#if UNITY_EDITOR
    public static class GUIEditorExtensions
    {
        public static bool BeginCollapseGroup(string label, bool showAdvanced)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.label);

            if (GUILayout.Button(showAdvanced ? "Advanced" : "Simple", EditorStyles.linkLabel))
                showAdvanced = !showAdvanced;

            EditorGUILayout.EndHorizontal();
            return showAdvanced;
        }

        public static bool BeginCollapseGroup(string label, bool showAdvanced, out bool doReset)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.label);

            doReset = false;
            if (showAdvanced && GUILayout.Button("Reset", GUILayout.MaxWidth(50f)))
                doReset = true;

            if (GUILayout.Button(showAdvanced ? "Advanced" : "Simple", EditorStyles.linkLabel))
                showAdvanced = !showAdvanced;

            EditorGUILayout.EndHorizontal();
            return showAdvanced;
        }

        public static void EndCollapseGroup()
        {
            EditorGUILayout.EndVertical();
        }
    }
#endif
}
