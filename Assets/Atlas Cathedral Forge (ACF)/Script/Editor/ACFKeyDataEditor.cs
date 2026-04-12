using System;
using UnityEditor;
using UnityEngine;

namespace ACFSystem
{
    [CustomEditor(typeof(ACFKeyData))]
    [CanEditMultipleObjects]
    public class ACFKeyDataEditor : Editor
    {
        private SerializedProperty keyId;
        private SerializedProperty displayName;

        private void OnEnable()
        {
            keyId = serializedObject.FindProperty("keyId");
            displayName = serializedObject.FindProperty("displayName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("ACF Key Data", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox($"Editing {targets.Length} keys simultaneously.", MessageType.Info);
                if (GUILayout.Button("Generate Unique IDs"))
                {
                    foreach (UnityEngine.Object obj in targets)
                    {
                        ACFKeyData key = (ACFKeyData)obj;
                        Undo.RecordObject(key, "Generate Unique Key IDs");
                        key.keyId = $"{key.gameObject.name}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                        EditorUtility.SetDirty(key);
                    }
                }

                EditorGUILayout.Space();
            }

            EditorGUILayout.PropertyField(keyId);
            EditorGUILayout.PropertyField(displayName);

            if (targets.Length == 1 && GUILayout.Button("Auto Configure From Object Name"))
            {
                ACFKeyData key = (ACFKeyData)target;
                Undo.RecordObject(key, "Auto Configure Key Data");
                key.AutoConfigureFromObjectName();
                EditorUtility.SetDirty(key);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
