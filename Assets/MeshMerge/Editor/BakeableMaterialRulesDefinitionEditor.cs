using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace maleric
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BakeableMaterialRulesDefinition), true)]
    public class BakeableMaterialRulesDefinitionEditor : Editor
    {
        ABakeableMaterialRuleDefinition[] _rules;
        List<int> _selectedRuleIndexes = new List<int>();
        List<string> _takenNames = new List<string>();
        int[] _selectedRuleTypes = new int[0];
        Dictionary<ABakeableMaterialRuleDefinition, string> _newRuleNames = new Dictionary<ABakeableMaterialRuleDefinition, string>();

        int _ruleCreationRuleIndex = 0;
        Material _ruleCreationMaterial;
        string _ruleCreationName;

        string _defaultAssetPath = "Assets/newAsset.asset";

        bool _fetchRules = false;

        bool _rulesListFoldout = true;
        bool _ruleCreatorFoldout = false;
        bool _defaultRulesFoldout = false;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var rulesTarget = (BakeableMaterialRulesDefinition)target;

            _defaultRulesFoldout = EditorGUILayout.Foldout(_defaultRulesFoldout, "Default Rules");
            if (_defaultRulesFoldout)
            {
                EditorGUILayout.HelpBox("Materials that dont have any rule defined.\nThose materials will be included in the build.", MessageType.Info);
                EditorGUILayout.IntField("Materials Without Rule", rulesTarget._defaultMaterialRules.Count);

                EditorGUI.indentLevel++;
                for (int i = 0; i < rulesTarget._defaultMaterialRules.Count; i++)
                {
                    EditorGUILayout.ObjectField(new GUIContent("Material " + i + " (" + rulesTarget._defaultMaterialRules[i].MaterialGuid.Substring(0, 10) + ")",
                        rulesTarget._defaultMaterialRules[i].MaterialGuid),
                        rulesTarget._defaultMaterialRules[i].Rule.Material, typeof(Material), false);
                }
                EditorGUI.indentLevel--;

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Refetch (current scene)"))
                {
#pragma warning disable CS0618
                    var meshes = FindObjectsByType<MergeableMeshOwner>(FindObjectsSortMode.None);
#pragma warning restore CS0618
                    for (int i = 0; i < meshes.Length; i++)
                    {
                        meshes[i].UpdateMeshOwnerMaterialsWithoutRules();
                    }
                }

                if (GUILayout.Button("Clear"))
                {
                    rulesTarget._defaultMaterialRules.Clear();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            _rulesListFoldout = EditorGUILayout.Foldout(_rulesListFoldout, "Found Rules", true);
            if ((_rulesListFoldout && GUILayout.Button("Fetch rules", GUILayout.MaxWidth(80))) || _rules == null || _fetchRules)
            {
                _rules = Resources.LoadAll<ABakeableMaterialRuleDefinition>("");
                _selectedRuleTypes = new int[_rules.Length];
            }
            EditorGUILayout.EndHorizontal();
            if (_rulesListFoldout)
            {
                EditorGUILayout.HelpBox("Rules that define change from one material to another.\nImportant: Old replaced materials will not be included in the build.", MessageType.Info);
                _takenNames.Clear();

                if (_rules != null)
                {
                    for (int i = 0; i < _rules.Length; i++)
                    {
                        DrawRuleArray(_rules[i], i);

                        if (_selectedRuleIndexes.Contains(i))
                        {
                            DrawRuleSelection(_rules[i], i);
                        }
                    }
                }
            }

            EditorGUILayout.Space();
            _ruleCreatorFoldout = EditorGUILayout.Foldout(_ruleCreatorFoldout, "Rule Creator", true);
            if (_ruleCreatorFoldout)
            {
                DrawRuleCreation(_defaultAssetPath);
            }
        }

        void DrawRuleArray(ABakeableMaterialRuleDefinition rule, int index)
        {
            if (rule == null) return;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(rule, typeof(ABakeableMaterialRuleDefinition), false);
            _takenNames.Add(rule.name);

            Color tmpColor = GUI.color;
            if (_selectedRuleIndexes.Contains(index))
                GUI.color = Color.Lerp(tmpColor, Color.green, 0.2f);
            else if (rule.GetRule() == null || rule.GetRule().GetBakeableMaterial() == null || string.IsNullOrEmpty(rule.MaterialGuid))
                GUI.color = Color.Lerp(tmpColor, Color.red, 0.2f);

            if (GUILayout.Button("Select", GUILayout.MaxWidth(74)))
            {
                if (_selectedRuleIndexes.Contains(index))
                    _selectedRuleIndexes.Remove(index);
                else
                    _selectedRuleIndexes.Add(index);
            }
            GUI.color = tmpColor;

            if (GUILayout.Button(new GUIContent("+", "Clones the Rule"), GUILayout.MaxWidth(22)))
            {
                CloneRule(rule);
                _fetchRules = true;
            }

            if (GUILayout.Button(new GUIContent("-", "Removes the Rule"), GUILayout.MaxWidth(22)))
            {
                DeleteRule(rule);
                _fetchRules = true;
            }

            if (GUILayout.Button(new GUIContent("..\\", "Copies Path and paste it to Rule Creator"), GUILayout.MaxWidth(22)))
            {
                _ruleCreationName = GetNewPath(AssetDatabase.GetAssetPath(rule), "myNewRule");
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawRuleSelection(ABakeableMaterialRuleDefinition rule, int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            if (!_newRuleNames.ContainsKey(rule))
                _newRuleNames.Add(rule, rule.name);
            _newRuleNames[rule] = EditorGUILayout.TextField("Name", _newRuleNames[rule], EditorStyles.boldLabel);

            if (!_newRuleNames[rule].Equals(rule.name) && !_takenNames.Contains(_newRuleNames[rule]))
            {
                if (GUILayout.Button("Rename", GUILayout.MaxWidth(70f)))
                {
                    AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(rule), _newRuleNames[rule]);
                    rule.name = _newRuleNames[rule];
                }

                if (GUILayout.Button("Cancel", GUILayout.MaxWidth(70f)))
                {
                    _newRuleNames[rule] = rule.name;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _selectedRuleTypes[index] = DrawRulesPopup("Change Rule Type To", _selectedRuleTypes[index], out Type[] ruleTypes);
            if (GUILayout.Button("Change", GUILayout.MaxWidth(70f)))
            {
                _rules[index] = ReplaceRule(rule, ruleTypes[_selectedRuleTypes[index]]);
                return;
            }
            EditorGUILayout.EndHorizontal();

            CreateEditor(rule).OnInspectorGUI();
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            if (string.IsNullOrEmpty(rule.MaterialGuid))
                EditorGUILayout.HelpBox("Input Material is null. You must slot material you want to replace.", MessageType.Error);
            else if (rule.GetRule().GetBakeableMaterial() == null)
                EditorGUILayout.HelpBox("Output Material for the rule is null. You must slot new material that will be used on new baked object.", MessageType.Error);

            EditorGUILayout.EndVertical();
        }

        ABakeableMaterialRuleDefinition ReplaceRule(ABakeableMaterialRuleDefinition ruleDefinition, Type ruleType)
        {
            string path = AssetDatabase.GetAssetPath(ruleDefinition);
            string usedMaterial = ruleDefinition.MaterialGuid;
            DeleteRule(ruleDefinition);
            return CreateRule(ruleType, path, usedMaterial);
        }

        void CloneRule(ABakeableMaterialRuleDefinition rule)
        {
            string path = AssetDatabase.GetAssetPath(rule);
            var splittedPath = path.Split('.');
            string newPath = "";
            for (int j = 0; j < splittedPath.Length; j++)
                newPath += splittedPath[j] + (j == splittedPath.Length - 2 ? "_new." : "");

            AssetDatabase.CopyAsset(path, newPath);
        }

        void DeleteRule(ABakeableMaterialRuleDefinition rule)
        {
            string path = AssetDatabase.GetAssetPath(rule);
            AssetDatabase.RemoveObjectFromAsset(rule);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
        }

        ABakeableMaterialRuleDefinition CreateRule(Type ruleType, string name, Material material = null)
        {
            return CreateRule(ruleType, name, ReferenceAttributeDrawer.GetObjectGUID(material));
        }

        ABakeableMaterialRuleDefinition CreateRule(Type ruleType, string name, string material = null)
        {
            var newSO = ScriptableObject.CreateInstance(ruleType) as ABakeableMaterialRuleDefinition;
            newSO.SetMaterialGUID(material);

            AssetDatabase.CreateAsset(newSO, name);
            AssetDatabase.SaveAssets();
            return newSO;
        }

        void DrawRuleCreation(string defaultPath)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;

            _ruleCreationRuleIndex = DrawRulesPopup("Rule", _ruleCreationRuleIndex, out Type[] ruleTypes);
            _ruleCreationMaterial = (Material)EditorGUILayout.ObjectField("Original Material", _ruleCreationMaterial, typeof(Material), false);

            if (string.IsNullOrEmpty(_ruleCreationName))
                _ruleCreationName = GetNewPath(defaultPath, "new_rule_" + _rules.Length.ToString());

            _ruleCreationName = EditorGUILayout.TextField("Path", _ruleCreationName);

            if (GUILayout.Button("Create"))
            {
                CreateRule(ruleTypes[_ruleCreationRuleIndex], _ruleCreationName, _ruleCreationMaterial);
                _fetchRules = true;
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        int DrawRulesPopup(string label, int selectedRuleIndex)
        {
            return DrawRulesPopup(label, selectedRuleIndex, out Type[] ruleTypes);
        }

        int DrawRulesPopup(string lablel, int selectedRuleIndex, out Type[] ruleTypes)
        {
            var types = UnityEditor.TypeCache.GetTypesDerivedFrom<ABakeableMaterialRuleDefinition>()
                .Where(p => !p.IsGenericType && !p.IsAbstract);

            ruleTypes = types.ToArray();
            var ruleTypesNames = (string[])Array.ConvertAll(ruleTypes, item => item.ToString());

            return EditorGUILayout.Popup(lablel, selectedRuleIndex, ruleTypesNames);
        }

        string GetNewPath(string defaultPath, string name)
        {
            var splittedPath = defaultPath.Split('.', '/');
            string newPath = "";
            for (int j = 0; j < splittedPath.Length; j++)
            {
                if (j == splittedPath.Length - 2)
                {
                    newPath += name + ".";
                }
                else
                {
                    newPath += splittedPath[j];
                    if (j != splittedPath.Length - 1)
                        newPath += "/";
                }
            }
            return newPath;
        }
    }
}
