using UnityEngine;
using UnityEditor;

namespace ACFSystem
{
    [CustomEditor(typeof(ACFObjectData))]
    public class ACFObjectDataEditor : Editor
    {
        private ACFObjectData data;

        private void OnEnable()
        {
            data = (ACFObjectData)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("ACF Object Data", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Category selection
            EditorGUILayout.PropertyField(serializedObject.FindProperty("category"));

            if (data.category == ACFObjectData.ObjectCategory.Custom)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("customCategory"));
            }

            EditorGUILayout.Space();

            // Blockout settings
            EditorGUILayout.LabelField("Blockout Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("blockoutType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isBlockout"));

            EditorGUILayout.Space();

            // Replacement settings
            EditorGUILayout.LabelField("Replacement Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("finalPrefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("preserveTransform"));

            EditorGUILayout.Space();

            // Notes
            EditorGUILayout.LabelField("Diagnostic Notes", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("notes"));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            // Quick actions
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Mark as Blockout"))
            {
                data.isBlockout = true;
                EditorUtility.SetDirty(data);
            }

            if (GUILayout.Button("Set Category from Name"))
            {
                string name = data.gameObject.name.ToLower();
                if (name.Contains("floor")) data.category = ACFObjectData.ObjectCategory.Floor;
                else if (name.Contains("wall")) data.category = ACFObjectData.ObjectCategory.Wall;
                else if (name.Contains("prop")) data.category = ACFObjectData.ObjectCategory.Prop;
                else if (name.Contains("movable")) data.category = ACFObjectData.ObjectCategory.MovableProp;
                else if (name.Contains("key")) data.category = ACFObjectData.ObjectCategory.Key;
                else if (name.Contains("door")) data.category = ACFObjectData.ObjectCategory.Door;
                else if (name.Contains("roof")) data.category = ACFObjectData.ObjectCategory.Roof;

                EditorUtility.SetDirty(data);
            }
        }
    }
}