using UnityEditor;
using UnityEngine;

namespace BuildingBlocks.UnityExtensions.Editor {

    public class MaterialManipulatorWindow : EditorWindow {
        
        //...
        private enum Tab { MaterialRipper, MaterialReplacer };
        private Tab selectedTab = Tab.MaterialRipper;

        private MaterialRipperWindow _materialRipperWindow;
        private MaterialReplacerWindow _materialReplacerWindow;

        private GUIStyle _activeButtonStyle;

        //...
        [MenuItem("Tools/BuildingBlocks/Utilities Extensions/Material Manipulator", false, 11)]
        public static void ShowWindow() {
            GetWindow<MaterialManipulatorWindow>("Material Manipulator");
        }

        //...
        private void OnEnable() {
            minSize = new Vector2(200f, 200f);
            maxSize = new Vector2(1000f, 1000f);
        }

        protected void OnDestroy() {
            _materialReplacerWindow = null;
            _materialRipperWindow = null;
        }

        private void OnGUI() {
            // Create the tabs
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Material Ripper", GetButtonStyle(Tab.MaterialRipper))) {
                selectedTab = Tab.MaterialRipper;
            }
            if (GUILayout.Button("Material Replacer", GetButtonStyle(Tab.MaterialReplacer))) {
                selectedTab = Tab.MaterialReplacer;
            }
            EditorGUILayout.EndHorizontal();

            // Show the selected tab content
            switch (selectedTab) {
                case Tab.MaterialRipper:
                    ShowMaterialRipper();
                    break;
                case Tab.MaterialReplacer:
                    ShowMaterialReplacer();
                    break;
            }
        }

        private GUIStyle GetButtonStyle(Tab tab) {
            // Start with the default toolbar button
            GUIStyle copy = new GUIStyle(EditorStyles.toolbarButton);
            copy.fixedHeight = EditorGUIUtility.singleLineHeight * 2f;
            copy.richText = true;
            copy.wordWrap = true;

            // Add some changes
            if (selectedTab == tab) {
                if (_activeButtonStyle == null) {
                    _activeButtonStyle = new GUIStyle(copy);
                    _activeButtonStyle.normal.textColor = Color.white;
                    _activeButtonStyle.hover.textColor = Color.white;
                    _activeButtonStyle.normal.background = Texture2D.whiteTexture;
                    _activeButtonStyle.hover.background = Texture2D.grayTexture;
                }

                _activeButtonStyle.normal.textColor = tab switch {
                    Tab.MaterialRipper => Color.cyan,
                    Tab.MaterialReplacer => Color.cyan,
                    _ => Color.white,
                };
                _activeButtonStyle.hover.textColor = tab switch {
                    Tab.MaterialRipper => Color.cyan,
                    Tab.MaterialReplacer => Color.cyan,
                    _ => Color.white,
                };

                copy = _activeButtonStyle;                
            }
            
            return copy;
        }

        private void ShowMaterialRipper() {
            if (_materialRipperWindow == null) {
                _materialRipperWindow = ScriptableObject.CreateInstance<MaterialRipperWindow>();
            }

            minSize = _materialRipperWindow.minSize;
            maxSize = _materialRipperWindow.maxSize;

            _materialRipperWindow.OnGUI();
        }

        private void ShowMaterialReplacer() {
            if (_materialReplacerWindow == null) {
                _materialReplacerWindow = ScriptableObject.CreateInstance<MaterialReplacerWindow>();
            }

            minSize = _materialReplacerWindow.minSize;
            maxSize = _materialReplacerWindow.maxSize;

            _materialReplacerWindow.OnGUI();
        }
    }
}