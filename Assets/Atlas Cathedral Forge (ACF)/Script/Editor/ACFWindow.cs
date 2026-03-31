using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

namespace ACFSystem
{
    public class ACFWindow : EditorWindow
    {
        private enum Tab { Scan, Edit, Final, Diagnose }
        private Tab currentTab = Tab.Scan;

        // Scan results
        private GameObject[] floors;
        private GameObject[] walls;
        private GameObject[] props;
        private GameObject[] movableProps;
        private GameObject[] keys;
        private GameObject[] doors;
        private GameObject[] roofs;

        private Dictionary<string, List<GameObject>> categorizedObjects;
        private bool scanCompleted = false;

        // Edit mode variables
        private enum SelectionMode { All, ByCategory, Manual }
        private SelectionMode currentSelectionMode = SelectionMode.ByCategory;
        private string[] categories = { "Floors", "Walls", "Props", "MovableProps", "Keys", "Doors", "Roofs" };
        private bool[] categorySelections = new bool[7];
        private List<GameObject> selectedObjects = new List<GameObject>();
        private Vector3 editPosition;
        private Vector3 editRotation;
        private Vector3 editScale;
        private Transform selectedPivot;

        // Replace tab variables
        private Dictionary<GameObject, GameObject> replacementMap = new Dictionary<GameObject, GameObject>();
        private GameObject selectedPrefab;

        // Diagnose tab variables
        private string diagnoseOutput = "";
        private Vector2 diagnoseScrollPos;

        [MenuItem("Tools/Atlas-Cathedral-Forge/ACF Window")]
        public static void ShowWindow()
        {
            ACFWindow window = GetWindow<ACFWindow>("Atlas-Cathedral-Forge");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            // Title
            EditorGUILayout.LabelField("Atlas-Cathedral-Forge (ACF)", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Tab buttons
            currentTab = (Tab)GUILayout.Toolbar((int)currentTab, new string[] { "Scan", "Edit", "Final", "Diagnose" });
            GUILayout.Space(20);

            switch (currentTab)
            {
                case Tab.Scan:
                    DrawScanTab();
                    break;
                case Tab.Edit:
                    DrawEditTab();
                    break;
                case Tab.Final:
                    DrawFinalTab();
                    break;
                case Tab.Diagnose:
                    DrawDiagnoseTab();
                    break;
            }
        }

        private void DrawScanTab()
        {
            EditorGUILayout.LabelField("Scene Scan & Categorization", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Scan Scene", GUILayout.Height(40)))
            {
                ScanScene();
            }

            GUILayout.Space(20);

            if (scanCompleted)
            {
                EditorGUILayout.LabelField("Scan Results:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Floors: {floors?.Length ?? 0}");
                EditorGUILayout.LabelField($"Walls: {walls?.Length ?? 0}");
                EditorGUILayout.LabelField($"Props: {props?.Length ?? 0}");
                EditorGUILayout.LabelField($"Movable Props: {movableProps?.Length ?? 0}");
                EditorGUILayout.LabelField($"Keys: {keys?.Length ?? 0}");
                EditorGUILayout.LabelField($"Doors: {doors?.Length ?? 0}");
                EditorGUILayout.LabelField($"Roofs: {roofs?.Length ?? 0}");

                GUILayout.Space(20);

                EditorGUILayout.LabelField("Block Out Prefabs", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Click below to create blockout prefabs from scanned geometry", MessageType.Info);

                if (GUILayout.Button("Create Blockout Prefabs"))
                {
                    CreateBlockoutPrefabs();
                }
            }
        }

        private void DrawEditTab()
        {
            if (!scanCompleted)
            {
                EditorGUILayout.HelpBox("Please scan the scene first in the Scan tab.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Edit Mode", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Selection mode
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
                    }
                    break;
            }

            GUILayout.Space(20);

            if (selectedObjects.Count > 0)
            {
                EditorGUILayout.LabelField($"Selected Objects: {selectedObjects.Count}", EditorStyles.boldLabel);
                GUILayout.Space(10);

                EditorGUILayout.LabelField("Transform Controls (Per Object Pivot)", EditorStyles.boldLabel);

                // Position
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Position", GUILayout.Width(80)))
                {
                    Tools.current = Tool.Move;
                    Tools.pivotMode = PivotMode.Pivot;
                    SelectActivePivot();
                }
                editPosition = EditorGUILayout.Vector3Field("", editPosition);
                EditorGUILayout.EndHorizontal();

                // Rotation
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Rotation", GUILayout.Width(80)))
                {
                    Tools.current = Tool.Rotate;
                    Tools.pivotMode = PivotMode.Pivot;
                    SelectActivePivot();
                }
                editRotation = EditorGUILayout.Vector3Field("", editRotation);
                EditorGUILayout.EndHorizontal();

                // Scale
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Scale", GUILayout.Width(80)))
                {
                    Tools.current = Tool.Scale;
                    Tools.pivotMode = PivotMode.Pivot;
                    SelectActivePivot();
                }
                editScale = EditorGUILayout.Vector3Field("", editScale);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10);

                if (GUILayout.Button("Apply Transform to Selected"))
                {
                    ApplyTransformToSelected();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No objects selected. Use the selection tools above.", MessageType.Info);
            }
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

        private void DrawDiagnoseTab()
        {
            EditorGUILayout.LabelField("Deep Scene Diagnosis", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Run Deep Scan", GUILayout.Height(40)))
            {
                RunDeepScan();
            }

            GUILayout.Space(20);

            EditorGUILayout.LabelField("Diagnosis Results:", EditorStyles.boldLabel);
            diagnoseScrollPos = EditorGUILayout.BeginScrollView(diagnoseScrollPos, GUILayout.Height(400));
            EditorGUILayout.TextArea(diagnoseOutput, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);

            if (GUILayout.Button("Copy to Clipboard"))
            {
                EditorGUIUtility.systemCopyBuffer = diagnoseOutput;
                Debug.Log("Diagnosis output copied to clipboard");
            }
        }

        private void DrawCategorySelection()
        {
            EditorGUILayout.LabelField("Select Categories:", EditorStyles.boldLabel);

            for (int i = 0; i < categories.Length; i++)
            {
                categorySelections[i] = EditorGUILayout.Toggle(categories[i], categorySelections[i]);
            }

            if (GUILayout.Button("Select from Categories"))
            {
                selectedObjects.Clear();

                if (categorySelections[0] && floors != null) selectedObjects.AddRange(floors);
                if (categorySelections[1] && walls != null) selectedObjects.AddRange(walls);
                if (categorySelections[2] && props != null) selectedObjects.AddRange(props);
                if (categorySelections[3] && movableProps != null) selectedObjects.AddRange(movableProps);
                if (categorySelections[4] && keys != null) selectedObjects.AddRange(keys);
                if (categorySelections[5] && doors != null) selectedObjects.AddRange(doors);
                if (categorySelections[6] && roofs != null) selectedObjects.AddRange(roofs);

                Selection.objects = selectedObjects.ToArray();
                UpdatePivotSelection();
                Debug.Log($"Selected {selectedObjects.Count} objects from categories");
            }
        }

        private void ScanScene()
        {
            GameObject[] allObjects = SceneManager.GetActiveScene().GetRootGameObjects();

            List<GameObject> floorsList = new List<GameObject>();
            List<GameObject> wallsList = new List<GameObject>();
            List<GameObject> propsList = new List<GameObject>();
            List<GameObject> movablePropsList = new List<GameObject>();
            List<GameObject> keysList = new List<GameObject>();
            List<GameObject> doorsList = new List<GameObject>();
            List<GameObject> roofsList = new List<GameObject>();

            foreach (GameObject obj in allObjects)
            {
                // Get all children recursively
                GameObject[] allChildren = obj.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject).ToArray();

                foreach (GameObject go in allChildren)
                {
                    string name = go.name.ToLower();

                    // Categorize based on name and tags
                    if (name.Contains("floor") || go.CompareTag("Floor"))
                        floorsList.Add(go);
                    else if (name.Contains("wall") || go.CompareTag("Wall"))
                        wallsList.Add(go);
                    else if (name.Contains("prop") || go.CompareTag("Prop"))
                        propsList.Add(go);
                    else if (name.Contains("movable") || name.Contains("move") || go.CompareTag("MovableProp"))
                        movablePropsList.Add(go);
                    else if (name.Contains("key") || go.CompareTag("Key"))
                        keysList.Add(go);
                    else if (name.Contains("door") || go.CompareTag("Door"))
                        doorsList.Add(go);
                    else if (name.Contains("roof") || go.CompareTag("Roof"))
                        roofsList.Add(go);
                    else if (go.GetComponent<Renderer>() != null)
                        propsList.Add(go); // Default to props
                }
            }

            floors = floorsList.ToArray();
            walls = wallsList.ToArray();
            props = propsList.ToArray();
            movableProps = movablePropsList.ToArray();
            keys = keysList.ToArray();
            doors = doorsList.ToArray();
            roofs = roofsList.ToArray();

            scanCompleted = true;

            Debug.Log($"ACF Scan Complete:\nFloors: {floors.Length}\nWalls: {walls.Length}\nProps: {props.Length}\nMovable Props: {movableProps.Length}\nKeys: {keys.Length}\nDoors: {doors.Length}\nRoofs: {roofs.Length}");

            EditorUtility.DisplayDialog("Scan Complete", $"Found {allObjects.Sum(obj => obj.GetComponentsInChildren<Transform>(true).Length)} objects\n\nCategorized into 7 types", "OK");
        }

        private void CreateBlockoutPrefabs()
        {
            // Create a folder for blockout prefabs
            string path = "Assets/ACF_Blockouts";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder("Assets", "ACF_Blockouts");

            int createdCount = 0;

            // Create blockout prefabs for each category
            createdCount += CreateBlockoutForCategory(floors, "Floor", path);
            createdCount += CreateBlockoutForCategory(walls, "Wall", path);
            createdCount += CreateBlockoutForCategory(props, "Prop", path);
            createdCount += CreateBlockoutForCategory(movableProps, "MovableProp", path);
            createdCount += CreateBlockoutForCategory(keys, "Key", path);
            createdCount += CreateBlockoutForCategory(doors, "Door", path);
            createdCount += CreateBlockoutForCategory(roofs, "Roof", path);

            AssetDatabase.Refresh();
            Debug.Log($"Created {createdCount} blockout prefabs");
            EditorUtility.DisplayDialog("Blockout Prefabs Created", $"Successfully created {createdCount} blockout prefabs in {path}", "OK");
        }

        private int CreateBlockoutForCategory(GameObject[] objects, string categoryName, string path)
        {
            int count = 0;

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == null) continue;

                // Create a simple cube as placeholder
                GameObject blockout = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blockout.name = $"{categoryName}_Blockout_{i}";
                blockout.transform.position = objects[i].transform.position;
                blockout.transform.rotation = objects[i].transform.rotation;
                blockout.transform.localScale = objects[i].transform.lossyScale;

                // Save as prefab
                string prefabPath = $"{path}/{blockout.name}.prefab";
                PrefabUtility.SaveAsPrefabAsset(blockout, prefabPath);
                DestroyImmediate(blockout);

                count++;
            }

            return count;
        }

        private void SelectAllObjects()
        {
            selectedObjects.Clear();
            selectedObjects.AddRange(floors);
            selectedObjects.AddRange(walls);
            selectedObjects.AddRange(props);
            selectedObjects.AddRange(movableProps);
            selectedObjects.AddRange(keys);
            selectedObjects.AddRange(doors);
            selectedObjects.AddRange(roofs);

            Selection.objects = selectedObjects.ToArray();
            UpdatePivotSelection();
            Debug.Log($"Selected all {selectedObjects.Count} objects");
        }

        private void UpdatePivotSelection()
        {
            if (selectedObjects.Count > 0)
            {
                selectedPivot = selectedObjects[0].transform;
                editPosition = selectedPivot.localPosition;
                editRotation = selectedPivot.localEulerAngles;
                editScale = selectedPivot.localScale;
            }
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
            foreach (GameObject obj in selectedObjects)
            {
                Undo.RecordObject(obj.transform, "Apply Transform");
                obj.transform.localPosition = editPosition;
                obj.transform.localEulerAngles = editRotation;
                obj.transform.localScale = editScale;
            }
            Debug.Log($"Applied transform to {selectedObjects.Count} objects");
        }

        private void ReplaceSelectedObjects()
        {
            if (selectedPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a replacement prefab first.", "OK");
                return;
            }

            List<GameObject> selected = new List<GameObject>();

            for (int i = 0; i < categories.Length; i++)
            {
                if (categorySelections[i])
                {
                    switch (i)
                    {
                        case 0: if (floors != null) selected.AddRange(floors); break;
                        case 1: if (walls != null) selected.AddRange(walls); break;
                        case 2: if (props != null) selected.AddRange(props); break;
                        case 3: if (movableProps != null) selected.AddRange(movableProps); break;
                        case 4: if (keys != null) selected.AddRange(keys); break;
                        case 5: if (doors != null) selected.AddRange(doors); break;
                        case 6: if (roofs != null) selected.AddRange(roofs); break;
                    }
                }
            }

            int replacedCount = 0;

            foreach (GameObject obj in selected)
            {
                if (obj == null) continue;

                GameObject newObj = PrefabUtility.InstantiatePrefab(selectedPrefab) as GameObject;
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

            foreach (var pair in replacementMap)
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

            // Mesh renderer analysis
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

            // Collider analysis
            int colliders = 0;
            int meshColliders = 0;
            int boxColliders = 0;
            int sphereColliders = 0;
            int capsuleColliders = 0;

            foreach (GameObject obj in allObjects)
            {
                var cols = obj.GetComponentsInChildren<Collider>(true);
                colliders += cols.Length;

                foreach (var col in cols)
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

            // Memory estimation
            diagnoseOutput += "=== Memory Estimation ===\n";
            diagnoseOutput += $"Approximate Memory Usage: {CalculateApproximateMemory(allObjects):F2} MB\n\n";

            // Performance warnings
            diagnoseOutput += "=== Performance Warnings ===\n";
            if (meshColliders > colliders * 0.5f)
                diagnoseOutput += "⚠ High number of MeshColliders - consider using primitive colliders\n";
            if (skinnedRenderers > 10)
                diagnoseOutput += "⚠ High number of skinned meshes - consider LOD system\n";
            if (particleSystems > 20)
                diagnoseOutput += "⚠ High number of particle systems - may impact performance\n";
            if (lights > 10)
                diagnoseOutput += "⚠ High number of real-time lights - consider baking lighting\n";

            diagnoseOutput += "\n=== Category Statistics (From Scan) ===\n";
            diagnoseOutput += $"Floors: {floors?.Length ?? 0}\n";
            diagnoseOutput += $"Walls: {walls?.Length ?? 0}\n";
            diagnoseOutput += $"Props: {props?.Length ?? 0}\n";
            diagnoseOutput += $"Movable Props: {movableProps?.Length ?? 0}\n";
            diagnoseOutput += $"Keys: {keys?.Length ?? 0}\n";
            diagnoseOutput += $"Doors: {doors?.Length ?? 0}\n";
            diagnoseOutput += $"Roofs: {roofs?.Length ?? 0}\n";

            // Send to console
            Debug.Log(diagnoseOutput);

            EditorUtility.DisplayDialog("Deep Scan Complete", "Diagnosis complete. Check the Diagnose tab and Console for details.", "OK");
        }

        private float CalculateApproximateMemory(GameObject[] objects)
        {
            float totalMemory = 0;

            foreach (GameObject obj in objects)
            {
                var renderers = obj.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    if (renderer.sharedMaterial != null)
                    {
                        totalMemory += 1.5f; // Approximate material memory
                    }
                }

                var meshes = obj.GetComponentsInChildren<MeshFilter>(true);
                foreach (var mesh in meshes)
                {
                    if (mesh.sharedMesh != null)
                    {
                        totalMemory += mesh.sharedMesh.vertexCount * 0.0001f; // Rough estimate
                    }
                }
            }

            return totalMemory;
        }
    }
}