using System;
using System.Collections;
using System.Linq;
using CoreLib.Scripts.Util.Atributes;
using CoreLib.Submodules.TileSet;
using UnityEditor;
using UnityEngine;
using Unity.EditorCoroutines.Editor;

namespace EditorKit.Editor
{
    [CustomPropertyDrawer(typeof(ModTilesetAttribute))]
    public class ModTilesetDrawer : PropertyDrawer
    {
        private static string[] _allModTilesets;
        private static bool _initialized;

        private int _selectedIndex;

        private static void Initialize(bool force = false)
        {
            if (_initialized && !force) return;
            
            string[] resultGUIDs = AssetDatabase.FindAssets("t:ModTileset");
            _allModTilesets = resultGUIDs.Select(guid =>
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                return AssetDatabase.LoadAssetAtPath<ModTileset>(path).tilesetId;
            }).ToArray();
            _initialized = true;
        }


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Initialize();
            EditorGUI.BeginProperty(position, label, property);

            var stringRect = new Rect(position.x, position.y, position.width, 18);
            var dropdownRect = new Rect(position.x, position.y + stringRect.height + 4, position.width, 18);

            var tilesetValue = property.stringValue;
            if (tilesetValue != null && !tilesetValue.Equals(""))
            {
                _selectedIndex = Array.IndexOf(_allModTilesets, tilesetValue);
                if (_selectedIndex == -1) _selectedIndex = 0;
            }

            var newValue = EditorGUI.TextField(stringRect, label, tilesetValue);
            var newIndex = EditorGUI.Popup(dropdownRect, " ", _selectedIndex, _allModTilesets);

            if (!newValue.Equals(tilesetValue))
            {
                property.stringValue = newValue;
            }else if (newIndex != _selectedIndex)
            {
                _selectedIndex = newIndex;
                property.stringValue = _allModTilesets[_selectedIndex];
            }
            
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 40;
        }

        public class MyAssetAssetModificationProcessor : AssetModificationProcessor
        {
            private static void OnWillCreateAsset(string assetName)
            {
                if(!assetName.Contains(".meta"))
                    EditorCoroutineUtility.StartCoroutineOwnerless(WaitCreation(assetName));
            }
            private static void OnCreated(Type type)
            {
                Initialize(true);
            }
            private static IEnumerator WaitCreation(string path)
            {
                Type type = AssetDatabase.GetMainAssetTypeAtPath(path);
                while (type == null)
                {
                    yield return null;
                    type = AssetDatabase.GetMainAssetTypeAtPath(path);
                }
                OnCreated(type);
            }
        }
    }
}