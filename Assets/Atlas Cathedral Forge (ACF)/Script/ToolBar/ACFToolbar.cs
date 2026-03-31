using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ACFSystem
{
    public class ACFToolbar
    {
        [MenuItem("Tools/Atlas-Cathedral-Forge/Quick Scan %#s")]
        public static void QuickScan()
        {
            ACFWindow window = EditorWindow.GetWindow<ACFWindow>();
            window.Show();
            window.SendEvent(EditorGUIUtility.CommandEvent("ScanScene"));
        }

        [MenuItem("Tools/Atlas-Cathedral-Forge/Deep Diagnose %#d")]
        public static void DeepDiagnose()
        {
            ACFWindow window = EditorWindow.GetWindow<ACFWindow>();
            window.Show();
            window.SendEvent(EditorGUIUtility.CommandEvent("DeepDiagnose"));
        }

        [MenuItem("Tools/Atlas-Cathedral-Forge/Edit Mode %#e")]
        public static void EnterEditMode()
        {
            ACFWindow window = EditorWindow.GetWindow<ACFWindow>();
            window.Show();
            window.SendEvent(EditorGUIUtility.CommandEvent("EnterEditMode"));
        }
    }

    [CustomEditor(typeof(Transform))]
    public class ACFFocusEditor : Editor
    {
        private static GameObject lastSelected;

        void OnEnable()
        {
            if (Selection.activeGameObject != null)
            {
                lastSelected = Selection.activeGameObject;
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);

            if (GUILayout.Button("Focus on Pivot (ACF)"))
            {
                if (Selection.activeGameObject != null)
                {
                    SceneView.lastActiveSceneView.FrameSelected();
                    Tools.pivotMode = PivotMode.Pivot;
                }
            }

            if (GUILayout.Button("Edit Per Object Transform"))
            {
                ACFWindow window = EditorWindow.GetWindow<ACFWindow>();
                window.Show();
                EditorGUIUtility.PingObject(Selection.activeGameObject);
            }
        }
    }
}