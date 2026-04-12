using UnityEditor;
using UnityEngine;

namespace ACFSystem
{
    [CustomEditor(typeof(ACFDoorData))]
    [CanEditMultipleObjects]
    public class ACFDoorDataEditor : Editor
    {
        private SerializedProperty requiredKeyId;
        private SerializedProperty linkedKey;
        private SerializedProperty startsLocked;
        private SerializedProperty pushToOpen;
        private SerializedProperty openAngle;
        private SerializedProperty openSpeed;
        private SerializedProperty hingeEdge;
        private SerializedProperty hingePivotOverride;
        private SerializedProperty buildRuntimePivot;
        private SerializedProperty playerTag;
        private SerializedProperty minimumPushStrength;
        private SerializedProperty autoClose;
        private SerializedProperty closeSpeed;
        private SerializedProperty useTriggerDetection;
        private SerializedProperty useCollisionDetection;
        private SerializedProperty pushLayers;
        private SerializedProperty preventOpeningIntoObstacles;
        private SerializedProperty obstacleLayers;
        private SerializedProperty obstaclePadding;
        private SerializedProperty obstacleInset;
        private SerializedProperty ignoreCameraBlocker;
        private SerializedProperty ignoreFloorWhenOpening;
        private SerializedProperty ignoreWallsWhenOpening;
        private SerializedProperty ignoreCorridorsWhenOpening;
        private SerializedProperty floorTagName;
        private SerializedProperty ignoredObstacleNames;
        private SerializedProperty autoAddColliderIfMissing;
        private SerializedProperty generatedColliderIsTrigger;
        private SerializedProperty enableLogs;

        private void OnEnable()
        {
            requiredKeyId = serializedObject.FindProperty("requiredKeyId");
            linkedKey = serializedObject.FindProperty("linkedKey");
            startsLocked = serializedObject.FindProperty("startsLocked");
            pushToOpen = serializedObject.FindProperty("pushToOpen");
            openAngle = serializedObject.FindProperty("openAngle");
            openSpeed = serializedObject.FindProperty("openSpeed");
            hingeEdge = serializedObject.FindProperty("hingeEdge");
            hingePivotOverride = serializedObject.FindProperty("hingePivotOverride");
            buildRuntimePivot = serializedObject.FindProperty("buildRuntimePivot");
            playerTag = serializedObject.FindProperty("playerTag");
            minimumPushStrength = serializedObject.FindProperty("minimumPushStrength");
            autoClose = serializedObject.FindProperty("autoClose");
            closeSpeed = serializedObject.FindProperty("closeSpeed");
            useTriggerDetection = serializedObject.FindProperty("useTriggerDetection");
            useCollisionDetection = serializedObject.FindProperty("useCollisionDetection");
            pushLayers = serializedObject.FindProperty("pushLayers");
            preventOpeningIntoObstacles = serializedObject.FindProperty("preventOpeningIntoObstacles");
            obstacleLayers = serializedObject.FindProperty("obstacleLayers");
            obstaclePadding = serializedObject.FindProperty("obstaclePadding");
            obstacleInset = serializedObject.FindProperty("obstacleInset");
            ignoreCameraBlocker = serializedObject.FindProperty("ignoreCameraBlocker");
            ignoreFloorWhenOpening = serializedObject.FindProperty("ignoreFloorWhenOpening");
            ignoreWallsWhenOpening = serializedObject.FindProperty("ignoreWallsWhenOpening");
            ignoreCorridorsWhenOpening = serializedObject.FindProperty("ignoreCorridorsWhenOpening");
            floorTagName = serializedObject.FindProperty("floorTagName");
            ignoredObstacleNames = serializedObject.FindProperty("ignoredObstacleNames");
            autoAddColliderIfMissing = serializedObject.FindProperty("autoAddColliderIfMissing");
            generatedColliderIsTrigger = serializedObject.FindProperty("generatedColliderIsTrigger");
            enableLogs = serializedObject.FindProperty("enableLogs");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("ACF Door Data", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (targets.Length > 1)
                EditorGUILayout.HelpBox($"Editing {targets.Length} doors simultaneously.", MessageType.Info);

            EditorGUILayout.LabelField("Unlock Setup", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(requiredKeyId);
            EditorGUILayout.PropertyField(linkedKey);
            EditorGUILayout.PropertyField(startsLocked);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Door Behavior", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(pushToOpen);
            EditorGUILayout.PropertyField(openAngle);
            EditorGUILayout.PropertyField(openSpeed);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hinge", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(hingeEdge);
            EditorGUILayout.PropertyField(hingePivotOverride);
            EditorGUILayout.PropertyField(buildRuntimePivot);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Push Detection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(playerTag);
            EditorGUILayout.PropertyField(minimumPushStrength);
            EditorGUILayout.PropertyField(autoClose);
            EditorGUILayout.PropertyField(closeSpeed);
            EditorGUILayout.PropertyField(useTriggerDetection);
            EditorGUILayout.PropertyField(useCollisionDetection);
            EditorGUILayout.PropertyField(pushLayers);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Obstacle Check", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(preventOpeningIntoObstacles);
            if (preventOpeningIntoObstacles.boolValue)
            {
                EditorGUILayout.PropertyField(obstacleLayers);
                EditorGUILayout.PropertyField(obstaclePadding);
                EditorGUILayout.PropertyField(obstacleInset);
                EditorGUILayout.PropertyField(ignoreCameraBlocker);
                EditorGUILayout.PropertyField(ignoreFloorWhenOpening);
                EditorGUILayout.PropertyField(ignoreWallsWhenOpening);
                EditorGUILayout.PropertyField(ignoreCorridorsWhenOpening);
                EditorGUILayout.PropertyField(floorTagName);
                EditorGUILayout.PropertyField(ignoredObstacleNames, true);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Collider Setup", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(autoAddColliderIfMissing);
            EditorGUILayout.PropertyField(generatedColliderIsTrigger);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableLogs);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Force Rebuild Root Collider"))
            {
                foreach (Object obj in targets)
                {
                    ACFDoorData door = (ACFDoorData)obj;
                    Undo.RegisterFullObjectHierarchyUndo(door.gameObject, "Force Rebuild Door Collider");

                    Collider rootCollider = door.GetComponent<Collider>();
                    if (rootCollider != null)
                        DestroyImmediate(rootCollider);

                    BoxCollider newCollider = door.gameObject.AddComponent<BoxCollider>();
                    Bounds bounds = CalculateLocalBounds(door.transform);
                    newCollider.center = bounds.center;
                    newCollider.size = bounds.size;
                    newCollider.isTrigger = door.generatedColliderIsTrigger;

                    EditorUtility.SetDirty(door);
                    EditorUtility.SetDirty(newCollider);
                }

                EditorUtility.DisplayDialog("Door Colliders Rebuilt", "Root colliders were rebuilt for the selected doors.", "OK");
            }
        }

        private static Bounds CalculateLocalBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Bounds combined = default;

            for (int i = 0; i < renderers.Length; i++)
            {
                Bounds worldBounds = renderers[i].bounds;
                Vector3 extents = worldBounds.extents;
                Vector3 center = worldBounds.center;
                Vector3[] corners =
                {
                    center + new Vector3(-extents.x, -extents.y, -extents.z),
                    center + new Vector3(-extents.x, -extents.y, extents.z),
                    center + new Vector3(-extents.x, extents.y, -extents.z),
                    center + new Vector3(-extents.x, extents.y, extents.z),
                    center + new Vector3(extents.x, -extents.y, -extents.z),
                    center + new Vector3(extents.x, -extents.y, extents.z),
                    center + new Vector3(extents.x, extents.y, -extents.z),
                    center + new Vector3(extents.x, extents.y, extents.z)
                };

                for (int c = 0; c < corners.Length; c++)
                {
                    Vector3 localPoint = root.InverseTransformPoint(corners[c]);
                    if (!hasBounds)
                    {
                        combined = new Bounds(localPoint, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        combined.Encapsulate(localPoint);
                    }
                }
            }

            return hasBounds ? combined : new Bounds(Vector3.zero, Vector3.one);
        }
    }
}
