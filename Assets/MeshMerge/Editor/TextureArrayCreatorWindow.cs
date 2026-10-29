using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace maleric
{
    public class TextureArrayCreatorWindow : EditorWindow
    {
        private List<Material> _materials = new List<Material>();
        private Vector2 _scrollPos;
        private string _outputPath = TextureArrayMaterialCreator.DefaultOutputPath;
        private bool _isProcessing = false;

        [MenuItem("Tools/MeshMerge/Texture Array Creator", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<TextureArrayCreatorWindow>("Texture Array Creator");
            window.Show();
        }

        public void SetMaterials(List<Material> materials)
        {
            _materials = new List<Material>(materials);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Texture Array Creator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Create a Texture2DArray from multiple materials.\nEach material's texture becomes a slice in the array.", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(_isProcessing);

            EditorGUILayout.LabelField("Materials (" + _materials.Count + ")", EditorStyles.boldLabel);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(200));

            if (_materials.Count == 0)
            {
                EditorGUILayout.HelpBox("Drop materials below or click 'From Selection' to auto-populate from selected GameObjects.", MessageType.Info);
            }

            for (int i = 0; i < _materials.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                var newMat = (Material)EditorGUILayout.ObjectField(_materials[i], typeof(Material), false);
                if (newMat != _materials[i]) _materials[i] = newMat;

                if (GUILayout.Button("X", GUILayout.Width(22)))
                {
                    _materials.RemoveAt(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add Material Slot"))
            {
                _materials.Add(null);
            }

            if (GUILayout.Button("From Selection"))
            {
                PopulateFromSelection();
            }

            if (GUILayout.Button("Clear"))
            {
                _materials.Clear();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _outputPath = EditorGUILayout.TextField("Output Path", _outputPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
                if (!string.IsNullOrEmpty(selected))
                {
                    if (selected.StartsWith(Application.dataPath))
                        _outputPath = "Assets" + selected.Substring(Application.dataPath.Length);
                    else
                        Debug.LogError("Path must be inside the Assets folder.");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            int validCount = 0;
            int texCount = 0;
            foreach (var m in _materials)
            {
                if (m != null) validCount++;
                if (m != null && (m.GetTexture("_MainTex") != null || m.GetTexture("_BaseMap") != null)) texCount++;
            }

            string status;
            Color statusColor;
            if (_materials.Count == 0)
            {
                status = "Add materials to begin";
                statusColor = Color.gray;
            }
            else if (validCount < _materials.Count)
            {
                status = validCount + "/" + _materials.Count + " materials assigned. Fill empty slots.";
                statusColor = Color.yellow;
            }
            else if (texCount == 0)
            {
                status = "Materials found, but none have _MainTex. Textures may be missing.";
                statusColor = Color.yellow;
            }
            else
            {
                status = validCount + " materials ready. " + texCount + " with textures detected.";
                statusColor = Color.green;
            }

            GUI.color = statusColor;
            EditorGUILayout.LabelField(status, EditorStyles.miniLabel);
            GUI.color = Color.white;

            EditorGUILayout.Space();

            GUI.enabled = validCount >= 2 && !_isProcessing;
            if (GUILayout.Button("Create Texture Array", GUILayout.Height(40)))
            {
                CreateTextureArray();
            }
            GUI.enabled = true;

            EditorGUI.EndDisabledGroup();

            if (_isProcessing)
            {
                EditorGUILayout.HelpBox("Creating texture array...", MessageType.Info);
            }
        }

        private void PopulateFromSelection()
        {
            _materials.Clear();
            var selectedObjects = Selection.gameObjects;
            var materialSet = new HashSet<Material>();

            foreach (var go in selectedObjects)
            {
                var renderers = go.GetComponentsInChildren<MeshRenderer>();
                foreach (var r in renderers)
                {
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m != null && !materialSet.Contains(m))
                        {
                            materialSet.Add(m);
                            _materials.Add(m);
                        }
                    }
                }
            }

            if (_materials.Count == 0)
            {
                EditorUtility.DisplayDialog("No Materials Found", "Selected GameObjects have no MeshRenderer components with materials.", "OK");
            }
        }

        private void CreateTextureArray()
        {
            var validMaterials = new List<Material>();
            foreach (var m in _materials)
            {
                if (m != null && !validMaterials.Contains(m))
                    validMaterials.Add(m);
            }

            if (validMaterials.Count < 1)
            {
                EditorUtility.DisplayDialog("Error", "Need at least 1 material to create a texture array.", "OK");
                return;
            }

            _isProcessing = true;
            Repaint();

            bool success = TextureArrayMaterialCreator.CreateTextureArrayFromMaterials(
                validMaterials,
                out Texture2DArray texArray,
                out Material arrayMat,
                out int[] sliceIndices,
                _outputPath);

            if (success)
            {
                TextureArrayRuleAutoGenerator.GenerateRules(
                    validMaterials.ToArray(),
                    arrayMat,
                    sliceIndices,
                    _outputPath + "/Rules");

                EditorUtility.DisplayDialog("Success",
                    "Created:\n" +
                    "- Texture2DArray with " + texArray.depth + " slices\n" +
                    "- Material: " + arrayMat.name + "\n" +
                    "- " + validMaterials.Count + " rule definitions\n\n" +
                    "Now select your Mesh Combiner object and click 'Update Input Data' then 'Create'.\n" +
                    "The material rules will automatically convert to 1 draw call!",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Failed to create texture array. Check console for details.", "OK");
            }

            _isProcessing = false;
            Repaint();
        }
    }
}
