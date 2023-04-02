using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace BuildingBlocks.UnityExtensions.Editor {

    public class MaterialReplacerWindow : EditorWindow {
        #region ***** Fields and Properties *****

        // Serialized Properties
        [SerializeField] private GameObject _parent;
        [SerializeField] private List<Material> _find = new List<Material>();
        [SerializeField] private List<Material> _replace = new List<Material>();

        // Serialized Properties
        private SerializedObject _serializedObject;
        private SerializedProperty _parentSP;
        private SerializedProperty _findSP;
        private SerializedProperty _replaceSP;
        private ReorderableList _findList;
        private ReorderableList _replaceList;

        // UI Constants
        private const int WINDOW_WIDTH = 400;
        private const int WINDOW_HEIGHT = 550;
        private const string TOOL_NAME = "Material Replacer";

        // Custom UI variables
        private Texture2D _bannerTexture;
        private GUIStyle _bannerStyle;
        private GUIStyle _introStyle;
        private GUIStyle _listHeaderStyle;
        private GUIStyle _historyStyle;

        private Vector2 _listScrollPos;
        private float _listHeight;
        private bool _listFoldedOut = false;
        private Vector2 _resultsScrollPos;
        private List<string> _changesMade = new List<string>();

        #endregion

        #region ***** Unity Callbacks *****

        public static void ShowWindow() {
            GetWindow<MaterialReplacerWindow>(TOOL_NAME);
        }

        public void OnEnable() {

            InitializeSerializedProperties();
            InitializeBannerTexture();
            InitializeWindow();
            InitializeReorderableList();
            InitializeGUIComponents();

        }

        public void OnGUI() {
            _serializedObject.Update();

            DrawBanner();
            DrawIntro();
            EditorGUILayout.Space();

            DrawParentField();
            EditorGUILayout.Space();

            DrawListContent();
            EditorGUILayout.Space();

            DrawButtons();

            DisplayResults();

            HandleDragAndDrop();

            _serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region ***** Initialization *****

        private void InitializeSerializedProperties() {
            _serializedObject = new SerializedObject(this);
            _parentSP = _serializedObject.FindProperty(nameof(_parent));
            _findSP = _serializedObject.FindProperty(nameof(_find));
            _replaceSP = _serializedObject.FindProperty(nameof(_replace));
        }

        private void InitializeBannerTexture() {
            _bannerTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/BuildingBlocks/UtilitiesAndExtensions/Editor/Resources/MaterialCreatorBanner.png");
        }

        private void InitializeWindow() {
            minSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
            maxSize = new Vector2(WINDOW_WIDTH + 1000f, WINDOW_HEIGHT + 1000f);
        }

        private void InitializeReorderableList() {
            _findList = new ReorderableList(_serializedObject, _findSP, true, true, true, true);
            _replaceList = new ReorderableList(_serializedObject, _replaceSP, true, true, true, true);

            float listWidth = position.width / 2;
            _findList.drawHeaderCallback = (Rect rect) => {
                DrawHeaderCallback(rect, _findSP, _findList, "Find");
            };
            _findList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                DrawElementCallback(rect, index, isActive, isFocused, _findSP, listWidth);
            };

            _replaceList.drawHeaderCallback = (Rect rect) => {
                DrawHeaderCallback(rect, _replaceSP, _replaceList, "Replace");
            };
            _replaceList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                DrawElementCallback(rect, index, isActive, isFocused, _replaceSP, listWidth);
            };
        }

        private void InitializeGUIComponents() {

            _bannerStyle = new GUIStyle() { richText = true, wordWrap = true };
            _bannerStyle.normal.textColor = Color.white;
            _bannerStyle.alignment = TextAnchor.MiddleCenter;

            _introStyle = new GUIStyle() { richText = true, wordWrap = true };
            _introStyle.normal.textColor = Color.white;
            _introStyle.alignment = TextAnchor.MiddleCenter;

            _listHeaderStyle = new GUIStyle() { richText = true, alignment = TextAnchor.MiddleLeft };

            _historyStyle = new GUIStyle() { richText = true };
            _historyStyle.normal.textColor = Color.white;

        }

        #endregion

        #region ***** Drawing *****

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
                    "\nThis tool helps you swap out materials throughout a hierarchy. " +
                    "Materials to find and matching materials to replace them with index for index."
            };

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(labelContent, _introStyle, GUILayout.MaxWidth(300f));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawParentField() {
            var cacheAlignment = EditorStyles.label.alignment;
            EditorStyles.label.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.PropertyField(_parentSP);
            EditorStyles.label.alignment = cacheAlignment;
        }

        private void DrawListContent() {
            _listScrollPos = EditorGUILayout.BeginScrollView(_listScrollPos, GUILayout.MaxHeight(_listHeight + 40));
            EditorGUILayout.BeginHorizontal();
            _findList.DoLayoutList();
            _replaceList.DoLayoutList();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        private void DrawButtons() {
            if (GUILayout.Button("Gather Materials")) {
                var changeCount = GatherMaterials(_parent, _findSP);
                if (_parent != null) {
                    _changesMade.Insert(0, $"{_parent.name} unique materials found: {changeCount}"); 
                }
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Find & Replace", GUILayout.Height(EditorGUIUtility.singleLineHeight * 2f))) {
                var changeCount = FindAndReplaceMaterials(_parent, _find, _replace);
                if (_parent != null) {
                    _changesMade.Insert(0, $"{_parent.name} renderers modified: {changeCount}"); 
                }
            }
        }

        private void DisplayResults() {
            _resultsScrollPos = EditorGUILayout.BeginScrollView(_resultsScrollPos);

            if (_changesMade.Count > 0) {
                EditorGUILayout.LabelField("Change History:");
                foreach (var message in _changesMade) {
                    EditorGUILayout.LabelField(message, _historyStyle);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeaderCallback(Rect rect, SerializedProperty serializedList, ReorderableList serializedRList, string label) {
            EditorGUILayout.BeginHorizontal();
            int arraySize = serializedList.arraySize;
            bool newCollapsedState = EditorGUI.Foldout(new Rect(rect.x, rect.y, 20f, rect.height), _listFoldedOut, GUIContent.none);
            rect.x += 20f;
            rect.width -= 70f;
            EditorGUI.LabelField(rect, $"<b><color=#A4A4A4>Materials To {label}:</color></b>", _listHeaderStyle);
            rect.x = rect.xMax;
            rect.width = 50f;
            var count = EditorGUI.DelayedIntField(rect, arraySize);
            EditorGUILayout.EndHorizontal();
            // List foldout draw
            if (EditorGUI.EndChangeCheck()) {
                if (_listFoldedOut != newCollapsedState) {
                    _listFoldedOut = newCollapsedState;
                    Repaint();
                }
                serializedRList.elementHeight = _listFoldedOut ? EditorGUIUtility.singleLineHeight : 0f;
                _listHeight = _listFoldedOut ? Mathf.Max(arraySize * EditorGUIUtility.singleLineHeight * 1.1f + EditorGUIUtility.singleLineHeight, 40f) : EditorGUIUtility.singleLineHeight;
            }
            // Resizing control
            if (count != arraySize) {
                if (count < 0) {
                    count = 0;
                }
                Resize(serializedList, count);
            }
        }
        private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused, SerializedProperty serializedList, float listWidth) {
            if (_listFoldedOut) {
                var element = serializedList.GetArrayElementAtIndex(index);
                var label = new GUIContent($"  {index}");
                EditorGUI.LabelField(new Rect(rect.x, rect.y, listWidth * 0.3f, rect.height), label);
                Rect propertyRect = new Rect(rect.x + listWidth * 0.3f, rect.y, listWidth * 0.7f, rect.height);
                EditorGUI.BeginProperty(propertyRect, label, element);
                EditorGUI.PropertyField(propertyRect, element, GUIContent.none);
                EditorGUI.EndProperty();
            }
        }

        #endregion

        #region ***** Helper Functions *****
        private int GatherMaterials(GameObject parent, SerializedProperty materialsProperty) {
            if (parent == null) {
                Debug.LogError($"{TOOL_NAME}: Parent is not set.");
                return 0;
            }

            // Clear the find list
            materialsProperty.ClearArray();

            // Get all renderers within the parent hierarchy
            var renderers = parent.GetComponentsInChildren<MeshRenderer>();

            // Get all unique materials and put them into the find list using LINQ
            var uniqueMaterials = renderers
                .SelectMany(renderer => renderer.sharedMaterials)
                .Distinct()
                .ToList();

            // Add unique materials to the SerializedProperty
            foreach (var material in uniqueMaterials) {
                materialsProperty.arraySize++;
                materialsProperty.GetArrayElementAtIndex(materialsProperty.arraySize - 1).objectReferenceValue = material;
            }
            return uniqueMaterials.Count;
        }

        private int FindAndReplaceMaterials(GameObject parent, IList<Material> find, IList<Material> replace) {
            // Checks
            if (parent == null) {
                Debug.LogError($"{TOOL_NAME}: Parent can not be null.");
                return 0;
            }
            if (find.Any((e) => e == null || replace.Any((e) => e == null))) {
                Debug.LogError($"{TOOL_NAME}: Null elements are not allowed.");
                return 0;
            }
            if (find.Count == 0 || replace.Count == 0 || find.Count != replace.Count) {
                Debug.LogError($"{TOOL_NAME}: Element counts must match.");
                return 0;
            }

            var renderers = parent.GetComponentsInChildren<MeshRenderer>(true);
            bool overallChange = false;
            int changed = 0;
            bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(parent);
            bool isPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(parent);
            if (isPrefabInstance || isPrefabAsset is false) {
                Undo.RecordObjects(renderers, "Material change");
            }

            foreach (var renderer in renderers) {
                var rendererMaterials = renderer.sharedMaterials;
                // Elements in replacements that are not in the rendererMaterials will be returned.
                // No difference; skip.
                if (!find.Any(mat => rendererMaterials.Contains(mat))) { continue; }
                var newArray = new Material[rendererMaterials.Length];

                for (int i = 0; i < rendererMaterials.Length; i++) {
                    int index = find.IndexOf(rendererMaterials[i]);

                    newArray[i] = index != -1 ? replace[index] : rendererMaterials[i];
                }

                renderer.sharedMaterials = newArray;
                changed++;
                overallChange = true;

                // Undo for scene prefab instances and objects, not prefab assets
                if (isPrefabInstance) {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(parent);
                }
                else if (isPrefabAsset is false) {
                    EditorUtility.SetDirty(parent);
                }
            }

            if (overallChange) {
                if (isPrefabInstance || isPrefabAsset is false) {
                    Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                }
            }
            return changed;
        }

        private void HandleDragAndDrop() {
            if (Event.current.type == EventType.DragUpdated) {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                Event.current.Use();
            }
            else if (Event.current.type == EventType.DragPerform) {
                DragAndDrop.AcceptDrag();
                PerformDragAndDrop(_find);
            }
        }

        private void PerformDragAndDrop<T>(List<T> objects) {
            if (DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0) {
                var gameObjects = new List<T>();
                foreach (var obj in DragAndDrop.objectReferences) {
                    if (obj is T go) {
                        gameObjects.Add(go);
                    }
                }
                objects.AddRange(gameObjects);
            }
        }

        private void Resize<T>(List<T> list, int count) {
            if (count > list.Count) {
                while (list.Count < count) {
                    list.Add(default);
                }
            }
            else {
                list.RemoveRange(count, list.Count - count);
            }
        }

        private void Resize(SerializedProperty list, int count) {
            if (count > list.arraySize) {
                for (int i = list.arraySize; i < count; i++) {
                    list.InsertArrayElementAtIndex(i);
                }
            }
            else {
                for (int i = list.arraySize - 1; i >= count; i--) {
                    list.DeleteArrayElementAtIndex(i);
                }
            }
        }

        #endregion
    }
}