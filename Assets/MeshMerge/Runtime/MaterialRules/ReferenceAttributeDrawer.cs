using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

namespace maleric
{
    public class ReferenceAttribute : PropertyAttribute
    {
        public System.Type ReferenceType { get; private set; }
        public bool ShowGuid { get; private set; }

        public ReferenceAttribute(System.Type type)
        {
            ReferenceType = type;
            ShowGuid = false;
        }

        public ReferenceAttribute(System.Type type, bool showGuid)
        {
            ReferenceType = type;
            ShowGuid = showGuid;
        }
    }

    public class ShowSerializedPropertyAttribute : PropertyAttribute
    {
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ReferenceAttribute))]
    public class ReferenceAttributeDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            var refAttribute = ((ReferenceAttribute)attribute);

            if (property != null)
            {
                string referencePath = AssetDatabase.GUIDToAssetPath(property.stringValue);
                var referenceObj = AssetDatabase.LoadAssetAtPath(referencePath, refAttribute.ReferenceType);

                var newObjReference = EditorGUI.ObjectField(new Rect(position.x, position.y, position.width * (refAttribute.ShowGuid ? 0.7f : 1), position.height),
                    new GUIContent(refAttribute.ReferenceType.Name + " (" + property.stringValue.Substring(0, 10) + ")", property.stringValue),
                    referenceObj,
                    refAttribute.ReferenceType,
                    false);
                if (refAttribute.ShowGuid)
                    EditorGUI.LabelField(new Rect(position.x + position.width * 0.69f, position.y, position.width * 0.3f, position.height), property.stringValue);

                if (newObjReference != null)
                {
                    string guid;
                    long localId;
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(newObjReference, out guid, out localId))
                        property.stringValue = guid;
                }
                else if (newObjReference != referenceObj)
                {
                    property.stringValue = null;
                }
            }

            EditorGUI.EndProperty();
        }

        public static T[] GetObjectsByGuids<T>(string[] guids) where T : Object
        {
            T[] objects = new T[guids.Length];
            for(int i = 0; i < guids.Length; i++)
            {
                objects[i] = GetObjectByGuid<T>(guids[i]);
            }
            return objects;
        }

        public static T GetObjectByGuid<T>(string guid) where T : Object
        {
            string referencePath = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<T>(referencePath);
        }

        public static string GetObjectGUID(Object obj)
        {
            string guid;
            long localId;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out guid, out localId);
            return guid;
        }

    }

    [CustomPropertyDrawer(typeof(ShowSerializedPropertyAttribute))]
    public class ShowSerializedPropertyAttributeDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            EditorGUI.PropertyField(position, property, label);
            if (property.objectReferenceValue != null)
            {
                var editor = Editor.CreateEditor(property.objectReferenceValue);
                editor.OnInspectorGUI();

            }

            EditorGUI.EndProperty();
        }

    }
#endif
}