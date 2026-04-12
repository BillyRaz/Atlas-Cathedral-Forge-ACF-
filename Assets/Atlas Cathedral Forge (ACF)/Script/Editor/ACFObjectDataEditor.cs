using UnityEditor;
using UnityEngine;

namespace ACFSystem
{
    [CustomEditor(typeof(ACFObjectData))]
    [CanEditMultipleObjects]
    public class ACFObjectDataEditor : Editor
    {
        private SerializedProperty category;
        private SerializedProperty customCategory;
        private SerializedProperty blockoutType;
        private SerializedProperty isBlockout;
        private SerializedProperty finalPrefab;
        private SerializedProperty preserveTransform;
        private SerializedProperty snapToGrid;
        private SerializedProperty gridSize;
        private SerializedProperty notes;
        private SerializedProperty scannedUnityTag;
        private SerializedProperty scannedUnityLayer;
        private SerializedProperty assignedCategoryTag;
        private SerializedProperty assignedCategoryLayer;

        private void OnEnable()
        {
            category = serializedObject.FindProperty("category");
            customCategory = serializedObject.FindProperty("customCategory");
            blockoutType = serializedObject.FindProperty("blockoutType");
            isBlockout = serializedObject.FindProperty("isBlockout");
            finalPrefab = serializedObject.FindProperty("finalPrefab");
            preserveTransform = serializedObject.FindProperty("preserveTransform");
            snapToGrid = serializedObject.FindProperty("snapToGrid");
            gridSize = serializedObject.FindProperty("gridSize");
            notes = serializedObject.FindProperty("notes");
            scannedUnityTag = serializedObject.FindProperty("scannedUnityTag");
            scannedUnityLayer = serializedObject.FindProperty("scannedUnityLayer");
            assignedCategoryTag = serializedObject.FindProperty("assignedCategoryTag");
            assignedCategoryLayer = serializedObject.FindProperty("assignedCategoryLayer");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("ACF Object Data", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (targets.Length > 1)
                EditorGUILayout.HelpBox($"Editing {targets.Length} objects simultaneously.", MessageType.Info);

            EditorGUILayout.PropertyField(category);
            if (!category.hasMultipleDifferentValues &&
                category.enumValueIndex == (int)ACFObjectData.ObjectCategory.Custom)
            {
                EditorGUILayout.PropertyField(customCategory);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Blockout Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(blockoutType);
            EditorGUILayout.PropertyField(isBlockout);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Replacement Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(finalPrefab);
            EditorGUILayout.PropertyField(preserveTransform);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Transform Presets", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(snapToGrid);
            if (!snapToGrid.hasMultipleDifferentValues && snapToGrid.boolValue)
                EditorGUILayout.PropertyField(gridSize);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Diagnostic Notes", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(notes);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Unity Sync", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(scannedUnityTag);
                EditorGUILayout.PropertyField(scannedUnityLayer);
                EditorGUILayout.PropertyField(assignedCategoryTag);
                EditorGUILayout.PropertyField(assignedCategoryLayer);
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Mark as Blockout"))
            {
                foreach (UnityEngine.Object obj in targets)
                {
                    ACFObjectData data = (ACFObjectData)obj;
                    Undo.RecordObject(data, "Mark as Blockout");
                    data.isBlockout = true;
                    EditorUtility.SetDirty(data);
                }
            }

            if (GUILayout.Button("Set Category from Name"))
            {
                foreach (UnityEngine.Object obj in targets)
                {
                    ACFObjectData data = (ACFObjectData)obj;
                    if (!ACFCategoryUtility.TryInferCategory(data.gameObject, out string inferredCategory))
                        continue;

                    Undo.RecordObject(data, "Set Category from Name");
                    ApplyCategoryToData(data, inferredCategory);
                    EditorUtility.SetDirty(data);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Snap to Grid"))
            {
                foreach (UnityEngine.Object obj in targets)
                {
                    ACFObjectData data = (ACFObjectData)obj;
                    Undo.RecordObject(data.transform, "Snap to Grid");
                    data.SnapToGrid();
                    EditorUtility.SetDirty(data);
                }
            }
        }

        private static void ApplyCategoryToData(ACFObjectData data, string categoryName)
        {
            switch (categoryName)
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
                default:
                    data.category = ACFObjectData.ObjectCategory.Custom;
                    data.customCategory = categoryName;
                    break;
            }
        }
    }
}
