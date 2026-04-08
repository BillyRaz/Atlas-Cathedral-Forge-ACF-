using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ACFSystem
{
    public class ACFWindow : EditorWindow
    {
        private const string CorridorFloorCustomCategory = "CorridorFloor";
        private const string MaskFloorCustomCategory = "MaskFloor";

        private enum Tab { Scan, Blockout, Floor, Walls, Edit, Final, Diagnose }
        private enum SelectionMode { All, ByCategory, Manual }

        private struct FloorLayoutInfo
        {
            public GameObject source;
            public Bounds bounds;
            public bool isCorridor;
        }

        private readonly Dictionary<string, List<GameObject>> categorizedObjects = new Dictionary<string, List<GameObject>>();
        private readonly bool[] categorySelections = new bool[ACFCategoryUtility.AllCategories.Length];
        private readonly Dictionary<GameObject, GameObject> replacementMap = new Dictionary<GameObject, GameObject>();
        private readonly Dictionary<Renderer, Material[]> originalPreviewMaterials = new Dictionary<Renderer, Material[]>();

        private Tab currentTab = Tab.Scan;
        private SelectionMode currentSelectionMode = SelectionMode.ByCategory;
        private List<GameObject> selectedObjects = new List<GameObject>();
        private Vector3 editPosition;
        private Vector3 editRotation;
        private Vector3 editScale;
        private readonly Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
        private readonly Dictionary<GameObject, Vector3> originalRotations = new Dictionary<GameObject, Vector3>();
        private readonly Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();
        private bool realTimeUpdate = true;
        private int transformMode;
        private Transform selectedPivot;
        private int selectedIndex = -1;
        private GameObject selectedPrefab;
        private bool scanCompleted;
        private string diagnoseOutput = string.Empty;
        private Vector2 scanScrollPos;
        private Vector2 blockoutScrollPos;
        private Vector2 floorScrollPos;
        private Vector2 wallsScrollPos;
        private Vector2 editScrollPos;
        private Vector2 finalScrollPos;
        private Vector2 diagnoseScrollPos;
        private Vector2 diagnoseTextScrollPos;
        private Vector2 objectListScrollPos;
        private bool deepNameAnalysis = true;
        private bool suggestCategorization = true;
        private readonly bool[] categoryFoldouts = new bool[ACFCategoryUtility.AllCategories.Length];
        private bool showScanResults = true;
        private bool showDetailedObjects;
        private int blockoutCategoryIndex;
        private GameObject selectedBlockoutPrefab;
        private Material scanPreviewMaterial;
        private float sourcePreviewOpacity = 0.5f;
        private float generatedWallHeight = 3f;
        private float generatedWallThickness = 0.25f;
        private float roofThickness = 0.2f;
        private float roofOverhang = 0f;
        private bool generateMaskFloorInsteadOfRoof;
        private bool replacePreviousGeneratedRoofObjects = true;
        private float doorOpeningWidth = 1.5f;
        private float doorOpeningHeight = 2.2f;
        private bool spawnDoorPlaceholders = true;
        private bool generateOnlyExteriorWalls = true;
        private bool generateSharedEdgeDoorways = true;
        private float adjacentFloorTolerance = 0.05f;
        private readonly int[] wallDoorCounts = new int[4];
        private bool generatedWallsHidden;
        private GameObject mainFloorObject;
        private GameObject floorSnapTarget;
        private int targetFloorSnapSide;
        private float floorSnapGap;
        private static readonly string[] CardinalSides = { "North", "South", "East", "West" };

        [MenuItem("Tools/Atlas-Cathedral-Forge/ACF Window")]
        public static void ShowWindow()
        {
            ACFWindow window = GetWindow<ACFWindow>("Atlas-Cathedral-Forge");
            window.minSize = new Vector2(500, 700);
            window.Show();
        }

        private void OnEnable()
        {
            for (int i = 0; i < categoryFoldouts.Length; i++)
            {
                categoryFoldouts[i] = false;
            }
        }

        private void OnDisable()
        {
            RestorePreviewMaterials();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Atlas-Cathedral-Forge (ACF)", EditorStyles.boldLabel);
            GUILayout.Space(10);

            currentTab = (Tab)GUILayout.Toolbar((int)currentTab, new[] { "Scan", "Blockout", "Floor", "Walls", "Edit", "Final", "Diagnose" });
            GUILayout.Space(20);

            switch (currentTab)
            {
                case Tab.Scan:
                    scanScrollPos = EditorGUILayout.BeginScrollView(scanScrollPos);
                    DrawScanTab();
                    EditorGUILayout.EndScrollView();
                    break;
                case Tab.Blockout:
                    blockoutScrollPos = EditorGUILayout.BeginScrollView(blockoutScrollPos);
                    DrawBlockoutTab();
                    EditorGUILayout.EndScrollView();
                    break;
                case Tab.Floor:
                    floorScrollPos = EditorGUILayout.BeginScrollView(floorScrollPos);
                    DrawFloorTab();
                    EditorGUILayout.EndScrollView();
                    break;
                case Tab.Walls:
                    wallsScrollPos = EditorGUILayout.BeginScrollView(wallsScrollPos);
                    DrawWallsTab();
                    EditorGUILayout.EndScrollView();
                    break;
                case Tab.Edit:
                    editScrollPos = EditorGUILayout.BeginScrollView(editScrollPos);
                    DrawEditTab();
                    EditorGUILayout.EndScrollView();
                    break;
                case Tab.Final:
                    finalScrollPos = EditorGUILayout.BeginScrollView(finalScrollPos);
                    DrawFinalTab();
                    EditorGUILayout.EndScrollView();
                    break;
                case Tab.Diagnose:
                    diagnoseScrollPos = EditorGUILayout.BeginScrollView(diagnoseScrollPos);
                    DrawDiagnoseTab();
                    EditorGUILayout.EndScrollView();
                    break;
            }
        }

        private void DrawScanTab()
        {
            EditorGUILayout.LabelField("Scene Scan & Categorization", EditorStyles.boldLabel);
            GUILayout.Space(10);
            DrawGeneratedObjectQuickTools();
            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan Scene", GUILayout.Height(40)))
            {
                ScanScene();
            }
            if (GUILayout.Button("Clear Scan", GUILayout.Height(40)))
            {
                ClearScan();
            }
            if (GUILayout.Button("Auto-Categorize by Name", GUILayout.Height(40)))
            {
                AutoCategorizeByName();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);
            DrawQuickAssignButtons();

            if (!scanCompleted)
            {
                EditorGUILayout.HelpBox("Click 'Scan Scene' to analyze and categorize all objects in the scene.", MessageType.Info);
                return;
            }

            GUILayout.Space(20);
            showScanResults = EditorGUILayout.Foldout(showScanResults, "Scan Results", true);
            if (showScanResults)
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Summary Statistics:", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                DrawCategoryResult(ACFCategoryUtility.Floor);
                DrawCategoryResult(ACFCategoryUtility.Wall);
                DrawCategoryResult(ACFCategoryUtility.Roof);
                DrawCategoryResult(ACFCategoryUtility.Prop);
                DrawCategoryResult(ACFCategoryUtility.MovableProp);
                DrawCategoryResult(ACFCategoryUtility.Door);
                DrawCategoryResult(ACFCategoryUtility.Key);
                DrawCategoryResult(ACFCategoryUtility.Landmark);
                DrawCategoryResult(ACFCategoryUtility.Ignore);
                EditorGUILayout.EndVertical();

                GUILayout.Space(10);
                showDetailedObjects = EditorGUILayout.Foldout(showDetailedObjects, "Detailed Object View", true);
                if (showDetailedObjects)
                {
                    EditorGUILayout.HelpBox("Click the dot to select and frame an object in the scene.", MessageType.Info);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Expand All", GUILayout.Width(100)))
                    {
                        for (int i = 0; i < categoryFoldouts.Length; i++)
                        {
                            categoryFoldouts[i] = true;
                        }
                    }
                    if (GUILayout.Button("Collapse All", GUILayout.Width(100)))
                    {
                        for (int i = 0; i < categoryFoldouts.Length; i++)
                        {
                            categoryFoldouts[i] = false;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(10);
                    objectListScrollPos = EditorGUILayout.BeginScrollView(objectListScrollPos, GUILayout.Height(300));
                    for (int i = 0; i < ACFCategoryUtility.AllCategories.Length; i++)
                    {
                        DrawCategoryObjectList(ACFCategoryUtility.AllCategories[i], i);
                    }
                    EditorGUILayout.EndScrollView();
                }
            }

            GUILayout.Space(20);
            EditorGUILayout.LabelField("Block Out Prefabs", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Click below to create blockout prefabs from scanned geometry.", MessageType.Info);

            if (GUILayout.Button("Create Blockout Prefabs"))
            {
                CreateBlockoutPrefabs();
            }
        }

        private void DrawEditTab()
        {
            if (!scanCompleted)
            {
                EditorGUILayout.HelpBox("Please scan the scene first in the Scan tab.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Real-Time Individual Pivot Editing", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Live controls affect all selected objects at once, but each object uses its own pivot and anchor.", MessageType.Info);
            GUILayout.Space(10);
            DrawGeneratedObjectQuickTools();
            GUILayout.Space(10);

            currentSelectionMode = (SelectionMode)EditorGUILayout.EnumPopup("Selection Mode:", currentSelectionMode);
            GUILayout.Space(10);

            switch (currentSelectionMode)
            {
                case SelectionMode.All:
                    if (GUILayout.Button("Select All Objects"))
                    {
                        SelectAllObjects();
                    }
                    break;
                case SelectionMode.ByCategory:
                    DrawCategorySelection();
                    break;
                case SelectionMode.Manual:
                    EditorGUILayout.HelpBox("Select objects manually in the scene hierarchy.", MessageType.Info);
                    if (GUILayout.Button("Refresh Selected Objects"))
                    {
                        selectedObjects = Selection.gameObjects.ToList();
                        UpdatePivotSelection();
                        StoreOriginalTransforms();
                    }
                    break;
            }

            GUILayout.Space(20);

            DrawLiveSelectionEditor("No objects selected. Use the selection tools above to select objects for editing.");
        }

        private void DrawBlockoutTab()
        {
            if (!scanCompleted)
            {
                EditorGUILayout.HelpBox("Please scan the scene first in the Scan tab.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Blockout Prefabs", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Create blockout prefabs, place them in the scene, then select and edit them before moving to Final.", MessageType.Info);
            GUILayout.Space(10);
            DrawGeneratedObjectQuickTools();
            GUILayout.Space(10);

            if (GUILayout.Button("Create / Refresh Blockout Prefabs", GUILayout.Height(36)))
            {
                CreateBlockoutPrefabs();
                selectedBlockoutPrefab = LoadFirstBlockoutPrefabForCategory(ACFCategoryUtility.AllCategories[blockoutCategoryIndex]);
            }

            GUILayout.Space(10);
            blockoutCategoryIndex = EditorGUILayout.Popup("Category", blockoutCategoryIndex, ACFCategoryUtility.AllCategories);
            string blockoutCategory = ACFCategoryUtility.AllCategories[blockoutCategoryIndex];
            selectedBlockoutPrefab = LoadFirstBlockoutPrefabForCategory(blockoutCategory);
            EditorGUILayout.ObjectField("Prefab", selectedBlockoutPrefab, typeof(GameObject), false);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Blockout Prefab"))
            {
                SpawnBlockoutPrefab(blockoutCategory, selectedBlockoutPrefab);
            }
            if (GUILayout.Button("Select Scene Blockouts"))
            {
                SelectSceneBlockouts(blockoutCategory);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Layout Helpers", EditorStyles.boldLabel);
            sourcePreviewOpacity = EditorGUILayout.Slider("Scene Opacity", sourcePreviewOpacity, 0.1f, 1f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Link Key To Doors"))
            {
                LinkSelectedKeyToDoors();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Refresh Scene Fade"))
            {
                ApplyScanPreviewMaterials();
            }

            GUILayout.Space(15);
            DrawLiveSelectionEditor("No blockout objects selected. Spawn or select scene blockouts from a category first.");
        }

        private void DrawWallsTab()
        {
            if (!scanCompleted)
            {
                EditorGUILayout.HelpBox("Please scan the scene first in the Scan tab.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Auto Walls", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Select one or more floor objects, then configure doors per side. Connected floors share edges, so exterior walls are generated around the layout instead of across corridor seams.", MessageType.Info);
            GUILayout.Space(10);
            DrawGeneratedObjectQuickTools();
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Wall And Roof Setup", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            generatedWallHeight = EditorGUILayout.Slider("Wall Height", generatedWallHeight, 1f, 8f);
            generatedWallThickness = EditorGUILayout.Slider("Wall Thickness", generatedWallThickness, 0.1f, 1f);
            roofThickness = EditorGUILayout.Slider("Roof Thickness", roofThickness, 0.05f, 1f);
            roofOverhang = EditorGUILayout.Slider("Roof Overhang", roofOverhang, 0f, 1f);
            EditorGUI.BeginChangeCheck();
            generateMaskFloorInsteadOfRoof = EditorGUILayout.Toggle("Create Mask Floor", generateMaskFloorInsteadOfRoof);
            if (EditorGUI.EndChangeCheck())
            {
                ConvertGeneratedRoofObjects(generateMaskFloorInsteadOfRoof);
            }
            replacePreviousGeneratedRoofObjects = EditorGUILayout.Toggle("Replace Previous Roof Gen", replacePreviousGeneratedRoofObjects);
            doorOpeningWidth = EditorGUILayout.Slider("Door Width", doorOpeningWidth, 0.8f, 4f);
            doorOpeningHeight = EditorGUILayout.Slider("Door Height", doorOpeningHeight, 1.8f, 4f);
            spawnDoorPlaceholders = EditorGUILayout.Toggle("Spawn Door Placeholders", spawnDoorPlaceholders);
            generateOnlyExteriorWalls = EditorGUILayout.Toggle("Only Exterior Edges", generateOnlyExteriorWalls);
            generateSharedEdgeDoorways = EditorGUILayout.Toggle("Door At Floor Connections", generateSharedEdgeDoorways);
            adjacentFloorTolerance = EditorGUILayout.Slider("Shared Edge Tolerance", adjacentFloorTolerance, 0.01f, 0.3f);
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Doors Per Side", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            wallDoorCounts[0] = EditorGUILayout.IntSlider(CardinalSides[0], wallDoorCounts[0], 0, 4);
            wallDoorCounts[1] = EditorGUILayout.IntSlider(CardinalSides[1], wallDoorCounts[1], 0, 4);
            wallDoorCounts[2] = EditorGUILayout.IntSlider(CardinalSides[2], wallDoorCounts[2], 0, 4);
            wallDoorCounts[3] = EditorGUILayout.IntSlider(CardinalSides[3], wallDoorCounts[3], 0, 4);
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Generation Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            if (GUILayout.Button("Use Floor Category Selection"))
            {
                selectedObjects = GetObjectsForCategory(ACFCategoryUtility.Floor).Where(obj => obj != null).ToList();
                Selection.objects = selectedObjects.ToArray();
                UpdatePivotSelection();
                StoreOriginalTransforms();
            }
            if (GUILayout.Button("Generate Walls"))
            {
                AutoGenerateWallsForSelection();
            }
            if (GUILayout.Button("Generate Roofs"))
            {
                AutoGenerateRoofsForSelection();
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(8);
            DrawLiveSelectionEditor("No floor or wall-related objects selected yet. Select floor objects or use the floor category button.");
        }

        private void DrawFloorTab()
        {
            if (!scanCompleted)
            {
                EditorGUILayout.HelpBox("Please scan the scene first in the Scan tab.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Floor Layout", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Snap selected floor or corridor pieces to another floor edge so connected layouts line up cleanly before generating walls.", MessageType.Info);
            GUILayout.Space(10);
            DrawGeneratedObjectQuickTools();
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Selection Setup", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            if (GUILayout.Button("Use Floor Category Selection"))
            {
                selectedObjects = GetObjectsForCategory(ACFCategoryUtility.Floor).Where(obj => obj != null).ToList();
                Selection.objects = selectedObjects.ToArray();
                if (selectedObjects.Count > 0)
                {
                    mainFloorObject = selectedObjects[0];
                }
                UpdatePivotSelection();
                StoreOriginalTransforms();
            }

            if (GUILayout.Button("Use Current Selection"))
            {
                selectedObjects = Selection.gameObjects.Where(obj => obj != null).ToList();
                if (selectedObjects.Count > 0)
                {
                    mainFloorObject = selectedObjects[0];
                }
                UpdatePivotSelection();
                StoreOriginalTransforms();
            }

            if (GUILayout.Button("Use Roof Category Selection"))
            {
                selectedObjects = GetObjectsForCategory(ACFCategoryUtility.Roof).Where(obj => obj != null).ToList();
                Selection.objects = selectedObjects.ToArray();
                if (selectedObjects.Count > 0)
                {
                    mainFloorObject = selectedObjects[0];
                }
                UpdatePivotSelection();
                StoreOriginalTransforms();
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Snap Setup", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            mainFloorObject = (GameObject)EditorGUILayout.ObjectField("Main Floor", mainFloorObject, typeof(GameObject), true);
            floorSnapTarget = (GameObject)EditorGUILayout.ObjectField("Target Floor", floorSnapTarget, typeof(GameObject), true);
            targetFloorSnapSide = EditorGUILayout.Popup("Attach To Target Edge", targetFloorSnapSide, CardinalSides);
            floorSnapGap = EditorGUILayout.FloatField("Gap", floorSnapGap);
            EditorGUILayout.EndVertical();

            EditorGUILayout.LabelField("Floor Type", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            if (GUILayout.Button("Mark Selected As Corridor"))
            {
                SetSelectedFloorSubtype(true);
            }

            if (GUILayout.Button("Mark Selected As Room"))
            {
                SetSelectedFloorSubtype(false);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.LabelField("Mask Floor", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            if (GUILayout.Button("Create Mask Floor From Selected Roofs"))
            {
                CreateMaskFloorFromSelectedRoofs();
            }

            if (GUILayout.Button("Mark Selected As Mask Floor"))
            {
                SetSelectedMaskFloorSubtype();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            if (GUILayout.Button("Use Active As Main"))
            {
                mainFloorObject = Selection.activeGameObject;
            }

            if (GUILayout.Button("Use Active As Target"))
            {
                floorSnapTarget = Selection.activeGameObject;
            }

            if (GUILayout.Button("Snap Main Floor To Target"))
            {
                SnapSelectedFloorsToTarget();
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);
            DrawLiveSelectionEditor("Select floor or corridor pieces here, then snap them against a target floor edge.");
        }

        private void DrawFinalTab()
        {
            if (!scanCompleted)
            {
                EditorGUILayout.HelpBox("Please scan the scene first in the Scan tab.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Replace with Final Props", EditorStyles.boldLabel);
            GUILayout.Space(10);
            DrawGeneratedObjectQuickTools();
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("Replace simple shapes with final prefabs while preserving transforms.", MessageType.Info);
            GUILayout.Space(10);

            selectedPrefab = (GameObject)EditorGUILayout.ObjectField("Replacement Prefab:", selectedPrefab, typeof(GameObject), false);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Select objects to replace:", EditorStyles.boldLabel);
            DrawCategorySelection();

            GUILayout.Space(20);
            if (GUILayout.Button("Replace Selected Objects", GUILayout.Height(40)))
            {
                ReplaceSelectedObjects();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Add Colliders to Replaced Objects"))
            {
                AddCollidersToReplaced();
            }
        }

        private void DrawLiveSelectionEditor(string emptyMessage)
        {
            if (selectedObjects.Count <= 0)
            {
                EditorGUILayout.HelpBox(emptyMessage, MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Selected Objects: {selectedObjects.Count}", EditorStyles.boldLabel);
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Objects:", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginVertical("box");
            objectListScrollPos = EditorGUILayout.BeginScrollView(objectListScrollPos, GUILayout.MaxHeight(200));
            for (int i = 0; i < selectedObjects.Count; i++)
            {
                if (selectedObjects[i] == null)
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = selectedIndex == i ? Color.cyan : Color.white;
                if (GUILayout.Button($"{i + 1}", GUILayout.Width(30)))
                {
                    selectedIndex = i;
                    selectedPivot = selectedObjects[i].transform;
                    UpdateTransformFields();
                    Selection.activeGameObject = selectedObjects[i];
                    if (SceneView.lastActiveSceneView != null)
                    {
                        SceneView.lastActiveSceneView.FrameSelected();
                    }
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.ObjectField(selectedObjects[i], typeof(GameObject), true);
                if (GUILayout.Button("M", GUILayout.Width(25)))
                {
                    Selection.activeGameObject = selectedObjects[i];
                    Tools.current = Tool.Move;
                }
                if (GUILayout.Button("R", GUILayout.Width(25)))
                {
                    Selection.activeGameObject = selectedObjects[i];
                    Tools.current = Tool.Rotate;
                }
                if (GUILayout.Button("S", GUILayout.Width(25)))
                {
                    Selection.activeGameObject = selectedObjects[i];
                    Tools.current = Tool.Scale;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);
            EditorGUILayout.LabelField("Live Transform Controls", EditorStyles.boldLabel);
            transformMode = GUILayout.Toolbar(transformMode, new[] { "Position", "Rotation", "Scale" });
            realTimeUpdate = EditorGUILayout.Toggle("Real-Time Update", realTimeUpdate);
            GUILayout.Space(8);

            EditorGUI.BeginChangeCheck();

            switch (transformMode)
            {
                case 0:
                    Tools.current = Tool.Move;
                    Tools.pivotMode = PivotMode.Pivot;
                    editPosition = EditorGUILayout.Vector3Field("Offset", editPosition);
                    editPosition.x = EditorGUILayout.Slider("X", editPosition.x, -10f, 10f);
                    editPosition.y = EditorGUILayout.Slider("Y", editPosition.y, -10f, 10f);
                    editPosition.z = EditorGUILayout.Slider("Z", editPosition.z, -10f, 10f);
                    break;
                case 1:
                    Tools.current = Tool.Rotate;
                    Tools.pivotMode = PivotMode.Pivot;
                    editRotation = EditorGUILayout.Vector3Field("Offset", editRotation);
                    editRotation.x = EditorGUILayout.Slider("X", editRotation.x, -360f, 360f);
                    editRotation.y = EditorGUILayout.Slider("Y", editRotation.y, -360f, 360f);
                    editRotation.z = EditorGUILayout.Slider("Z", editRotation.z, -360f, 360f);
                    break;
                case 2:
                    Tools.current = Tool.Scale;
                    Tools.pivotMode = PivotMode.Pivot;
                    editScale = EditorGUILayout.Vector3Field("Multiplier", editScale);
                    editScale.x = EditorGUILayout.Slider("X", editScale.x, 0.1f, 5f);
                    editScale.y = EditorGUILayout.Slider("Y", editScale.y, 0.1f, 5f);
                    editScale.z = EditorGUILayout.Slider("Z", editScale.z, 0.1f, 5f);
                    break;
            }

            if (EditorGUI.EndChangeCheck() && realTimeUpdate)
            {
                ApplyLiveTransforms();
            }

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Store Current as Baseline"))
            {
                StoreOriginalTransforms();
            }
            if (GUILayout.Button("Reset Controls"))
            {
                ResetLiveControls();
                ApplyLiveTransforms();
            }
            if (GUILayout.Button("Reset Objects"))
            {
                ResetSelectedTransforms();
                StoreOriginalTransforms();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDiagnoseTab()
        {
            EditorGUILayout.LabelField("Deep Scene Diagnosis", EditorStyles.boldLabel);
            GUILayout.Space(10);
            DrawGeneratedObjectQuickTools();
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Diagnosis Options:", EditorStyles.boldLabel);
            deepNameAnalysis = EditorGUILayout.Toggle("Deep Name Analysis", deepNameAnalysis);
            suggestCategorization = EditorGUILayout.Toggle("Suggest Categorization", suggestCategorization);
            GUILayout.Space(10);

            if (GUILayout.Button("Run Deep Scan", GUILayout.Height(40)))
            {
                RunDeepScan();
            }

            GUILayout.Space(20);
            EditorGUILayout.LabelField("Diagnosis Results:", EditorStyles.boldLabel);
            diagnoseTextScrollPos = EditorGUILayout.BeginScrollView(diagnoseTextScrollPos, GUILayout.Height(400));
            EditorGUILayout.TextArea(diagnoseOutput, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);
            if (GUILayout.Button("Copy to Clipboard"))
            {
                EditorGUIUtility.systemCopyBuffer = diagnoseOutput;
                Debug.Log("Diagnosis output copied to clipboard");
            }

            if (GUILayout.Button("Export to File"))
            {
                string path = EditorUtility.SaveFilePanel("Save Diagnosis", "", "ACF_Diagnosis.txt", "txt");
                if (!string.IsNullOrEmpty(path))
                {
                    System.IO.File.WriteAllText(path, diagnoseOutput);
                    Debug.Log($"Diagnosis saved to: {path}");
                }
            }
        }

        private void DrawCategorySelection()
        {
            EditorGUILayout.LabelField("Select Categories:", EditorStyles.boldLabel);

            for (int i = 0; i < ACFCategoryUtility.AllCategories.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                categorySelections[i] = EditorGUILayout.Toggle(GetDisplayName(ACFCategoryUtility.AllCategories[i]), categorySelections[i]);
                int count = GetObjectsForCategory(ACFCategoryUtility.AllCategories[i]).Length;
                if (count > 0)
                {
                    EditorGUILayout.LabelField($"({count})", GUILayout.Width(50));
                    if (GUILayout.Button("Select", GUILayout.Width(50)))
                    {
                        categorySelections[i] = true;
                        SelectFromCategories();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select from Categories"))
            {
                SelectFromCategories();
            }
            if (GUILayout.Button("Select All Categories"))
            {
                for (int i = 0; i < categorySelections.Length; i++)
                {
                    categorySelections[i] = true;
                }
                SelectFromCategories();
            }
            if (GUILayout.Button("Clear Selections"))
            {
                for (int i = 0; i < categorySelections.Length; i++)
                {
                    categorySelections[i] = false;
                }
                selectedObjects.Clear();
                selectedPivot = null;
                selectedIndex = -1;
                Selection.objects = new Object[0];
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ScanScene()
        {
            GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            InitializeCategoryDictionary();

            foreach (GameObject rootObject in rootObjects)
            {
                GameObject[] allChildren = rootObject.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject).ToArray();
                foreach (GameObject gameObject in allChildren)
                {
                    if (ShouldSkipFromScan(gameObject))
                    {
                        continue;
                    }

                    if (ACFCategoryUtility.TryInferCategory(gameObject, out string category))
                    {
                        categorizedObjects[category].Add(gameObject);
                    }
                }
            }

            OrganizeSceneHierarchy(rootObjects);
            ApplyScanPreviewMaterials();
            scanCompleted = true;
            Debug.Log(BuildCategorySummary("ACF Scan Complete:"));

            int totalObjects = rootObjects.Sum(obj => obj.GetComponentsInChildren<Transform>(true).Length);
            EditorUtility.DisplayDialog("Scan Complete", $"Found {totalObjects} objects\n\nCategorized into {ACFCategoryUtility.AllCategories.Length} standard types", "OK");
        }

        private void AutoCategorizeByName()
        {
            int categorizedCount = 0;
            GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (GameObject rootObject in rootObjects)
            {
                GameObject[] allChildren = rootObject.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject).ToArray();
                foreach (GameObject gameObject in allChildren)
                {
                    if (ShouldSkipFromScan(gameObject))
                    {
                        continue;
                    }

                    if (!TrySuggestCategory(gameObject, out string category))
                    {
                        continue;
                    }

                    ACFObjectData existingData = gameObject.GetComponent<ACFObjectData>();
                    string existingCategory = existingData == null
                        ? string.Empty
                        : ACFCategoryUtility.GetCategoryName(existingData.category, existingData.customCategory);

                    if (existingCategory == category)
                    {
                        continue;
                    }

                    AssignCategoryToObject(gameObject, category);
                    categorizedCount++;
                }
            }

            ScanScene();
            EditorUtility.DisplayDialog("Auto-Categorization Complete", $"Categorized {categorizedCount} objects based on their names.", "OK");
        }

        private void CreateBlockoutPrefabs()
        {
            string path = "Assets/ACF_Blockouts";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder("Assets", "ACF_Blockouts");
            }

            int createdCount = 0;
            foreach (string category in ACFCategoryUtility.AllCategories)
            {
                if (category == ACFCategoryUtility.Ignore)
                {
                    continue;
                }

                createdCount += CreateBlockoutForCategory(GetObjectsForCategory(category), category, path);
            }

            AssetDatabase.Refresh();
            Debug.Log($"Created {createdCount} blockout prefabs");
            EditorUtility.DisplayDialog("Blockout Prefabs Created", $"Successfully created {createdCount} blockout prefabs in {path}", "OK");
        }

        private int CreateBlockoutForCategory(GameObject[] objects, string categoryName, string path)
        {
            int count = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == null)
                {
                    continue;
                }

                GameObject blockout = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blockout.name = $"{categoryName}_Blockout_{i}";
                blockout.transform.position = objects[i].transform.position;
                blockout.transform.rotation = objects[i].transform.rotation;
                blockout.transform.localScale = objects[i].transform.lossyScale;

                string prefabPath = $"{path}/{blockout.name}.prefab";
                PrefabUtility.SaveAsPrefabAsset(blockout, prefabPath);
                DestroyImmediate(blockout);
                count++;
            }

            return count;
        }

        private GameObject LoadFirstBlockoutPrefabForCategory(string category)
        {
            string[] guids = AssetDatabase.FindAssets($"{category}_Blockout_ t:Prefab", new[] { "Assets/ACF_Blockouts" });
            if (guids.Length == 0)
            {
                return null;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        }

        private void SpawnBlockoutPrefab(string category, GameObject prefab)
        {
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Missing Prefab", $"No blockout prefab found for {category}. Create blockout prefabs first.", "OK");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                return;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Spawn Blockout Prefab");
            SceneView sceneView = SceneView.lastActiveSceneView;
            instance.transform.position = sceneView != null ? sceneView.pivot : Vector3.zero;

            ACFObjectData data = instance.GetComponent<ACFObjectData>();
            if (data == null)
            {
                data = Undo.AddComponent<ACFObjectData>(instance);
            }

            ApplyCategoryToObjectData(data, category);
            data.isBlockout = true;
            EditorUtility.SetDirty(data);

            selectedObjects = new List<GameObject> { instance };
            Selection.activeGameObject = instance;
            Selection.objects = selectedObjects.ToArray();
            UpdatePivotSelection();
            StoreOriginalTransforms();
            ApplyScanPreviewMaterials();
        }

        private void SelectSceneBlockouts(string category)
        {
            ACFObjectData[] objectDataComponents = Object.FindObjectsByType<ACFObjectData>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            selectedObjects = objectDataComponents
                .Where(data => data != null && data.isBlockout && ACFCategoryUtility.GetCategoryName(data.category, data.customCategory) == category)
                .Select(data => data.gameObject)
                .ToList();

            Selection.objects = selectedObjects.ToArray();
            UpdatePivotSelection();
            StoreOriginalTransforms();
            Debug.Log($"Selected {selectedObjects.Count} blockout object(s) from {GetDisplayName(category)}");
        }

        private void AutoGenerateWallsForSelection()
        {
            IEnumerable<GameObject> sources = selectedObjects.Count > 0
                ? selectedObjects.Where(obj => obj != null)
                : GetObjectsForCategory(ACFCategoryUtility.Floor).Where(obj => obj != null);

            List<GameObject> sourceList = sources.Distinct().ToList();
            List<FloorLayoutInfo> layoutInfos = new List<FloorLayoutInfo>();
            foreach (GameObject source in sourceList)
            {
                Bounds? bounds = GetWorldBounds(source);
                if (bounds.HasValue)
                {
                    layoutInfos.Add(new FloorLayoutInfo
                    {
                        source = source,
                        bounds = bounds.Value,
                        isCorridor = IsCorridorFloor(source)
                    });
                }
            }

            int generatedCount = 0;
            for (int i = 0; i < layoutInfos.Count; i++)
            {
                generatedCount += CreateWallRingFromBounds(layoutInfos[i], layoutInfos);
            }

            ApplyScanPreviewMaterials();
            EditorUtility.DisplayDialog("Walls Generated", $"Generated {generatedCount} wall blockout object(s).", "OK");
        }

        private void AutoGenerateRoofsForSelection()
        {
            IEnumerable<GameObject> sources = selectedObjects.Count > 0
                ? selectedObjects.Where(obj => obj != null)
                : GetObjectsForCategory(ACFCategoryUtility.Floor).Where(obj => obj != null);

            if (replacePreviousGeneratedRoofObjects)
            {
                ClearGeneratedRoofObjects();
            }

            List<GameObject> sourceList = sources.Distinct().ToList();
            int generatedCount = 0;
            foreach (GameObject source in sourceList)
            {
                Bounds? sourceBounds = GetWorldBounds(source);
                if (!sourceBounds.HasValue)
                {
                    continue;
                }

                CreateGeneratedRoof(source.name, sourceBounds.Value, generateMaskFloorInsteadOfRoof);
                generatedCount++;
            }

            ApplyScanPreviewMaterials();
            EditorUtility.DisplayDialog(
                generateMaskFloorInsteadOfRoof ? "Mask Floors Generated" : "Roofs Generated",
                generateMaskFloorInsteadOfRoof
                    ? $"Generated {generatedCount} mask floor blockout object(s)."
                    : $"Generated {generatedCount} roof blockout object(s).",
                "OK");
        }

        private void ClearGeneratedRoofObjects()
        {
            ACFObjectData[] objectDataComponents = Object.FindObjectsByType<ACFObjectData>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (ACFObjectData objectData in objectDataComponents)
            {
                if (objectData == null || !objectData.isBlockout)
                {
                    continue;
                }

                bool isGeneratedRoof = objectData.category == ACFObjectData.ObjectCategory.Roof;
                bool isGeneratedMaskFloor = objectData.category == ACFObjectData.ObjectCategory.Floor &&
                    objectData.customCategory == MaskFloorCustomCategory;

                if (!isGeneratedRoof && !isGeneratedMaskFloor)
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(objectData.gameObject);
            }
        }

        private void ConvertGeneratedRoofObjects(bool toMaskFloor)
        {
            ACFObjectData[] objectDataComponents = Object.FindObjectsByType<ACFObjectData>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (ACFObjectData objectData in objectDataComponents)
            {
                if (objectData == null || !objectData.isBlockout)
                {
                    continue;
                }

                bool isGeneratedRoof = objectData.category == ACFObjectData.ObjectCategory.Roof;
                bool isGeneratedMaskFloor = objectData.category == ACFObjectData.ObjectCategory.Floor &&
                    objectData.customCategory == MaskFloorCustomCategory;

                if (!isGeneratedRoof && !isGeneratedMaskFloor)
                {
                    continue;
                }

                Undo.RecordObject(objectData, "Convert Generated Roof Objects");
                Undo.RecordObject(objectData.gameObject, "Convert Generated Roof Objects");

                if (toMaskFloor)
                {
                    ApplyCategoryToObjectData(objectData, ACFCategoryUtility.Floor);
                    objectData.customCategory = MaskFloorCustomCategory;
                    objectData.gameObject.name = NormalizeGeneratedRoofName(objectData.gameObject.name, true);
                }
                else
                {
                    ApplyCategoryToObjectData(objectData, ACFCategoryUtility.Roof);
                    objectData.customCategory = string.Empty;
                    objectData.gameObject.name = NormalizeGeneratedRoofName(objectData.gameObject.name, false);
                }

                EditorUtility.SetDirty(objectData);
                EditorUtility.SetDirty(objectData.gameObject);
            }
        }

        private string NormalizeGeneratedRoofName(string currentName, bool asMaskFloor)
        {
            if (asMaskFloor)
            {
                if (currentName.StartsWith("Roof_"))
                {
                    return "MaskFloor_" + currentName.Substring("Roof_".Length);
                }

                if (!currentName.StartsWith("MaskFloor_"))
                {
                    return "MaskFloor_" + currentName;
                }

                return currentName;
            }

            if (currentName.StartsWith("MaskFloor_"))
            {
                return "Roof_" + currentName.Substring("MaskFloor_".Length);
            }

            if (!currentName.StartsWith("Roof_"))
            {
                return "Roof_" + currentName;
            }

            return currentName;
        }

        private int CreateWallRingFromBounds(FloorLayoutInfo floorInfo, List<FloorLayoutInfo> layoutInfos)
        {
            int count = 0;
            Bounds bounds = floorInfo.bounds;

            bool northHasAdjacent = false;
            List<Vector2> sharedNorthGaps = GetAdjacentFloorGaps(floorInfo, CardinalSides[0], layoutInfos, out northHasAdjacent);
            if (northHasAdjacent)
            {
                if (sharedNorthGaps.Count > 0)
                {
                    count += CreateWallSideWithGaps(bounds, CardinalSides[0], true, bounds.max.z + generatedWallThickness * 0.5f, sharedNorthGaps, generateSharedEdgeDoorways);
                }
            }
            else
            {
                count += CreateSegmentedWallSide(bounds, CardinalSides[0], wallDoorCounts[0], true, bounds.max.z + generatedWallThickness * 0.5f);
            }

            bool southHasAdjacent = false;
            List<Vector2> sharedSouthGaps = GetAdjacentFloorGaps(floorInfo, CardinalSides[1], layoutInfos, out southHasAdjacent);
            if (southHasAdjacent)
            {
                if (sharedSouthGaps.Count > 0)
                {
                    count += CreateWallSideWithGaps(bounds, CardinalSides[1], true, bounds.min.z - generatedWallThickness * 0.5f, sharedSouthGaps, generateSharedEdgeDoorways);
                }
            }
            else
            {
                count += CreateSegmentedWallSide(bounds, CardinalSides[1], wallDoorCounts[1], true, bounds.min.z - generatedWallThickness * 0.5f);
            }

            bool eastHasAdjacent = false;
            List<Vector2> sharedEastGaps = GetAdjacentFloorGaps(floorInfo, CardinalSides[2], layoutInfos, out eastHasAdjacent);
            if (eastHasAdjacent)
            {
                if (sharedEastGaps.Count > 0)
                {
                    count += CreateWallSideWithGaps(bounds, CardinalSides[2], false, bounds.max.x + generatedWallThickness * 0.5f, sharedEastGaps, generateSharedEdgeDoorways);
                }
            }
            else
            {
                count += CreateSegmentedWallSide(bounds, CardinalSides[2], wallDoorCounts[2], false, bounds.max.x + generatedWallThickness * 0.5f);
            }

            bool westHasAdjacent = false;
            List<Vector2> sharedWestGaps = GetAdjacentFloorGaps(floorInfo, CardinalSides[3], layoutInfos, out westHasAdjacent);
            if (westHasAdjacent)
            {
                if (sharedWestGaps.Count > 0)
                {
                    count += CreateWallSideWithGaps(bounds, CardinalSides[3], false, bounds.min.x - generatedWallThickness * 0.5f, sharedWestGaps, generateSharedEdgeDoorways);
                }
            }
            else
            {
                count += CreateSegmentedWallSide(bounds, CardinalSides[3], wallDoorCounts[3], false, bounds.min.x - generatedWallThickness * 0.5f);
            }
            return count;
        }

        private bool ShouldGenerateSharedEdgeFromThisSide(string sideName)
        {
            return sideName == "North" || sideName == "East";
        }

        private List<Vector2> GetAdjacentFloorGaps(FloorLayoutInfo floorInfo, string sideName, List<FloorLayoutInfo> layoutInfos, out bool hasAdjacent)
        {
            List<Vector2> gaps = new List<Vector2>();
            hasAdjacent = false;
            if (!generateOnlyExteriorWalls)
            {
                return gaps;
            }

            for (int i = 0; i < layoutInfos.Count; i++)
            {
                FloorLayoutInfo otherInfo = layoutInfos[i];
                Bounds bounds = floorInfo.bounds;
                Bounds other = otherInfo.bounds;
                if (ApproximatelySameBounds(bounds, other))
                {
                    continue;
                }

                switch (sideName)
                {
                    case "North":
                        if (Mathf.Abs(other.min.z - bounds.max.z) <= adjacentFloorTolerance &&
                            GetOverlapLength(bounds.min.x, bounds.max.x, other.min.x, other.max.x) > adjacentFloorTolerance)
                        {
                            hasAdjacent = true;
                            if (ShouldCurrentFloorOwnSharedEdge(floorInfo, otherInfo, sideName))
                            {
                                gaps.Add(CreateCenteredConnectionGap(
                                    Mathf.Max(bounds.min.x - generatedWallThickness, other.min.x),
                                    Mathf.Min(bounds.max.x + generatedWallThickness, other.max.x)));
                            }
                        }
                        break;
                    case "South":
                        if (Mathf.Abs(other.max.z - bounds.min.z) <= adjacentFloorTolerance &&
                            GetOverlapLength(bounds.min.x, bounds.max.x, other.min.x, other.max.x) > adjacentFloorTolerance)
                        {
                            hasAdjacent = true;
                            if (ShouldCurrentFloorOwnSharedEdge(floorInfo, otherInfo, sideName))
                            {
                                gaps.Add(CreateCenteredConnectionGap(
                                    Mathf.Max(bounds.min.x - generatedWallThickness, other.min.x),
                                    Mathf.Min(bounds.max.x + generatedWallThickness, other.max.x)));
                            }
                        }
                        break;
                    case "East":
                        if (Mathf.Abs(other.min.x - bounds.max.x) <= adjacentFloorTolerance &&
                            GetOverlapLength(bounds.min.z, bounds.max.z, other.min.z, other.max.z) > adjacentFloorTolerance)
                        {
                            hasAdjacent = true;
                            if (ShouldCurrentFloorOwnSharedEdge(floorInfo, otherInfo, sideName))
                            {
                                gaps.Add(CreateCenteredConnectionGap(
                                    Mathf.Max(bounds.min.z - generatedWallThickness, other.min.z),
                                    Mathf.Min(bounds.max.z + generatedWallThickness, other.max.z)));
                            }
                        }
                        break;
                    case "West":
                        if (Mathf.Abs(other.max.x - bounds.min.x) <= adjacentFloorTolerance &&
                            GetOverlapLength(bounds.min.z, bounds.max.z, other.min.z, other.max.z) > adjacentFloorTolerance)
                        {
                            hasAdjacent = true;
                            if (ShouldCurrentFloorOwnSharedEdge(floorInfo, otherInfo, sideName))
                            {
                                gaps.Add(CreateCenteredConnectionGap(
                                    Mathf.Max(bounds.min.z - generatedWallThickness, other.min.z),
                                    Mathf.Min(bounds.max.z + generatedWallThickness, other.max.z)));
                            }
                        }
                        break;
                }
            }

            return MergeGaps(gaps);
        }

        private bool ShouldCurrentFloorOwnSharedEdge(FloorLayoutInfo floorInfo, FloorLayoutInfo otherInfo, string sideName)
        {
            if (floorInfo.isCorridor != otherInfo.isCorridor)
            {
                return !floorInfo.isCorridor;
            }

            return ShouldGenerateSharedEdgeFromThisSide(sideName);
        }

        private Vector2 CreateCenteredConnectionGap(float overlapStart, float overlapEnd)
        {
            float overlapLength = overlapEnd - overlapStart;
            float gapWidth = Mathf.Min(doorOpeningWidth, overlapLength);
            float center = (overlapStart + overlapEnd) * 0.5f;
            float halfWidth = gapWidth * 0.5f;
            return new Vector2(center - halfWidth, center + halfWidth);
        }

        private List<Vector2> MergeGaps(List<Vector2> gaps)
        {
            List<Vector2> ordered = gaps
                .Where(gap => gap.y - gap.x > adjacentFloorTolerance)
                .OrderBy(gap => gap.x)
                .ToList();

            if (ordered.Count == 0)
            {
                return ordered;
            }

            List<Vector2> merged = new List<Vector2> { ordered[0] };
            for (int i = 1; i < ordered.Count; i++)
            {
                Vector2 current = ordered[i];
                Vector2 previous = merged[merged.Count - 1];
                if (current.x <= previous.y + adjacentFloorTolerance)
                {
                    merged[merged.Count - 1] = new Vector2(previous.x, Mathf.Max(previous.y, current.y));
                }
                else
                {
                    merged.Add(current);
                }
            }

            return merged;
        }

        private float GetOverlapLength(float minA, float maxA, float minB, float maxB)
        {
            return Mathf.Max(0f, Mathf.Min(maxA, maxB) - Mathf.Max(minA, minB));
        }

        private bool ApproximatelySameBounds(Bounds a, Bounds b)
        {
            return Vector3.Distance(a.center, b.center) <= 0.001f && Vector3.Distance(a.size, b.size) <= 0.001f;
        }

        private void CreateGeneratedWall(string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(wall, "Generate Wall Blockout");
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;

            ACFObjectData data = Undo.AddComponent<ACFObjectData>(wall);
            ApplyCategoryToObjectData(data, ACFCategoryUtility.Wall);
            data.isBlockout = true;
            EditorUtility.SetDirty(data);
        }

        private void CreateGeneratedRoof(string sourceName, Bounds bounds, bool createMaskFloor)
        {
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(roof, createMaskFloor ? "Generate Mask Floor Blockout" : "Generate Roof Blockout");
            roof.name = createMaskFloor ? $"MaskFloor_{sourceName}" : $"Roof_{sourceName}";
            roof.transform.position = new Vector3(
                bounds.center.x,
                bounds.max.y + generatedWallHeight + roofThickness * 0.5f,
                bounds.center.z);
            roof.transform.localScale = new Vector3(
                bounds.size.x + generatedWallThickness * 2f + roofOverhang * 2f,
                roofThickness,
                bounds.size.z + generatedWallThickness * 2f + roofOverhang * 2f);

            ACFObjectData data = Undo.AddComponent<ACFObjectData>(roof);
            ApplyCategoryToObjectData(data, createMaskFloor ? ACFCategoryUtility.Floor : ACFCategoryUtility.Roof);
            data.customCategory = createMaskFloor ? MaskFloorCustomCategory : string.Empty;
            data.isBlockout = true;
            EditorUtility.SetDirty(data);
        }

        private int CreateSegmentedWallSide(Bounds bounds, string sideName, int doorCount, bool horizontalSide, float fixedAxis)
        {
            float length = horizontalSide ? bounds.size.x + generatedWallThickness * 2f : bounds.size.z + generatedWallThickness * 2f;
            float start = horizontalSide ? bounds.min.x - generatedWallThickness : bounds.min.z - generatedWallThickness;
            List<Vector2> gaps = BuildDoorGaps(start, length, doorCount);
            return CreateWallSideWithGaps(bounds, sideName, horizontalSide, fixedAxis, gaps, spawnDoorPlaceholders);
        }

        private int CreateWallSideWithGaps(Bounds bounds, string sideName, bool horizontalSide, float fixedAxis, List<Vector2> gaps, bool spawnDoorsForGaps)
        {
            int generated = 0;
            float length = horizontalSide ? bounds.size.x + generatedWallThickness * 2f : bounds.size.z + generatedWallThickness * 2f;
            float start = horizontalSide ? bounds.min.x - generatedWallThickness : bounds.min.z - generatedWallThickness;
            float centerY = bounds.center.y + generatedWallHeight * 0.5f;
            float cursor = start;

            for (int i = 0; i <= gaps.Count; i++)
            {
                float segmentEnd = i < gaps.Count ? gaps[i].x : start + length;
                float segmentLength = segmentEnd - cursor;
                if (segmentLength > 0.05f)
                {
                    Vector3 position;
                    Vector3 scale;
                    if (horizontalSide)
                    {
                        position = new Vector3(cursor + segmentLength * 0.5f, centerY, fixedAxis);
                        scale = new Vector3(segmentLength, generatedWallHeight, generatedWallThickness);
                    }
                    else
                    {
                        position = new Vector3(fixedAxis, centerY, cursor + segmentLength * 0.5f);
                        scale = new Vector3(generatedWallThickness, generatedWallHeight, segmentLength);
                    }

                    CreateGeneratedWall($"Wall_{sideName}_{generated + 1}", position, scale);
                    generated++;
                }

                if (i < gaps.Count)
                {
                    if (spawnDoorsForGaps)
                    {
                        CreateDoorPlaceholder(bounds, sideName, horizontalSide, fixedAxis, gaps[i]);
                    }
                    cursor = gaps[i].y;
                }
            }

            return generated;
        }

        private List<Vector2> BuildDoorGaps(float start, float length, int doorCount)
        {
            List<Vector2> gaps = new List<Vector2>();
            if (doorCount <= 0)
            {
                return gaps;
            }

            float spacing = length / (doorCount + 1);
            for (int i = 0; i < doorCount; i++)
            {
                float center = start + spacing * (i + 1);
                gaps.Add(new Vector2(center - doorOpeningWidth * 0.5f, center + doorOpeningWidth * 0.5f));
            }

            return gaps;
        }

        private void CreateDoorPlaceholder(Bounds bounds, string sideName, bool horizontalSide, float fixedAxis, Vector2 gap)
        {
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(door, "Generate Door Placeholder");
            door.name = $"Door_{sideName}";

            float centerY = bounds.center.y + doorOpeningHeight * 0.5f;
            if (horizontalSide)
            {
                door.transform.position = new Vector3((gap.x + gap.y) * 0.5f, centerY, fixedAxis);
                door.transform.localScale = new Vector3(Mathf.Max(0.1f, gap.y - gap.x), doorOpeningHeight, generatedWallThickness * 0.8f);
            }
            else
            {
                door.transform.position = new Vector3(fixedAxis, centerY, (gap.x + gap.y) * 0.5f);
                door.transform.localScale = new Vector3(generatedWallThickness * 0.8f, doorOpeningHeight, Mathf.Max(0.1f, gap.y - gap.x));
            }

            ACFObjectData data = Undo.AddComponent<ACFObjectData>(door);
            ApplyCategoryToObjectData(data, ACFCategoryUtility.Door);
            data.isBlockout = true;
            EditorUtility.SetDirty(data);
        }

        private void LinkSelectedKeyToDoors()
        {
            ACFKeyData key = selectedObjects.Select(obj => obj != null ? obj.GetComponent<ACFKeyData>() : null).FirstOrDefault(component => component != null);
            if (key == null)
            {
                GameObject keyObject = selectedObjects.FirstOrDefault(obj => obj != null && obj.GetComponent<ACFObjectData>() != null &&
                    obj.GetComponent<ACFObjectData>().category == ACFObjectData.ObjectCategory.Key);
                if (keyObject != null)
                {
                    key = keyObject.GetComponent<ACFKeyData>() ?? Undo.AddComponent<ACFKeyData>(keyObject);
                    key.keyId = string.IsNullOrWhiteSpace(key.keyId) ? keyObject.name : key.keyId;
                }
            }

            if (key == null)
            {
                EditorUtility.DisplayDialog("Missing Key", "Select at least one key object along with one or more doors.", "OK");
                return;
            }

            int linkedCount = 0;
            foreach (GameObject obj in selectedObjects)
            {
                if (obj == null || obj == key.gameObject)
                {
                    continue;
                }

                ACFObjectData objectData = obj.GetComponent<ACFObjectData>();
                if (objectData == null || objectData.category != ACFObjectData.ObjectCategory.Door)
                {
                    continue;
                }

                ACFDoorData door = obj.GetComponent<ACFDoorData>() ?? Undo.AddComponent<ACFDoorData>(obj);
                door.linkedKey = key;
                door.requiredKeyId = key.keyId;
                door.pushToOpen = true;
                EditorUtility.SetDirty(door);
                linkedCount++;
            }

            EditorUtility.DisplayDialog("Key Linked", $"Linked key '{key.keyId}' to {linkedCount} door(s).", "OK");
        }

        private void SetGeneratedObjectVisibility(bool hidden)
        {
            ACFObjectData[] objects = Object.FindObjectsByType<ACFObjectData>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (ACFObjectData data in objects)
            {
                if (data == null || !data.isBlockout)
                {
                    continue;
                }

                data.gameObject.SetActive(!hidden);
            }
        }

        private void DrawGeneratedObjectQuickTools()
        {
            EditorGUILayout.LabelField("Generated Object Visibility", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(generatedWallsHidden ? "Show Generated Objects" : "Hide Generated Objects"))
            {
                generatedWallsHidden = !generatedWallsHidden;
                SetGeneratedObjectVisibility(generatedWallsHidden);
            }
            if (GUILayout.Button("Hide Only Doors"))
            {
                SetGeneratedCategoryVisibility(ACFObjectData.ObjectCategory.Door, true);
            }
            if (GUILayout.Button("Show Only Doors"))
            {
                SetGeneratedCategoryVisibility(ACFObjectData.ObjectCategory.Door, false);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Hide Only Roofs"))
            {
                SetGeneratedCategoryVisibility(ACFObjectData.ObjectCategory.Roof, true);
            }
            if (GUILayout.Button("Show Only Roofs"))
            {
                SetGeneratedCategoryVisibility(ACFObjectData.ObjectCategory.Roof, false);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void SetGeneratedDoorVisibility(bool hidden)
        {
            SetGeneratedCategoryVisibility(ACFObjectData.ObjectCategory.Door, hidden);
        }

        private void SetGeneratedCategoryVisibility(ACFObjectData.ObjectCategory category, bool hidden)
        {
            ACFObjectData[] objects = Object.FindObjectsByType<ACFObjectData>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (ACFObjectData data in objects)
            {
                if (data == null || !data.isBlockout || data.category != category)
                {
                    continue;
                }

                data.gameObject.SetActive(!hidden);
            }
        }

        private Bounds? GetWorldBounds(GameObject gameObject)
        {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Collider[] colliders = gameObject.GetComponentsInChildren<Collider>(true);
                if (colliders.Length == 0)
                {
                    return null;
                }

                Bounds colliderBounds = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++)
                {
                    colliderBounds.Encapsulate(colliders[i].bounds);
                }

                return colliderBounds;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private void SnapSelectedFloorsToTarget()
        {
            if (mainFloorObject == null)
            {
                EditorUtility.DisplayDialog("Missing Main Floor", "Assign the floor object you want to move first.", "OK");
                return;
            }

            if (floorSnapTarget == null)
            {
                EditorUtility.DisplayDialog("Missing Target", "Assign a target floor object first.", "OK");
                return;
            }

            if (mainFloorObject == floorSnapTarget)
            {
                EditorUtility.DisplayDialog("Invalid Target", "Main Floor and Target Floor must be different objects.", "OK");
                return;
            }

            Bounds? targetBoundsOptional = GetWorldBounds(floorSnapTarget);
            if (!targetBoundsOptional.HasValue)
            {
                EditorUtility.DisplayDialog("Invalid Target", "The target floor does not have renderers or colliders to calculate bounds.", "OK");
                return;
            }

            Bounds? mainBoundsOptional = GetWorldBounds(mainFloorObject);
            if (!mainBoundsOptional.HasValue)
            {
                EditorUtility.DisplayDialog("Invalid Main Floor", "The main floor does not have renderers or colliders to calculate bounds.", "OK");
                return;
            }

            Bounds targetBounds = targetBoundsOptional.Value;
            Bounds mainBounds = mainBoundsOptional.Value;
            int mainFloorSnapSide = GetOppositeSide(targetFloorSnapSide);
            Vector3 offset = CalculateSnapOffset(mainBounds, targetBounds, mainFloorSnapSide, targetFloorSnapSide, floorSnapGap);

            Undo.RecordObject(mainFloorObject.transform, "Snap Floor To Edge");
            mainFloorObject.transform.position += offset;

            selectedObjects = new List<GameObject> { mainFloorObject };
            Selection.objects = selectedObjects.ToArray();
            Selection.activeGameObject = mainFloorObject;
            UpdatePivotSelection();
            StoreOriginalTransforms();

            EditorUtility.DisplayDialog("Floor Snap Complete", $"Snapped '{mainFloorObject.name}' to '{floorSnapTarget.name}'.", "OK");
        }

        private int GetOppositeSide(int sideIndex)
        {
            switch (sideIndex)
            {
                case 0: return 1;
                case 1: return 0;
                case 2: return 3;
                case 3: return 2;
                default: return 1;
            }
        }

        private Vector3 CalculateSnapOffset(Bounds movingBounds, Bounds targetBounds, int movingSide, int targetSide, float gap)
        {
            Vector3 offset = Vector3.zero;

            float movingPlane = GetBoundsSidePosition(movingBounds, movingSide);
            float targetPlane = GetBoundsSidePosition(targetBounds, targetSide);
            float signedGap = GetSideOutwardDirection(targetSide) * gap;

            if (targetSide <= 1 && movingSide <= 1)
            {
                offset.z = (targetPlane + signedGap) - movingPlane;
                offset.x = targetBounds.center.x - movingBounds.center.x;
            }
            else if (targetSide > 1 && movingSide > 1)
            {
                offset.x = (targetPlane + signedGap) - movingPlane;
                offset.z = targetBounds.center.z - movingBounds.center.z;
            }
            else
            {
                if (targetSide <= 1)
                {
                    offset.z = (targetPlane + signedGap) - movingBounds.center.z;
                }
                else
                {
                    offset.x = (targetPlane + signedGap) - movingBounds.center.x;
                }

                offset.x += targetBounds.center.x - movingBounds.center.x;
                offset.z += targetBounds.center.z - movingBounds.center.z;
            }

            return offset;
        }

        private float GetSideOutwardDirection(int sideIndex)
        {
            switch (sideIndex)
            {
                case 0:
                case 2:
                    return 1f;
                case 1:
                case 3:
                    return -1f;
                default:
                    return 0f;
            }
        }

        private float GetBoundsSidePosition(Bounds bounds, int sideIndex)
        {
            switch (sideIndex)
            {
                case 0: return bounds.max.z;
                case 1: return bounds.min.z;
                case 2: return bounds.max.x;
                case 3: return bounds.min.x;
                default: return 0f;
            }
        }

        private void SetSelectedFloorSubtype(bool isCorridor)
        {
            GameObject[] floors = Selection.gameObjects.Where(obj => obj != null).ToArray();
            if (floors.Length == 0)
            {
                EditorUtility.DisplayDialog("No Floor Selected", "Select one or more floor objects first.", "OK");
                return;
            }

            int updatedCount = 0;
            foreach (GameObject floor in floors)
            {
                ACFObjectData objectData = floor.GetComponent<ACFObjectData>();
                if (objectData == null)
                {
                    objectData = Undo.AddComponent<ACFObjectData>(floor);
                }

                Undo.RecordObject(objectData, "Set Floor Subtype");
                ApplyCategoryToObjectData(objectData, ACFCategoryUtility.Floor);
                objectData.customCategory = isCorridor ? CorridorFloorCustomCategory : string.Empty;
                EditorUtility.SetDirty(objectData);
                updatedCount++;
            }

            ScanScene();
            EditorUtility.DisplayDialog("Floor Subtype Updated", $"{updatedCount} floor object(s) marked as {(isCorridor ? "Corridor Floor" : "Room Floor")}.", "OK");
        }

        private void SetSelectedMaskFloorSubtype()
        {
            GameObject[] floors = Selection.gameObjects.Where(obj => obj != null).ToArray();
            if (floors.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Select one or more floor-like objects first.", "OK");
                return;
            }

            int updatedCount = 0;
            foreach (GameObject floor in floors)
            {
                ACFObjectData objectData = floor.GetComponent<ACFObjectData>();
                if (objectData == null)
                {
                    objectData = Undo.AddComponent<ACFObjectData>(floor);
                }

                Undo.RecordObject(objectData, "Set Mask Floor Subtype");
                ApplyCategoryToObjectData(objectData, ACFCategoryUtility.Floor);
                objectData.customCategory = MaskFloorCustomCategory;
                EditorUtility.SetDirty(objectData);
                updatedCount++;
            }

            ScanScene();
            EditorUtility.DisplayDialog("Mask Floor Updated", $"{updatedCount} object(s) marked as mask floors for upper-level layout.", "OK");
        }

        private void CreateMaskFloorFromSelectedRoofs()
        {
            GameObject[] roofs = Selection.gameObjects.Where(obj => obj != null).ToArray();
            if (roofs.Length == 0)
            {
                EditorUtility.DisplayDialog("No Roof Selected", "Select one or more roof blockouts first.", "OK");
                return;
            }

            int createdCount = 0;
            foreach (GameObject roof in roofs)
            {
                ACFObjectData roofData = roof.GetComponent<ACFObjectData>();
                if (roofData == null || roofData.category != ACFObjectData.ObjectCategory.Roof)
                {
                    continue;
                }

                GameObject maskFloor = Instantiate(roof, roof.transform.parent);
                Undo.RegisterCreatedObjectUndo(maskFloor, "Create Mask Floor");
                maskFloor.name = $"MaskFloor_{roof.name}";

                ACFObjectData maskData = maskFloor.GetComponent<ACFObjectData>();
                if (maskData == null)
                {
                    maskData = Undo.AddComponent<ACFObjectData>(maskFloor);
                }

                ApplyCategoryToObjectData(maskData, ACFCategoryUtility.Floor);
                maskData.customCategory = MaskFloorCustomCategory;
                maskData.isBlockout = true;
                EditorUtility.SetDirty(maskData);
                createdCount++;
            }

            ScanScene();
            EditorUtility.DisplayDialog("Mask Floors Created", $"Created {createdCount} mask floor object(s) from selected roofs.", "OK");
        }

        private bool IsCorridorFloor(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }

            ACFObjectData objectData = gameObject.GetComponent<ACFObjectData>();
            return objectData != null &&
                   objectData.category == ACFObjectData.ObjectCategory.Floor &&
                   objectData.customCategory == CorridorFloorCustomCategory;
        }

        private void SelectAllObjects()
        {
            selectedObjects.Clear();
            foreach (string category in ACFCategoryUtility.AllCategories)
            {
                selectedObjects.AddRange(GetObjectsForCategory(category).Where(obj => obj != null));
            }

            Selection.objects = selectedObjects.ToArray();
            UpdatePivotSelection();
            StoreOriginalTransforms();
            Debug.Log($"Selected all {selectedObjects.Count} objects");
        }

        private void UpdatePivotSelection()
        {
            if (selectedObjects.Count <= 0)
            {
                return;
            }

            selectedIndex = 0;
            selectedPivot = selectedObjects[0].transform;
            UpdateTransformFields();
        }

        private void SelectActivePivot()
        {
            if (selectedPivot != null)
            {
                Selection.activeGameObject = selectedPivot.gameObject;
            }
            else if (selectedObjects.Count > 0)
            {
                Selection.activeGameObject = selectedObjects[0];
                selectedPivot = selectedObjects[0].transform;
            }
        }

        private void ApplyTransformToSelected()
        {
            ApplyLiveTransforms();
        }

        private void UpdateTransformFields()
        {
            ResetLiveControls();
        }

        private void ApplyTransformToAllSelected()
        {
            ApplyLiveTransforms();
        }

        private void ResetSelectedTransforms()
        {
            foreach (GameObject obj in selectedObjects)
            {
                if (obj == null)
                {
                    continue;
                }

                if (originalPositions.TryGetValue(obj, out Vector3 originalPosition))
                {
                    Undo.RecordObject(obj.transform, "Reset Transform");
                    obj.transform.position = originalPosition;
                }
                if (originalRotations.TryGetValue(obj, out Vector3 originalRotation))
                {
                    obj.transform.eulerAngles = originalRotation;
                }
                if (originalScales.TryGetValue(obj, out Vector3 originalScale))
                {
                    obj.transform.localScale = originalScale;
                }
            }

            ResetLiveControls();
            Debug.Log($"Reset transforms for {selectedObjects.Count} objects");
        }

        private void ReplaceSelectedObjects()
        {
            if (selectedPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a replacement prefab first.", "OK");
                return;
            }

            List<GameObject> selected = new List<GameObject>();
            for (int i = 0; i < ACFCategoryUtility.AllCategories.Length; i++)
            {
                if (categorySelections[i])
                {
                    selected.AddRange(GetObjectsForCategory(ACFCategoryUtility.AllCategories[i]).Where(obj => obj != null));
                }
            }

            int replacedCount = 0;
            foreach (GameObject obj in selected)
            {
                GameObject newObj = PrefabUtility.InstantiatePrefab(selectedPrefab) as GameObject;
                if (newObj == null)
                {
                    continue;
                }

                newObj.transform.position = obj.transform.position;
                newObj.transform.rotation = obj.transform.rotation;
                newObj.transform.localScale = obj.transform.localScale;
                newObj.name = $"{selectedPrefab.name}_{obj.name}";

                replacementMap[obj] = newObj;
                Undo.DestroyObjectImmediate(obj);
                replacedCount++;
            }

            Debug.Log($"Replaced {replacedCount} objects with {selectedPrefab.name}");
            EditorUtility.DisplayDialog("Replace Complete", $"Replaced {replacedCount} objects with {selectedPrefab.name}", "OK");
        }

        private void AddCollidersToReplaced()
        {
            int addedCount = 0;
            foreach (KeyValuePair<GameObject, GameObject> pair in replacementMap)
            {
                if (pair.Value != null && pair.Value.GetComponent<Collider>() == null)
                {
                    Undo.AddComponent<MeshCollider>(pair.Value);
                    addedCount++;
                }
            }

            Debug.Log($"Added colliders to {addedCount} replaced objects");
            EditorUtility.DisplayDialog("Colliders Added", $"Added colliders to {addedCount} objects", "OK");
        }

        private void RunDeepScan()
        {
            diagnoseOutput = "=== ACF Deep Scene Diagnosis ===\n\n";
            diagnoseOutput += $"Scan Time: {System.DateTime.Now}\n";
            diagnoseOutput += $"Scene: {SceneManager.GetActiveScene().name}\n\n";

            GameObject[] allObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            int totalObjects = 0;
            foreach (GameObject obj in allObjects)
            {
                totalObjects += obj.GetComponentsInChildren<Transform>(true).Length;
            }

            diagnoseOutput += "=== Object Statistics ===\n";
            diagnoseOutput += $"Total Objects: {totalObjects}\n";
            diagnoseOutput += $"Root Objects: {allObjects.Length}\n\n";

            int meshRenderers = 0;
            int skinnedRenderers = 0;
            int particleSystems = 0;
            int lights = 0;
            int audioSources = 0;

            foreach (GameObject obj in allObjects)
            {
                meshRenderers += obj.GetComponentsInChildren<MeshRenderer>(true).Length;
                skinnedRenderers += obj.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
                particleSystems += obj.GetComponentsInChildren<ParticleSystem>(true).Length;
                lights += obj.GetComponentsInChildren<Light>(true).Length;
                audioSources += obj.GetComponentsInChildren<AudioSource>(true).Length;
            }

            diagnoseOutput += "=== Component Statistics ===\n";
            diagnoseOutput += $"Mesh Renderers: {meshRenderers}\n";
            diagnoseOutput += $"Skinned Mesh Renderers: {skinnedRenderers}\n";
            diagnoseOutput += $"Particle Systems: {particleSystems}\n";
            diagnoseOutput += $"Lights: {lights}\n";
            diagnoseOutput += $"Audio Sources: {audioSources}\n\n";

            int colliders = 0;
            int meshColliders = 0;
            int boxColliders = 0;
            int sphereColliders = 0;
            int capsuleColliders = 0;

            foreach (GameObject obj in allObjects)
            {
                Collider[] cols = obj.GetComponentsInChildren<Collider>(true);
                colliders += cols.Length;
                foreach (Collider col in cols)
                {
                    if (col is MeshCollider) meshColliders++;
                    else if (col is BoxCollider) boxColliders++;
                    else if (col is SphereCollider) sphereColliders++;
                    else if (col is CapsuleCollider) capsuleColliders++;
                }
            }

            diagnoseOutput += "=== Collider Statistics ===\n";
            diagnoseOutput += $"Total Colliders: {colliders}\n";
            diagnoseOutput += $"Mesh Colliders: {meshColliders}\n";
            diagnoseOutput += $"Box Colliders: {boxColliders}\n";
            diagnoseOutput += $"Sphere Colliders: {sphereColliders}\n";
            diagnoseOutput += $"Capsule Colliders: {capsuleColliders}\n\n";

            if (deepNameAnalysis)
            {
                AppendDeepNameAnalysis(allObjects);
            }

            diagnoseOutput += "=== Memory Estimation ===\n";
            diagnoseOutput += $"Approximate Memory Usage: {CalculateApproximateMemory(allObjects):F2} MB\n\n";

            diagnoseOutput += "=== Performance Warnings ===\n";
            if (meshColliders > colliders * 0.5f) diagnoseOutput += "High number of MeshColliders - consider using primitive colliders\n";
            if (skinnedRenderers > 10) diagnoseOutput += "High number of skinned meshes - consider LOD system\n";
            if (particleSystems > 20) diagnoseOutput += "High number of particle systems - may impact performance\n";
            if (lights > 10) diagnoseOutput += "High number of real-time lights - consider baking lighting\n";
            if (meshColliders <= colliders * 0.5f && skinnedRenderers <= 10 && particleSystems <= 20 && lights <= 10)
            {
                diagnoseOutput += "No major performance issues detected\n";
            }

            diagnoseOutput += "\n=== Category Statistics (From Scan) ===\n";
            diagnoseOutput += BuildCategorySummary();
            AppendGeneratedBlockoutStats();

            if (suggestCategorization)
            {
                AppendCategorizationSuggestions(allObjects);
            }

            Debug.Log(diagnoseOutput);
            EditorUtility.DisplayDialog("Deep Scan Complete", "Diagnosis complete. Check the Diagnose tab and Console for details.", "OK");
        }

        private float CalculateApproximateMemory(GameObject[] objects)
        {
            float totalMemory = 0;
            foreach (GameObject obj in objects)
            {
                Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.sharedMaterial != null)
                    {
                        totalMemory += 1.5f;
                    }
                }

                MeshFilter[] meshes = obj.GetComponentsInChildren<MeshFilter>(true);
                foreach (MeshFilter mesh in meshes)
                {
                    if (mesh.sharedMesh != null)
                    {
                        totalMemory += mesh.sharedMesh.vertexCount * 0.0001f;
                    }
                }
            }

            return totalMemory;
        }

        private void AppendDeepNameAnalysis(GameObject[] rootObjects)
        {
            diagnoseOutput += "=== Deep Name Analysis ===\n";

            Dictionary<string, List<GameObject>> namePatterns = AnalyzeObjectNames(rootObjects);
            foreach (KeyValuePair<string, List<GameObject>> pattern in namePatterns)
            {
                if (pattern.Value.Count == 0)
                {
                    continue;
                }

                diagnoseOutput += $"{pattern.Key}: {pattern.Value.Count} object(s)\n";
                if (pattern.Value.Count <= 5)
                {
                    diagnoseOutput += $"Examples: {string.Join(", ", pattern.Value.Select(obj => obj.name).Take(3).ToArray())}\n";
                }
            }

            List<GameObject> unusualNames = FindUnusualNamedObjects(rootObjects);
            diagnoseOutput += "\n=== Unusual Object Names ===\n";
            if (unusualNames.Count == 0)
            {
                diagnoseOutput += "No unusual object names detected.\n\n";
                return;
            }

            diagnoseOutput += $"Found {unusualNames.Count} renderable object(s) with weak name matches:\n";
            foreach (GameObject obj in unusualNames.Take(10))
            {
                diagnoseOutput += $"- {obj.name} at {obj.transform.position}\n";
            }

            if (unusualNames.Count > 10)
            {
                diagnoseOutput += $"... and {unusualNames.Count - 10} more\n";
            }

            diagnoseOutput += "\n";
        }

        private void AppendCategorizationSuggestions(GameObject[] rootObjects)
        {
            diagnoseOutput += "\n=== Categorization Suggestions ===\n";

            if (GetObjectsForCategory(ACFCategoryUtility.Wall).Length == 0 && GetObjectsForCategory(ACFCategoryUtility.Prop).Length > 0)
            {
                diagnoseOutput += "No walls detected but props exist. Check tall, thin objects that may actually be walls.\n";
            }

            if (GetObjectsForCategory(ACFCategoryUtility.Floor).Length == 0)
            {
                diagnoseOutput += "No floor objects detected. Check whether ground meshes need Floor tags or ACF data.\n";
            }

            int uncategorizedCount = 0;
            foreach (GameObject rootObject in rootObjects)
            {
                GameObject[] allChildren = rootObject.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject).ToArray();
                foreach (GameObject gameObject in allChildren)
                {
                    if (gameObject.GetComponent<Renderer>() == null)
                    {
                        continue;
                    }

                    if (gameObject.GetComponent<ACFObjectData>() == null)
                    {
                        uncategorizedCount++;
                    }
                }
            }

            if (uncategorizedCount > 0)
            {
                diagnoseOutput += $"{uncategorizedCount} renderable object(s) do not have ACFObjectData yet.\n";
            }

            diagnoseOutput += "\n";
        }

        private void AppendGeneratedBlockoutStats()
        {
            ACFObjectData[] objectDataComponents = Object.FindObjectsByType<ACFObjectData>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int generatedWalls = 0;
            int generatedDoors = 0;
            int generatedRoofs = 0;
            int corridorFloors = 0;
            int maskFloors = 0;

            foreach (ACFObjectData objectData in objectDataComponents)
            {
                if (objectData == null)
                {
                    continue;
                }

                if (IsCorridorFloor(objectData.gameObject))
                {
                    corridorFloors++;
                }

                if (objectData.category == ACFObjectData.ObjectCategory.Floor &&
                    objectData.customCategory == MaskFloorCustomCategory)
                {
                    maskFloors++;
                }

                if (!objectData.isBlockout)
                {
                    continue;
                }

                switch (objectData.category)
                {
                    case ACFObjectData.ObjectCategory.Wall:
                        generatedWalls++;
                        break;
                    case ACFObjectData.ObjectCategory.Door:
                        generatedDoors++;
                        break;
                    case ACFObjectData.ObjectCategory.Roof:
                        generatedRoofs++;
                        break;
                }
            }

            diagnoseOutput += "\n=== Generated Blockout Data ===\n";
            diagnoseOutput += $"Generated Wall Blockouts: {generatedWalls}\n";
            diagnoseOutput += $"Generated Door Blockouts: {generatedDoors}\n";
            diagnoseOutput += $"Generated Roof Blockouts: {generatedRoofs}\n";
            diagnoseOutput += $"Corridor Floors Tagged: {corridorFloors}\n";
            diagnoseOutput += $"Mask Floors Tagged: {maskFloors}\n";
        }

        private Dictionary<string, List<GameObject>> AnalyzeObjectNames(GameObject[] rootObjects)
        {
            Dictionary<string, List<GameObject>> patterns = new Dictionary<string, List<GameObject>>();
            foreach (string category in ACFCategoryUtility.AllCategories)
            {
                patterns[category] = new List<GameObject>();
            }

            foreach (GameObject rootObject in rootObjects)
            {
                GameObject[] allChildren = rootObject.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject).ToArray();
                foreach (GameObject gameObject in allChildren)
                {
                    if (ShouldSkipFromScan(gameObject))
                    {
                        continue;
                    }

                    if (ACFCategoryUtility.TryInferCategoryFromName(gameObject.name, out string category) && patterns.ContainsKey(category))
                    {
                        patterns[category].Add(gameObject);
                    }
                }
            }

            return patterns;
        }

        private void DrawCategoryResult(string category)
        {
            int count = GetObjectsForCategory(category).Length;
            EditorGUILayout.BeginHorizontal();
            GUI.color = ACFCategoryUtility.GetCategoryColor(category);
            EditorGUILayout.LabelField($"{GetDisplayName(category)}:", GUILayout.Width(120));
            GUI.color = Color.white;
            EditorGUILayout.LabelField($"{count}", EditorStyles.boldLabel);
            if (count > 0 && GUILayout.Button("Select", GUILayout.Width(50)))
            {
                SelectCategory(category);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCategoryObjectList(string category, int categoryIndex)
        {
            GameObject[] objects = GetObjectsForCategory(category);
            if (objects.Length == 0)
            {
                return;
            }

            categoryFoldouts[categoryIndex] = EditorGUILayout.Foldout(categoryFoldouts[categoryIndex], $"{GetDisplayName(category)} ({objects.Length})", true);
            if (!categoryFoldouts[categoryIndex])
            {
                return;
            }

            EditorGUI.indentLevel++;
            foreach (GameObject obj in objects)
            {
                if (obj == null)
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal();
                GUI.color = ACFCategoryUtility.GetCategoryColor(category);
                if (GUILayout.Button("o", GUILayout.Width(20)))
                {
                    Selection.activeGameObject = obj;
                    EditorGUIUtility.PingObject(obj);
                    if (SceneView.lastActiveSceneView != null)
                    {
                        SceneView.lastActiveSceneView.FrameSelected();
                    }
                }
                GUI.color = Color.white;
                EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                if (GUILayout.Button("P", GUILayout.Width(25), GUILayout.Height(18)))
                {
                    Selection.activeGameObject = obj;
                    Tools.current = Tool.Move;
                }
                if (GUILayout.Button("R", GUILayout.Width(25), GUILayout.Height(18)))
                {
                    Selection.activeGameObject = obj;
                    Tools.current = Tool.Rotate;
                }
                if (GUILayout.Button("S", GUILayout.Width(25), GUILayout.Height(18)))
                {
                    Selection.activeGameObject = obj;
                    Tools.current = Tool.Scale;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        private void SelectCategory(string category)
        {
            selectedObjects = GetObjectsForCategory(category).Where(obj => obj != null).ToList();
            Selection.objects = selectedObjects.ToArray();
            UpdatePivotSelection();
            StoreOriginalTransforms();
            Debug.Log($"Selected {Selection.gameObjects.Length} objects from {GetDisplayName(category)}");
        }

        private List<GameObject> FindUnusualNamedObjects(GameObject[] rootObjects)
        {
            List<GameObject> unusualNames = new List<GameObject>();
            foreach (GameObject rootObject in rootObjects)
            {
                GameObject[] allChildren = rootObject.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject).ToArray();
                foreach (GameObject gameObject in allChildren)
                {
                    if (ShouldSkipFromScan(gameObject))
                    {
                        continue;
                    }

                    if (gameObject.GetComponent<Renderer>() == null)
                    {
                        continue;
                    }

                    if (!ACFCategoryUtility.TryInferCategoryFromName(gameObject.name, out _))
                    {
                        unusualNames.Add(gameObject);
                    }
                }
            }

            return unusualNames;
        }

        private void OrganizeSceneHierarchy(GameObject[] rootObjects)
        {
            GameObject organizerRoot = GetOrCreateOrganizerRoot();
            Dictionary<string, Transform> categoryParents = new Dictionary<string, Transform>();

            foreach (string category in ACFCategoryUtility.AllCategories)
            {
                categoryParents[category] = GetOrCreateCategoryParent(organizerRoot.transform, category);
            }

            foreach (GameObject rootObject in rootObjects)
            {
                if (rootObject == null || rootObject == organizerRoot || rootObject.GetComponent<Camera>() != null || rootObject.GetComponent<Light>() != null)
                {
                    continue;
                }

                if (!ACFCategoryUtility.TryInferCategory(rootObject, out string category))
                {
                    continue;
                }

                Transform categoryParent = categoryParents[category];
                if (rootObject.transform.parent == categoryParent)
                {
                    continue;
                }

                Undo.SetTransformParent(rootObject.transform, categoryParent, "Organize ACF Hierarchy");
            }
        }

        private GameObject GetOrCreateOrganizerRoot()
        {
            GameObject organizerRoot = GameObject.Find("ACF_Organized");
            if (organizerRoot != null)
            {
                return organizerRoot;
            }

            organizerRoot = new GameObject("ACF_Organized");
            Undo.RegisterCreatedObjectUndo(organizerRoot, "Create ACF Organizer Root");
            return organizerRoot;
        }

        private bool ShouldSkipFromScan(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return true;
            }

            if (gameObject.name == "ACF_Organized")
            {
                return true;
            }

            Transform parent = gameObject.transform.parent;
            if (parent != null && parent.name == "ACF_Organized")
            {
                return true;
            }

            return false;
        }

        private Transform GetOrCreateCategoryParent(Transform organizerRoot, string category)
        {
            Transform existing = organizerRoot.Find(GetDisplayName(category));
            if (existing != null)
            {
                return existing;
            }

            GameObject categoryObject = new GameObject(GetDisplayName(category));
            Undo.RegisterCreatedObjectUndo(categoryObject, "Create ACF Category Root");
            categoryObject.transform.SetParent(organizerRoot, false);
            return categoryObject.transform;
        }

        private void ApplyScanPreviewMaterials()
        {
            EnsureScanPreviewMaterial();
            scanPreviewMaterial.color = new Color(1f, 1f, 1f, sourcePreviewOpacity);

            RestorePreviewMaterials();

            foreach (string category in ACFCategoryUtility.AllCategories)
            {
                foreach (GameObject obj in GetObjectsForCategory(category))
                {
                    if (obj == null)
                    {
                        continue;
                    }

                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer == null)
                    {
                        continue;
                    }

                    ACFObjectData objectData = obj.GetComponent<ACFObjectData>();
                    if (objectData != null && objectData.isBlockout)
                    {
                        continue;
                    }

                    if (!originalPreviewMaterials.ContainsKey(renderer))
                    {
                        originalPreviewMaterials[renderer] = renderer.sharedMaterials;
                    }

                    Material[] previewMaterials = new Material[renderer.sharedMaterials.Length];
                    for (int i = 0; i < previewMaterials.Length; i++)
                    {
                        previewMaterials[i] = scanPreviewMaterial;
                    }

                    renderer.sharedMaterials = previewMaterials;
                }
            }
        }

        private void EnsureScanPreviewMaterial()
        {
            if (scanPreviewMaterial != null)
            {
                return;
            }

            Shader shader = Shader.Find("Unlit/Transparent");
            scanPreviewMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            scanPreviewMaterial.mainTexture = CreateGridPreviewTexture();
            scanPreviewMaterial.color = new Color(1f, 1f, 1f, sourcePreviewOpacity);
        }

        private Texture2D CreateGridPreviewTexture()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color lightGray = new Color(0.72f, 0.72f, 0.72f, 1f);
            Color darkGray = new Color(0.45f, 0.45f, 0.45f, 1f);
            Color gridLine = new Color(0.3f, 0.3f, 0.3f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isGridLine = x % 16 == 0 || y % 16 == 0;
                    bool checker = ((x / 8) + (y / 8)) % 2 == 0;
                    texture.SetPixel(x, y, isGridLine ? gridLine : (checker ? lightGray : darkGray));
                }
            }

            texture.Apply();
            return texture;
        }

        private void RestorePreviewMaterials()
        {
            foreach (KeyValuePair<Renderer, Material[]> pair in originalPreviewMaterials)
            {
                if (pair.Key != null)
                {
                    pair.Key.sharedMaterials = pair.Value;
                }
            }

            originalPreviewMaterials.Clear();
        }

        private void StoreOriginalTransforms()
        {
            originalPositions.Clear();
            originalRotations.Clear();
            originalScales.Clear();

            foreach (GameObject obj in selectedObjects)
            {
                if (obj == null)
                {
                    continue;
                }

                originalPositions[obj] = obj.transform.position;
                originalRotations[obj] = obj.transform.eulerAngles;
                originalScales[obj] = obj.transform.localScale;
            }
        }

        private void ApplyLiveTransforms()
        {
            foreach (GameObject obj in selectedObjects)
            {
                if (obj == null)
                {
                    continue;
                }

                if (!originalPositions.TryGetValue(obj, out Vector3 basePosition) ||
                    !originalRotations.TryGetValue(obj, out Vector3 baseRotation) ||
                    !originalScales.TryGetValue(obj, out Vector3 baseScale))
                {
                    continue;
                }

                Undo.RecordObject(obj.transform, "Live Transform");
                obj.transform.position = basePosition + editPosition;
                obj.transform.eulerAngles = baseRotation + editRotation;
                obj.transform.localScale = new Vector3(
                    baseScale.x * editScale.x,
                    baseScale.y * editScale.y,
                    baseScale.z * editScale.z);
            }
        }

        private void ResetLiveControls()
        {
            editPosition = Vector3.zero;
            editRotation = Vector3.zero;
            editScale = Vector3.one;
        }

        private void InitializeCategoryDictionary()
        {
            categorizedObjects.Clear();
            foreach (string category in ACFCategoryUtility.AllCategories)
            {
                categorizedObjects[category] = new List<GameObject>();
            }
        }

        private GameObject[] GetObjectsForCategory(string category)
        {
            if (categorizedObjects.TryGetValue(category, out List<GameObject> objects))
            {
                return objects.ToArray();
            }

            return new GameObject[0];
        }

        private void ClearScan()
        {
            RestorePreviewMaterials();
            InitializeCategoryDictionary();
            selectedObjects.Clear();
            selectedPivot = null;
            selectedIndex = -1;
            scanCompleted = false;
            Debug.Log("Scan data cleared");
        }

        private void SelectFromCategories()
        {
            selectedObjects.Clear();

            for (int i = 0; i < ACFCategoryUtility.AllCategories.Length; i++)
            {
                if (!categorySelections[i])
                {
                    continue;
                }

                selectedObjects.AddRange(GetObjectsForCategory(ACFCategoryUtility.AllCategories[i]).Where(obj => obj != null));
            }

            Selection.objects = selectedObjects.ToArray();
            UpdatePivotSelection();
            StoreOriginalTransforms();
            Debug.Log($"Selected {selectedObjects.Count} objects from categories");
        }

        private void DrawCategoryCounts()
        {
            foreach (string category in ACFCategoryUtility.AllCategories)
            {
                EditorGUILayout.LabelField($"{category}: {GetObjectsForCategory(category).Length}");
            }
        }

        private string GetDisplayName(string category)
        {
            switch (category)
            {
                case ACFCategoryUtility.MovableProp: return "Movable Props";
                case ACFCategoryUtility.Landmark: return "Landmarks";
                case ACFCategoryUtility.Ignore: return "Ignored";
                default: return $"{category}s";
            }
        }

        private string BuildCategorySummary(string header = null)
        {
            List<string> lines = new List<string>();
            if (!string.IsNullOrEmpty(header))
            {
                lines.Add(header);
            }

            foreach (string category in ACFCategoryUtility.AllCategories)
            {
                lines.Add($"{category}: {GetObjectsForCategory(category).Length}");
            }

            return string.Join("\n", lines);
        }

        private bool IsConfiguredUnityTag(string category)
        {
            return InternalEditorUtility.tags.Contains(category);
        }

        private void AssignCategoryToSelected(string category)
        {
            if (!ACFCategoryUtility.IsStandardCategory(category))
            {
                Debug.LogWarning($"Category '{category}' is not part of the ACF standard category system.");
                return;
            }

            GameObject[] currentSelection = Selection.gameObjects;
            if (currentSelection.Length == 0)
            {
                Debug.LogWarning("Select one or more objects before assigning a category.");
                return;
            }

            bool unityTagExists = IsConfiguredUnityTag(category);
            if (!unityTagExists)
            {
                Debug.LogWarning($"Unity tag '{category}' is not configured yet. Add it in Tags and Layers to sync tags with ACF categories.");
            }

            foreach (GameObject obj in currentSelection)
            {
                AssignCategoryToObject(obj, category, unityTagExists);
            }

            Repaint();
            Debug.Log($"Assigned '{category}' to {currentSelection.Length} selected object(s).");
        }

        private void AssignCategoryToObject(GameObject obj, string category, bool setUnityTag = true)
        {
            ACFObjectData objectData = obj.GetComponent<ACFObjectData>();
            if (objectData == null)
            {
                objectData = Undo.AddComponent<ACFObjectData>(obj);
            }
            else
            {
                Undo.RecordObject(objectData, "Assign ACF Category");
            }

            ApplyCategoryToObjectData(objectData, category);
            objectData.customCategory = string.Empty;
            EditorUtility.SetDirty(objectData);

            if (setUnityTag && IsConfiguredUnityTag(category))
            {
                Undo.RecordObject(obj, "Assign ACF Category Tag");
                obj.tag = category;
                EditorUtility.SetDirty(obj);
            }
        }

        private bool TrySuggestCategory(GameObject gameObject, out string category)
        {
            if (ACFCategoryUtility.TryInferCategoryFromName(gameObject.name, out category))
            {
                return true;
            }

            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer == null)
            {
                category = string.Empty;
                return false;
            }

            Vector3 scale = gameObject.transform.localScale;
            if (scale.y > 2f && scale.x < 1.5f && scale.z < 1.5f)
            {
                category = ACFCategoryUtility.Wall;
                return true;
            }

            if (scale.y < 0.5f && (scale.x > 2f || scale.z > 2f))
            {
                category = ACFCategoryUtility.Floor;
                return true;
            }

            category = string.Empty;
            return false;
        }

        private void ApplyCategoryToObjectData(ACFObjectData objectData, string category)
        {
            switch (category)
            {
                case ACFCategoryUtility.Floor: objectData.category = ACFObjectData.ObjectCategory.Floor; break;
                case ACFCategoryUtility.Wall: objectData.category = ACFObjectData.ObjectCategory.Wall; break;
                case ACFCategoryUtility.Roof: objectData.category = ACFObjectData.ObjectCategory.Roof; break;
                case ACFCategoryUtility.Prop: objectData.category = ACFObjectData.ObjectCategory.Prop; break;
                case ACFCategoryUtility.MovableProp: objectData.category = ACFObjectData.ObjectCategory.MovableProp; break;
                case ACFCategoryUtility.Door: objectData.category = ACFObjectData.ObjectCategory.Door; break;
                case ACFCategoryUtility.Key: objectData.category = ACFObjectData.ObjectCategory.Key; break;
                case ACFCategoryUtility.Landmark: objectData.category = ACFObjectData.ObjectCategory.Landmark; break;
                case ACFCategoryUtility.Ignore: objectData.category = ACFObjectData.ObjectCategory.Ignore; break;
                default: objectData.category = ACFObjectData.ObjectCategory.Custom; break;
            }
        }

        private void DrawQuickAssignButtons()
        {
            EditorGUILayout.LabelField("Quick Category Assignment", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Add these tags in Unity Tags and Layers: Floor, Wall, Roof, Prop, MovableProp, Door, Key, Landmark, Ignore.", MessageType.Info);

            foreach (string category in ACFCategoryUtility.AllCategories)
            {
                if (GUILayout.Button(category))
                {
                    AssignCategoryToSelected(category);
                }
            }
        }
    }
}
