using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace BuildingBlocks.UnityExtensions.Editor {
    public class MaterialRipperWindow : EditorWindow {
        #region ***** Fields and Properties *****

        [SerializeField] private List<GameObject> _gameObjects = new List<GameObject>();
        [SerializeField] private string _folderPath = "Assets/Temp";
        [SerializeField] private string _materialNameAppend = " (Copy)";
        [SerializeField] private FolderOption _folderOption;

        private enum FolderOption { CreateSubFolderPerGameObject, DontCreateSubFolders }

        private const int WINDOW_WIDTH = 400;
        private const int WINDOW_HEIGHT = 550;
        private const string TOOL_NAME = "Material Ripper";

        private Texture2D _bannerTexture;
        private bool _listFoldedOut = false;
        private Vector2 _resultsScrollPos;
        private List<string> _materialsCreated = new List<string>();
        private Dictionary<Material, Material> _materialMapping = new Dictionary<Material, Material>();

        private ReorderableList _gameObjectList;
        private Vector2 _listScrollPos;
        private float _listHeight;

        private GUIStyle _historyStyle;
        private GUIStyle _bannerStyle;
        private GUIStyle _introStyle;
        private GUIStyle _listHeaderStyle;

        private SerializedObject _serializedObject;
        private SerializedProperty _gameObjectsSP;
        private SerializedProperty _folderPathSP;
        private SerializedProperty _materialNameAppendSP;
        private SerializedProperty _folderOptionSP;

        #endregion

        #region ***** Initialization *****

        public static void ShowWindow() {
            GetWindow<MaterialRipperWindow>(TOOL_NAME);
        }

        private void OnEnable() {
            InitializeSerializedProperties();
            InitializeBannerTexture();
            InitializeWindow();
            InitializeReorderableList();
            InitializeGUIComponents();
        }

        private void InitializeSerializedProperties() {
            _serializedObject = new SerializedObject(this);
            _gameObjectsSP = _serializedObject.FindProperty(nameof(_gameObjects));
            _folderPathSP = _serializedObject.FindProperty(nameof(_folderPath));
            _materialNameAppendSP = _serializedObject.FindProperty(nameof(_materialNameAppend));
            _folderOptionSP = _serializedObject.FindProperty(nameof(_folderOption));
        }

        private void InitializeBannerTexture() {
            _bannerTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/BuildingBlocks/UtilitiesAndExtensions/Editor/Resources/MaterialCreatorBanner.png");
        }

        private void InitializeWindow() {
            minSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
            maxSize = new Vector2(WINDOW_WIDTH + 1000f, WINDOW_HEIGHT + 1000f);
        }

        private void InitializeReorderableList() {
            _gameObjectList = new ReorderableList(_serializedObject, _gameObjectsSP, true, true, true, true);

            _gameObjectList.drawHeaderCallback = (Rect rect) => {
                DrawHeaderCallback(rect);
            };

            _gameObjectList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                DrawElementCallback(rect, index, isActive, isFocused);
            };
        }

        private void InitializeGUIComponents() {
            _historyStyle = new GUIStyle() { richText = true };
            _historyStyle.normal.textColor = Color.white;

            _bannerStyle = new GUIStyle() { richText = true, wordWrap = true };
            _bannerStyle.normal.textColor = Color.white;
            _bannerStyle.alignment = TextAnchor.MiddleCenter;

            _introStyle = new GUIStyle() { richText = true, wordWrap = true };
            _introStyle.normal.textColor = Color.white;
            _introStyle.alignment = TextAnchor.MiddleCenter;

            _listHeaderStyle = new GUIStyle() { richText = true, alignment = TextAnchor.MiddleLeft };
        }

        #endregion

        #region ***** GUI *****

        public void OnGUI() {
            _serializedObject.Update();

            DrawBanner();
            DrawIntro();
            EditorGUILayout.Space();

            DrawGameObjectsList();

            EditorGUILayout.Space();

            DrawFolderPath();
            DrawMaterialNameAppend();
            DrawFolderOption();

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Materials", GUILayout.Height(EditorGUIUtility.singleLineHeight * 2f))) {
                CreateMaterialsForGameObjects();
            }

            EditorGUILayout.BeginVertical();
            DisplayResults();
            EditorGUILayout.EndVertical();

            HandleDragAndDrop();

            _serializedObject.ApplyModifiedProperties();
        }

        private void DrawBanner() {
            float bannerHeight = 50f;
            Rect bannerRect = GUILayoutUtility.GetRect(position.width + 300, bannerHeight, GUI.skin.box);
            bannerRect.x = 0;
            GUI.Box(bannerRect, _bannerTexture, _bannerStyle);
        }

        private void DrawIntro() {
            GUIContent labelContent = new GUIContent {
                text =
                    $"Welcome to the <color=cyan>{TOOL_NAME}</color>! " +
                    "\nThis tool helps you group and " +
                    "organize Materials for your GameObjects. It creates a separate set of Materials for the selected " +
                    "GameObject and its hierarchy, allowing you to modify the new Materials in isolation." +
                    "\n\n" +
                    "* New Materials are only created per original instance per run, not per encounter. \nPrefabs supported."
            };

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(labelContent, _introStyle, GUILayout.MaxWidth(300f));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawGameObjectsList() {
            _listScrollPos = EditorGUILayout.BeginScrollView(_listScrollPos, GUILayout.MaxHeight(_listHeight + 40));
            _gameObjectList.DoLayoutList();
            EditorGUILayout.EndScrollView();
        }

        private void DrawFolderPath() {
            EditorGUILayout.HelpBox("Begin the path with the Assets folder.", MessageType.Info);
            EditorGUILayout.PropertyField(_folderPathSP, new GUIContent("Folder Path"));
        }

        private void DrawMaterialNameAppend() {
            EditorGUILayout.PropertyField(_materialNameAppendSP, new GUIContent("Material Name Append"));
        }

        private void DrawFolderOption() {
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_folderOptionSP, GUIContent.none, GUILayout.ExpandWidth(true));
        }

        private void DisplayResults() {
            _resultsScrollPos = EditorGUILayout.BeginScrollView(_resultsScrollPos);

            if (_materialsCreated.Count > 0) {
                EditorGUILayout.LabelField("Materials created:");
                foreach (var material in _materialsCreated) {
                    EditorGUILayout.LabelField(material, _historyStyle);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void HandleDragAndDrop() {
            if (Event.current.type == EventType.DragUpdated) {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                Event.current.Use();
            }
            else if (Event.current.type == EventType.DragPerform) {
                DragAndDrop.AcceptDrag();
                PerformDragAndDrop();
            }
        }

        #endregion

        #region ***** Callbacks *****

        private void DrawHeaderCallback(Rect rect) {
            EditorGUILayout.BeginHorizontal();
            bool newCollapsedState = EditorGUI.Foldout(new Rect(rect.x, rect.y, 20f, rect.height), _listFoldedOut, GUIContent.none);
            rect.x += 20f;
            rect.width -= 70f;
            EditorGUI.LabelField(rect, "<b><color=#A4A4A4>GameObjects To Run:</color></b>", _listHeaderStyle);
            rect.x = rect.xMax;
            rect.width = 50f;
            var count = EditorGUI.DelayedIntField(rect, _gameObjects.Count);
            EditorGUILayout.EndHorizontal();
            // List foldout draw
            if (EditorGUI.EndChangeCheck()) {
                if (_listFoldedOut != newCollapsedState) {
                    _listFoldedOut = newCollapsedState;
                    Repaint();
                }
                _gameObjectList.elementHeight = _listFoldedOut ? EditorGUIUtility.singleLineHeight : 0f;
                _listHeight = _listFoldedOut ? Mathf.Max(_gameObjects.Count * EditorGUIUtility.singleLineHeight * 1.1f + EditorGUIUtility.singleLineHeight, 40f) : EditorGUIUtility.singleLineHeight;
            }
            // Resizing control
            if (count != _gameObjects.Count) {
                if (count < 0) {
                    count = 0;
                }
                Resize(_gameObjects, count);
            }
        }
        private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused) {
            if (_listFoldedOut) {
                var element = _gameObjectsSP.GetArrayElementAtIndex(index);
                var label = element != null && element.objectReferenceValue != null ? new GUIContent(element.objectReferenceValue.name) : new GUIContent(element.displayName);
                EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width * 0.3f, rect.height), label);
                Rect propertyRect = new Rect(rect.x + rect.width * 0.3f, rect.y, rect.width * 0.7f, rect.height);
                EditorGUI.BeginProperty(propertyRect, label, element);
                EditorGUI.PropertyField(propertyRect, element, GUIContent.none);
                EditorGUI.EndProperty();
            }
        }

        #endregion

        #region ***** Helper Functions *****

        private void PerformDragAndDrop() {
            if (DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0) {
                var gameObjects = new List<GameObject>();
                foreach (var obj in DragAndDrop.objectReferences) {
                    if (obj is GameObject go) {
                        gameObjects.Add(go);
                    }
                }
                _gameObjects.AddRange(gameObjects);
            }
        }

        private void Resize<T>(List<T> list, int count) {
            if (count > list.Count) {
                // Increase the size of the list
                list.Capacity = count;
                while (list.Count < count) {
                    list.Add(default);
                }
            }
            else {
                // Reduce the size of the list
                list.RemoveRange(count, list.Count - count);
            }
        }

        private void CreateMaterialsForGameObjects() {
            _materialMapping.Clear();
            List<string> newMaterialNames = new List<string>();
            foreach (var go in _gameObjects) {
                newMaterialNames.AddRange(CreateMaterials(go));
            }
            if (newMaterialNames.Count > 0) {
                _materialsCreated.InsertRange(0, newMaterialNames);
                _materialsCreated.Insert(0, $"<color=cyan>*** {newMaterialNames.Count} materials created ***</color>");
            }
        }

        private List<string> CreateMaterials(GameObject gameObject) {
            //    Checks
            if (gameObject == null) {
                Debug.LogError($"{TOOL_NAME}: Please select a GameObject.");
                return null;
            }

            if (string.IsNullOrEmpty(_folderPath)) {
                Debug.LogError($"{TOOL_NAME}: Please enter a folder path.");
                return null;
            }

            // Create Sub Folder
            string subPath = _folderOption switch {
                FolderOption.CreateSubFolderPerGameObject => Path.Combine(_folderPath, gameObject.name),
                FolderOption.DontCreateSubFolders => _folderPath,
                _ => _folderPath,
            };
            if (Directory.Exists(subPath) is false) {
                Directory.CreateDirectory(subPath);
            }

            // Prepare for operation
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>( true);
            List<string> assetPaths = new List<string>();
            Material material = null;

            bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(gameObject);
            bool isPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(gameObject);
            if (isPrefabInstance || isPrefabAsset is false) {
                Undo.RecordObjects(renderers, "Material change");
            }

            // Run
            foreach (Renderer renderer in renderers) {
                Material originalMaterial = renderer.sharedMaterial;

                if (_materialMapping.ContainsKey(originalMaterial)) {
                    material = _materialMapping[originalMaterial];
                }
                else {
                    material = new Material(originalMaterial);
                    material.name = originalMaterial.name.Contains(_materialNameAppend) ?
                        originalMaterial.name :
                        originalMaterial.name + _materialNameAppend;
                    _materialMapping[originalMaterial] = material;

                    string assetPath = AssetDatabase.GenerateUniqueAssetPath(subPath + "/" + material.name + ".mat");
                    AssetDatabase.CreateAsset(material, assetPath);
                    assetPaths.Add(assetPath);

                    if (isPrefabInstance || isPrefabAsset is false) {
                        Undo.RegisterCreatedObjectUndo(material, "Create Material");
                    }
                }

                renderer.sharedMaterial = material;

                // Undo for scene prefab instances and objects, not prefab assets
                if (isPrefabInstance) {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.gameObject);
                }
                else if (isPrefabAsset is false) {
                    EditorUtility.SetDirty(renderer.gameObject);
                }
            }

            if (isPrefabInstance || isPrefabAsset is false) {
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(material);
            return assetPaths;
        }    

        #endregion
    }
}