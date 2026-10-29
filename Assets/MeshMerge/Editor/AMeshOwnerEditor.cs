using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace maleric
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AMeshOwner), true)]
    public class AMeshOwnerEditor : Editor
    {
        public const float SBUTTON_SIZE = 56f;
        public const float SBUTTON_SPACE = 3f;

        protected AMeshOwner _target;
        protected AMeshOwner[] _targets;

        protected bool _showAdvancedCreateMesh = false;
        protected bool _useNewInstanceForCreate = true;
        protected bool _addMeshOwnerOnNewInstance = false;
        protected bool _disableRenderersAtBake = true;
        protected bool _destroyRenderersAtBake = false;
        protected bool _autoCreateOnEditComplete = true;
        protected bool _cloneMeshOwnerComponent = true;

        protected bool _showAdvancedFetchInputs = false;
        protected bool _overwriteExistingInput = false;

        protected bool _showAdvancedStatistics = false;
        protected int _currentTotalDrawCallCount = 0;
        protected int _newTotalDrawCallCount = 0;
        protected Dictionary<string, IBakeableMaterialRule> _rules;
        protected List<string> _currentMaterials;
        protected List<Material> _newMaterials;
        protected List<Material[]> _meshOwnersMaterials;

        protected bool _isEditModeEnabled = false;
        protected bool _isInputRenderersEnabled = true;
        protected bool _isOutputRenderersEnabled = true;

        protected Color _defaultColor;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            _defaultColor = GUI.color;
            _target = (AMeshOwner)target;
            _targets = Array.ConvertAll(targets, item => (AMeshOwner)item);

            _useNewInstanceForCreate = _target.UseNewInstance;
            _isEditModeEnabled = _isEditModeEnabled || _target.EditModeMeshRenderers != null && _target.EditModeMeshRenderers.Length > 0;

            serializedObject.Update();

            DrawFetchInputData();
            DrawMergeSection();
            DrawStatistics();
            if (_targets.Length == 1) DrawEditorMode();

            serializedObject.ApplyModifiedProperties();
        }

        protected bool SButton(string label, int size = 1)
        {
            return GUILayout.Button(label, GUILayout.MaxWidth(size * SBUTTON_SIZE + Mathf.Max(size - 1, 0) * SBUTTON_SPACE));
        }

        protected void ProcessCreate(AMeshOwner target)
        {
            target.Create();
            if (_destroyRenderersAtBake)
            {
                if (target.InputRenderers != null)
                    Undo.RecordObjects(target.InputRenderers, "Destroy Input Renderers");
                target.CleanInputRenderers();
            }
            else if (_disableRenderersAtBake)
            {
                if (target.InputRenderers != null)
                    Undo.RecordObjects(target.InputRenderers, "Disable Input Renderers");
                target.SetEnableInputRenderers(false);
            }

            if (target.OutputGameObject != null)
                Undo.RegisterCreatedObjectUndo(target.OutputGameObject, "New Output Objects");
        }

        protected void ProcessDeleteCreated(AMeshOwner target)
        {
            if (target.OutputRenderers != null && target.OutputRenderers.Length > 0 && target.OutputGameObject != null)
            {
                Undo.RecordObject(target.OutputGameObject, "Output Renderers Delete");
                Undo.RecordObjects(target.OutputRenderers, "Output Renderers Delete");
            }

            target.CleanOutputRenderers();

            if (target.InputRenderers != null)
            {
                Undo.RecordObjects(target.InputRenderers, "Re-enable Input Renderers");
                target.SetEnableInputRenderers(true);
            }
        }


        protected void DrawFetchInputData()
        {
            _showAdvancedFetchInputs = GUIEditorExtensions.BeginCollapseGroup("Input Data", _showAdvancedFetchInputs, out bool doResetOptions);
            if (doResetOptions) _overwriteExistingInput = false;

            GUI.color = GetPositiveColor();
            GUI.enabled = !_isEditModeEnabled;
            if (GUILayout.Button("Update Input Data"))
            {
                Undo.RecordObjects(_targets, "Input Data Change");
                foreach (var target in _targets) target.UpdateMeshObjectInputData(_overwriteExistingInput);
            }
            GUIColorReset();

            if (_showAdvancedFetchInputs)
            {
                _overwriteExistingInput = EditorGUILayout.ToggleLeft("Overwrite and replace existing input data", _overwriteExistingInput);
                GUI.enabled = true;

                var useLodProp = serializedObject.FindProperty("_useLod");
                if (useLodProp != null && useLodProp.boolValue && _targets.Length == 1)
                {
                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField("LOD Levels (0 = most detailed)", EditorStyles.boldLabel);

                    var inputData = _target.MeshObjectInputData;
                    if (inputData != null && inputData.Length > 0)
                    {
                        bool lodChanged = false;
                        for (int i = 0; i < inputData.Length; i++)
                        {
                            var entry = inputData[i];
                            string entryLabel = string.IsNullOrEmpty(entry.Name) ? ("Group " + i) : entry.Name;
                            if (entry.Mesh != null) entryLabel += "  (" + entry.Mesh.name + ")";

                            var newLevel = (MeshObjectFactoryLodLevel)EditorGUILayout.EnumPopup(
                                new GUIContent(entryLabel),
                                entry.LodLevel);
                            if (newLevel != entry.LodLevel)
                            {
                                entry.LodLevel = newLevel;
                                inputData[i] = entry;
                                lodChanged = true;
                            }
                        }
                        if (lodChanged)
                        {
                            _target.SetInputData(inputData);
                            EditorUtility.SetDirty(_target);
                            serializedObject.Update();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Run 'Update Input Data' first to see the groups.", MessageType.Info);
                    }
                }
            }
            GUI.enabled = true;
            GUIEditorExtensions.EndCollapseGroup();
        }


        protected void DrawMergeSection()
        {
            bool isSplittable = _target.IsSplittable();
            string sectionLabel = isSplittable ? "Split" : "Merge";

            _showAdvancedCreateMesh = GUIEditorExtensions.BeginCollapseGroup(sectionLabel, _showAdvancedCreateMesh, out bool doResetOptions);
            if (doResetOptions)
            {
                _useNewInstanceForCreate = true;
                _disableRenderersAtBake = true;
                _destroyRenderersAtBake = false;
                _autoCreateOnEditComplete = true;
            }

            bool hasInput = _target.MeshObjectInputData != null && _target.MeshObjectInputData.Length > 0;

            bool hasOutput = _target.OutputRenderers != null && _target.OutputRenderers.Length > 0;
            GUI.enabled = !_isEditModeEnabled && hasInput;
            GUI.color = Color.Lerp(_defaultColor, Color.green, 0.25f);
            string mergeTooltip = isSplittable
                ? "Split each submesh into a separate child GameObject"
                : "Combine all child meshes into a single mesh. Materials are preserved (use Bake buttons to merge materials too).";
            string actionLabel = hasOutput
                ? (isSplittable ? "Update Split" : "Update Merge")
                : (isSplittable ? "Split Now" : "Merge Now");
            if (GUILayout.Button(new GUIContent(actionLabel, mergeTooltip), GUILayout.Height(30)))
            {
                foreach (var target in _targets)
                {
                    ProcessCreate(target);
                    if (_disableRenderersAtBake)
                        target.SetEnableInputRenderers(false);
                }
                RefreshStatistics();
            }
            GUIColorReset();
            GUI.enabled = true;

            if (_showAdvancedCreateMesh)
            {
                EditorGUILayout.LabelField(isSplittable
                    ? "Split multi-submesh meshes into separate GameObjects"
                    : "Combine all child meshes into a single mesh", EditorStyles.miniLabel);
            }

            if (!isSplittable)
            {
                GUI.enabled = !_isEditModeEnabled && hasInput;
                string texArrLabel = hasOutput ? "Update Bake (Texture Array)" : "Bake to 1 Material (Texture Array)";
                string atlasLabel = hasOutput ? "Update Bake (Atlas)" : "Bake to 1 Material (Atlas)";
                string texArrTooltip = "Creates a Texture2DArray from all input materials. Each material becomes a slice. UV.z encodes the slice index. Supports tiling textures. Custom shader: maleric/TextureArray/URP.";
                string atlasTooltip = "Packs all textures into a single atlas texture. UVs are remapped to cell positions. Best for non-tiling textures. Uses standard Lit shader.";

                GUI.color = Color.Lerp(_defaultColor, new Color(0.2f, 0.5f, 1f), 0.2f);
                if (GUILayout.Button(new GUIContent(texArrLabel, texArrTooltip), GUILayout.Height(26)))
                {
                    BakeToTextureArray();
                    RefreshStatistics();
                }
                GUIColorReset();

                GUI.color = Color.Lerp(_defaultColor, new Color(0.6f, 0.3f, 0.8f), 0.25f);
                if (GUILayout.Button(new GUIContent(atlasLabel, atlasTooltip), GUILayout.Height(26)))
                {
                    BakeToAtlas();
                    RefreshStatistics();
                }
                GUIColorReset();

                GUI.enabled = true;

                if (_showAdvancedCreateMesh)
                {
                    EditorGUILayout.LabelField("Texture Array: each material keeps its own texture slice (UV.z encoding). Atlas: all textures packed into one image (UV offset).", EditorStyles.miniLabel);
                }
            }

            var useLodProp = serializedObject.FindProperty("_useLod");
            if (useLodProp != null)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.PropertyField(useLodProp, new GUIContent("Use LOD Group"));
                if (useLodProp.boolValue)
                {
                    EditorGUILayout.HelpBox("Assign each input group a LOD level in the Input Data section. LOD 0 is the most detailed; lower LODs only include groups with LodLevel >= that level.", MessageType.Info);
                }
            }

            EditorGUILayout.Space(3);
            bool newDisable = EditorGUILayout.ToggleLeft("Disable input renderers after create", _disableRenderersAtBake);
            if (newDisable != _disableRenderersAtBake)
            {
                _disableRenderersAtBake = newDisable;
                _destroyRenderersAtBake = newDisable ? false : _destroyRenderersAtBake;
            }

            if (_target.OutputRenderers != null && _target.OutputRenderers.Length > 0)
            {
                GUI.color = Color.Lerp(_defaultColor, Color.red, 0.2f);
                if (GUILayout.Button("Remove Created Output"))
                {
                    foreach (var target in _targets)
                        ProcessDeleteCreated(target);
                }
                GUIColorReset();
            }

            if (_showAdvancedCreateMesh)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Input Renderer Options", EditorStyles.boldLabel);

				bool isAnyInput = _target.InputRenderers != null && _target.InputRenderers.Length > 0;
				EditorGUILayout.BeginHorizontal();
                _disableRenderersAtBake = EditorGUILayout.ToggleLeft("Disable On Create", _disableRenderersAtBake);
				GUI.enabled = isAnyInput && !_isEditModeEnabled;
				if (SButton("Enable")) { Undo.RecordObjects(_targets, "Enable"); foreach (var t in _targets) t.SetEnableInputRenderers(true); }
				if (SButton("Disable")) { Undo.RecordObjects(_targets, "Disable"); foreach (var t in _targets) t.SetEnableInputRenderers(false); }
				EditorGUILayout.EndHorizontal();
				GUI.enabled = true;
				if (_destroyRenderersAtBake) _disableRenderersAtBake = false;
                if (_disableRenderersAtBake) _destroyRenderersAtBake = false;

                EditorGUILayout.BeginHorizontal();
				_destroyRenderersAtBake = EditorGUILayout.ToggleLeft("Destroy On Create", _destroyRenderersAtBake);
				GUI.enabled = isAnyInput && !_isEditModeEnabled;
				if (SButton("Destroy", 2)) { Undo.RecordObjects(_targets, "Destroy"); foreach (var t in _targets) t.CleanInputRenderers(); }
				EditorGUILayout.EndHorizontal();
                GUI.enabled = true;

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Output Options", EditorStyles.boldLabel);
                _useNewInstanceForCreate = EditorGUILayout.ToggleLeft("Create to new GameObject", _useNewInstanceForCreate);
                foreach (var target in _targets)
                    if (_useNewInstanceForCreate != target.UseNewInstance)
                        target.UseNewInstance = _useNewInstanceForCreate;
                _autoCreateOnEditComplete = EditorGUILayout.ToggleLeft("Auto-create on edit complete", _autoCreateOnEditComplete);

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Batch Processing", EditorStyles.boldLabel);
                if (GUILayout.Button("Process All MeshCombiners in Scene"))
                {
                    BatchProcessAll();
                }
                EditorGUILayout.LabelField("Finds all Mesh Combiner components in the scene and runs Update + Merge on each.", EditorStyles.miniLabel);

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Quick Links", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Open Rules DB"))
                {
                    var db = BakeableMaterialRulesDefinition.Instance;
                    if (db != null) Selection.activeObject = db;
                    else EditorUtility.DisplayDialog("Not Found", "No BakeableMaterialRulesDefinition found.\nRun 'Bake to 1 Mat' first to create one.", "OK");
                }
                if (GUILayout.Button("Texture Array Creator"))
                {
                    TextureArrayCreatorWindow.ShowWindow();
                }
                EditorGUILayout.EndHorizontal();
            }

            GUI.enabled = true;
            GUIEditorExtensions.EndCollapseGroup();
        }


        protected void DrawStatistics()
        {
            _showAdvancedStatistics = GUIEditorExtensions.BeginCollapseGroup("Statistics & Info", _showAdvancedStatistics);

            if (_rules == null || _currentMaterials == null)
                RefreshStatistics();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            bool hasOutput = _target.OutputRenderers != null && _target.OutputRenderers.Length > 0;
            string beforeAfter;
            if (hasOutput)
            {
                int outputCalls = 0;
                foreach (var r in _target.OutputRenderers)
                    if (r != null && r.sharedMaterials != null)
                        outputCalls += r.sharedMaterials.Length;
                beforeAfter = "Draw Calls  →  " + outputCalls + "  (merged output)";
            }
            else
            {
                beforeAfter = "Draw Calls:  " + _currentTotalDrawCallCount + "  →  After  " + _newTotalDrawCallCount.ToString();
            }
            EditorGUILayout.LabelField(beforeAfter);

            string newMats = "New Materials:  ";
            if (_newMaterials.Count > 0)
            {
                for (int i = 0; i < _newMaterials.Count; i++)
                    newMats += _newMaterials[i].name + (i < _newMaterials.Count - 1 ? ", " : "");
            }
            else
            {
                newMats += "Same as original (no rules applied)";
            }
            EditorGUILayout.LabelField(newMats, EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();

            if (_showAdvancedStatistics)
            {
                if (GUILayout.Button("Refresh"))
                    RefreshStatistics();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Material Mappings", EditorStyles.boldLabel);

                for (int i = 0; i < _currentMaterials.Count; i++)
                {
                    string guid = _currentMaterials[i];
                    bool isFound = _rules != null && _rules.ContainsKey(guid);
                    Material mat = ReferenceAttributeDrawer.GetObjectByGuid<Material>(guid);

                    if (mat == null)
                    {
                        GUI.color = GetNegativeColor();
                        EditorGUILayout.HelpBox("[Missing] GUID: " + guid.Substring(0, 10) + "...", MessageType.Error, true);
                        GUIColorReset();
                    }
                    else if (isFound)
                    {
                        Material newMat = _rules[guid].GetBakeableMaterial();
                        EditorGUILayout.LabelField(mat.name + "  →  " + newMat.name, EditorStyles.miniLabel);
                    }
                    else
                    {
                        GUI.color = Color.Lerp(_defaultColor, Color.yellow, 0.3f);
                        EditorGUILayout.LabelField(mat.name + "  →  (no rule, kept as-is)", EditorStyles.miniLabel);
                        GUIColorReset();
                    }
                }
                EditorGUILayout.EndVertical();
            }

            GUI.enabled = true;
            GUIEditorExtensions.EndCollapseGroup();
        }


        protected void DrawEditorMode()
        {
            _isEditModeEnabled = _isEditModeEnabled || _target.EditModeMeshRenderers != null && _target.EditModeMeshRenderers.Length > 0;

            bool newEditModeValue = _isEditModeEnabled;
            if (GUILayout.Button(_isEditModeEnabled ? "Disable Edit Mode" : "Enable Edit Mode"))
                newEditModeValue = !_isEditModeEnabled;

            if (newEditModeValue != _isEditModeEnabled)
            {
                _isEditModeEnabled = newEditModeValue;

                if (_isEditModeEnabled)
                {
                    _isInputRenderersEnabled = _target.InputRenderers.IsFirstRendererEnabled();
                    _isOutputRenderersEnabled = _target.OutputRenderers.IsFirstRendererEnabled();
                    _target.SetEnableInputRenderers(false);
                    _target.SetEnableOutputRenderers(false);

                    if (_target.EditModeMeshRenderers == null || _target.EditModeMeshRenderers.Length == 0)
                    {
                        _target.EditModeMeshRenderers = new MeshRenderer[_target.MeshObjectInputData.Length];
                        for (int i = 0; i < _target.MeshObjectInputData.Length; i++)
                            _target.EditModeMeshRenderers[i] = _target.MeshObjectInputData[i].ToMeshRendererOnNewObject("EDIT_", _target.transform);
                    }
                }
                else
                {
                    if (_isInputRenderersEnabled)
                        _target.SetEnableInputRenderers(true);

                    if (_isOutputRenderersEnabled)
                        _target.SetEnableOutputRenderers(true);

                    if (_target.EditModeMeshRenderers != null && _target.EditModeMeshRenderers.Length > 0)
                    {
                        Vector3 parentOffset = _target.GetParentDifferences(_target.EditModeMeshRenderers);
                        _target.transform.position -= parentOffset;
                        List<EditModeRendererData> editRenderers = new List<EditModeRendererData>();
                        int addToBegginingCount = 0;
                        for (int i = 0; i < _target.EditModeMeshRenderers.Length; i++)
                        {
                            int targetIndex = MeshObjectUtilities.GetInputRendererIndex(_target, _target.EditModeMeshRenderers[i], i);
                            var elementToAdd = new EditModeRendererData()
                            {
                                InputRendererIndex = targetIndex,
                                EditModeRenderer = _target.EditModeMeshRenderers[i],
                                EditModeIndex = i
                            };
                            if (_target.InputRenderers != null && targetIndex > -1)
                            {
                                var childrenComponent = _target.InputRenderers[targetIndex].GetComponent<MeshRenderer>();
                                if (childrenComponent != _target.InputRenderers[targetIndex])
                                    editRenderers.Insert(addToBegginingCount++, elementToAdd);
                                else
                                    editRenderers.Add(elementToAdd);
                            }
                            else
                                editRenderers.Add(elementToAdd);
                        }

                        int count = editRenderers.Count;
                        for (int i = 0; i < count; i++)
                        {
                            if (editRenderers[i].EditModeRenderer != null)
                            {
                                if (_target.MeshObjectInputData.Length > editRenderers[i].EditModeIndex)
                                {
                                    editRenderers[i].EditModeRenderer.transform.position += parentOffset;
                                    _target.MeshObjectInputData[i] = _target.MeshObjectInputData[i].UpdateInputData(editRenderers[i].EditModeRenderer, _target.GameObject.transform);
                                    _target.UpdateInputRenderers(editRenderers[i].EditModeRenderer, editRenderers[i].InputRendererIndex, editRenderers[i].EditModeIndex);
                                }
                                MeshObjectUtilities.Destroy(editRenderers[i].EditModeRenderer.gameObject);
                            }
                        }
                        _target.EditModeMeshRenderers = null;
                    }

                    if (_autoCreateOnEditComplete && _isOutputRenderersEnabled)
                        ProcessCreate(_target);
                }
            }

            GUI.enabled = true;
        }


        private void BakeToTextureArray()
        {
            var materials = new List<Material>();
            var materialSet = new HashSet<string>();

            foreach (var target in _targets)
            {
                if (target.MeshObjectInputData == null) continue;
                foreach (var data in target.MeshObjectInputData)
                {
                    if (data.Materials == null) continue;
                    foreach (var guid in data.Materials)
                    {
                        if (string.IsNullOrEmpty(guid) || materialSet.Contains(guid)) continue;
                        materialSet.Add(guid);
                        var mat = ReferenceAttributeDrawer.GetObjectByGuid<Material>(guid);
                        if (mat != null && !materials.Contains(mat))
                            materials.Add(mat);
                    }
                }
            }

            if (materials.Count == 0)
            {
                EditorUtility.DisplayDialog("No Materials", "Could not find any materials in the input data.\nMake sure 'Update Input Data' has been run.", "OK");
                return;
            }

            string matList = "";
            foreach (var m in materials) matList += "  - " + m.name + "\n";
            bool proceed = EditorUtility.DisplayDialog("Bake to Texture Array",
                "This will create a Texture2DArray from " + materials.Count + " materials:\n" + matList +
                "\nAfter baking, the merged mesh will use 1 material (1 draw call).\n\nProceed?",
                "Bake", "Cancel");

            if (!proceed) return;

            EditorUtility.DisplayProgressBar("Baking to Texture Array", "Creating texture array...", 0.2f);

            bool success = TextureArrayMaterialCreator.CreateTextureArrayFromMaterials(
                materials,
                out Texture2DArray texArray,
                out Material arrayMat,
                out int[] sliceIndices);

            if (!success)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", "Failed to create texture array. Check console for details.", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("Baking to Texture Array", "Generating material rules...", 0.6f);

            TextureArrayRuleAutoGenerator.GenerateRules(
                materials.ToArray(),
                arrayMat,
                sliceIndices);

            EditorUtility.DisplayProgressBar("Baking to Texture Array", "Merging...", 0.8f);

            foreach (var target in _targets)
            {
                target.UpdateMeshOwnerMaterialsWithoutRules();
                ProcessCreate(target);
            }

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Bake Complete",
                "Texture array created with " + texArray.depth + " slices.\n" +
                materials.Count + " materials → 1 material (" + materials.Count + " draw calls → 1 draw call!).\n\n" +
                "Created assets are in " + TextureArrayMaterialCreator.DefaultOutputPath,
                "OK");
        }

        private void BakeToAtlas()
        {
            var materials = new List<Material>();
            var materialSet = new HashSet<string>();

            foreach (var target in _targets)
            {
                if (target.MeshObjectInputData == null) continue;
                foreach (var data in target.MeshObjectInputData)
                {
                    if (data.Materials == null) continue;
                    foreach (var guid in data.Materials)
                    {
                        if (string.IsNullOrEmpty(guid) || materialSet.Contains(guid)) continue;
                        materialSet.Add(guid);
                        var mat = ReferenceAttributeDrawer.GetObjectByGuid<Material>(guid);
                        if (mat != null && !materials.Contains(mat))
                            materials.Add(mat);
                    }
                }
            }

            if (materials.Count == 0)
            {
                EditorUtility.DisplayDialog("No Materials", "Could not find any materials.\nMake sure 'Update Input Data' has been run.", "OK");
                return;
            }

            string matList = "";
            foreach (var m in materials) matList += "  - " + m.name + "\n";
            bool proceed = EditorUtility.DisplayDialog("Bake to Atlas",
                "This will create a texture atlas from " + materials.Count + " materials:\n" + matList +
                "\nAfter baking, the merged mesh will use 1 material (1 draw call).\n\nProceed?",
                "Bake", "Cancel");
            if (!proceed) return;

            EditorUtility.DisplayProgressBar("Baking to Atlas", "Creating atlas...", 0.2f);

            bool success = AtlasMaterialCreator.CreateAtlasFromMaterials(
                materials,
                out Texture2D atlasTex,
                out Material atlasMat,
                out Vector2[] uvOffsets,
                out Vector2[] uvScales,
                out int cols,
                out int rows);

            if (!success)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", "Failed to create atlas. Check console.", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("Baking to Atlas", "Generating rules...", 0.6f);

            AtlasRuleAutoGenerator.GenerateRules(
                materials.ToArray(),
                atlasMat,
                uvOffsets,
                uvScales);

            EditorUtility.DisplayProgressBar("Baking to Atlas", "Merging...", 0.8f);

            foreach (var target in _targets)
            {
                target.UpdateMeshOwnerMaterialsWithoutRules();
                ProcessCreate(target);
            }

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Bake Complete",
                "Atlas created (" + cols + "x" + rows + " grid).\n" +
                materials.Count + " materials → 1 material (" + materials.Count + " draw calls → 1 draw call!).\n\n" +
                "Created assets are in " + AtlasMaterialCreator.DefaultOutputPath,
                "OK");
        }

        private void BatchProcessAll()
        {
#pragma warning disable CS0618
            var allOwners = FindObjectsByType<AMeshOwner>(FindObjectsSortMode.None);
#pragma warning restore CS0618
            if (allOwners.Length == 0)
            {
                EditorUtility.DisplayDialog("Batch Process", "No Mesh Combiner/Splitter components found in the scene.", "OK");
                return;
            }

            bool proceed = EditorUtility.DisplayDialog("Batch Process",
                "Found " + allOwners.Length + " Mesh Combiner/Splitter components.\n" +
                "This will update input data and create/update merged output for all of them.\n\nProceed?",
                "Process All", "Cancel");
            if (!proceed) return;

            for (int i = 0; i < allOwners.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Batch Processing",
                    "Processing " + allOwners[i].name + " (" + (i + 1) + "/" + allOwners.Length + ")",
                    (float)i / allOwners.Length);

                allOwners[i].UpdateMeshObjectInputData(true);
                allOwners[i].Create();

                if (_disableRenderersAtBake)
                    allOwners[i].SetEnableInputRenderers(false);
            }

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Batch Process Complete",
                "Processed " + allOwners.Length + " components.\n" +
                "Input data updated and output meshes created for each.",
                "OK");
        }


        protected void RefreshStatistics()
        {
            if (_rules == null)
            {
                _newMaterials = new List<Material>();
                _rules = new Dictionary<string, IBakeableMaterialRule>();
                _currentMaterials = new List<string>();
            }
            else
            {
                _rules.Clear();
                _newMaterials.Clear();
                _currentMaterials.Clear();
            }
            _currentTotalDrawCallCount = 0;
            _newTotalDrawCallCount = 0;
            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i].MeshObjectInputData != null)
                {
                    for (int j = 0; j < _targets[i].MeshObjectInputData.Length; j++)
                    {
                        foreach (var mat in _targets[i].MeshObjectInputData[j].Materials)
                        {
                            _currentTotalDrawCallCount++;
                            if (_targets[i].IsSplittable()) _newTotalDrawCallCount++;
                            if (!_currentMaterials.Contains(mat))
                                _currentMaterials.Add(mat);
                        }
                    }
                }
                var rules = MaterialUtilities.GetMaterialRules(_targets[i].MeshObjectInputData, out List<Material> newMaterials, _targets[i].IsSplittable());
                if (!_targets[i].IsSplittable()) _newTotalDrawCallCount += newMaterials.Count;

                foreach (var kvp in rules)
                {
                    if (!_rules.ContainsKey(kvp.Key))
                        _rules.Add(kvp.Key, kvp.Value);

                    var bakeableMaterial = kvp.Value.GetBakeableMaterial();
                    if (!_newMaterials.Contains(bakeableMaterial) && bakeableMaterial != null)
                        _newMaterials.Add(bakeableMaterial);
                }
            }
        }

        public struct EditModeRendererData
        {
            public int InputRendererIndex;
            public int EditModeIndex;
            public MeshRenderer EditModeRenderer;

            public EditModeRendererData(int inputRendererIndex, int editModeIndex, MeshRenderer renderer)
            {
                InputRendererIndex = inputRendererIndex;
                EditModeIndex = editModeIndex;
                EditModeRenderer = renderer;
            }
        }

        public Color GetStatisticsColor() => Color.Lerp(_defaultColor, Color.cyan, 0.10f);
        public Color GetPositiveColor() => Color.Lerp(_defaultColor, Color.green, 0.23f);
        public Color GetNegativeColor() => Color.Lerp(_defaultColor, Color.red, 0.2f);
        public Color GetOddColor() => Color.Lerp(_defaultColor, Color.gray, 0.1f);
        public void GUIColorReset() => GUI.color = _defaultColor;
    }
}
