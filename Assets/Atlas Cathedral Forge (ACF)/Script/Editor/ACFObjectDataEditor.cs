using UnityEditor;
using UnityEngine;

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

            EditorGUILayout.PropertyField(serializedObject.FindProperty("category"));

            if (data.category == ACFObjectData.ObjectCategory.Custom)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("customCategory"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Blockout Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("blockoutType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isBlockout"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Replacement Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("finalPrefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("preserveTransform"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Diagnostic Notes", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("notes"));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Mark as Blockout"))
            {
                data.isBlockout = true;
                EditorUtility.SetDirty(data);
            }

            if (GUILayout.Button("Set Category from Name") && ACFCategoryUtility.TryInferCategory(data.gameObject, out string category))
            {
                switch (category)
                {
                    case ACFCategoryUtility.Floor: data.category = ACFObjectData.ObjectCategory.Floor; break;
                    case ACFCategoryUtility.Wall: data.category = ACFObjectData.ObjectCategory.Wall; break;
                    case ACFCategoryUtility.Roof: data.category = ACFObjectData.ObjectCategory.Roof; break;
                    case ACFCategoryUtility.Prop: data.category = ACFObjectData.ObjectCategory.Prop; break;
                    case ACFCategoryUtility.MovableProp: data.category = ACFObjectData.ObjectCategory.MovableProp; break;
                    case ACFCategoryUtility.Door: data.category = ACFObjectData.ObjectCategory.Door; break;
                    case ACFCategoryUtility.Key: data.category = ACFObjectData.ObjectCategory.Key; break;
                    case ACFCategoryUtility.Landmark: data.category = ACFObjectData.ObjectCategory.Landmark; break;
                    case ACFCategoryUtility.Ignore: data.category = ACFObjectData.ObjectCategory.Ignore; break;
                }

                EditorUtility.SetDirty(data);
            }
        }
    }
}
