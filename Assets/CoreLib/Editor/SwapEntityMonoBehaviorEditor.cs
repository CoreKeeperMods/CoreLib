using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace EditorKit.Editor
{
    [CustomEditor(typeof(EntityMonoBehaviour), true)]
    public class SwapEntityMonoBehaviorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Swap component"))
            {
                SwapEntityMonoBehaviorWindow.ShowWindow((EntityMonoBehaviour)target);
            }
        }
    }

    public class SwapEntityMonoBehaviorWindow : EditorWindow
    {
        private const string WINDOW_TITLE = "Swap component";

        private EntityMonoBehaviour _originalComponent;

        private static Type[] _derivedTypes;
        private static EditorCoroutine _searchCoroutine;
        
        private Type[] _filteredTypes;
        private GUIContent[] _filteredLabels;

        private string _currentTypeName = "";
        private string _lastTypeName = "";
        private Type _selectedType;

        private int _filteredIndex;
        private GUIStyle _customLabelStyle;

        public static void ShowWindow(EntityMonoBehaviour target)
        {
            var window = GetWindow<SwapEntityMonoBehaviorWindow>(WINDOW_TITLE);
            window.OnOpen(target);
        }

        private void OnOpen(EntityMonoBehaviour target)
        {
            _originalComponent = target;
            _currentTypeName = "";
            _lastTypeName = "";
            _selectedType = null;
            
            if (_derivedTypes != null)
                RefreshSearch(true);
        }

        private IEnumerator FindAllDerivedTypes(Type baseType)
        {
            var tmpList = new List<Type>();
            
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    tmpList.AddRange(assembly
                        .GetTypes()
                        .Where(t =>
                            t != baseType &&
                            baseType.IsAssignableFrom(t)
                        ));
                }
                catch (Exception)
                {
                    // ignored
                }
                yield return null;
            }

            _derivedTypes = tmpList.ToArray();
            EditorCoroutineUtility.StopCoroutine(_searchCoroutine);
            _searchCoroutine = null;
            RefreshSearch(true);
        }

        private void OnGUI()
        {
            if (_originalComponent == null)
            {
                GUILayout.Label("No object is selected!");
                return;
            }

            _customLabelStyle ??= new GUIStyle(GUI.skin.GetStyle("label"))
            {
                wordWrap = true
            };
            
            if (_derivedTypes == null)
            {
                _searchCoroutine ??= EditorCoroutineUtility.StartCoroutine(FindAllDerivedTypes(typeof(EntityMonoBehaviour)), this);
                GUILayout.Label("Please wait");
                return;
            }
            
            GUILayout.Label($"To perform swap of {_originalComponent.GetType().Name} on {_originalComponent.gameObject.name} " +
                            $"select new EntityMonoBehaviour derived class below:", _customLabelStyle);

            _currentTypeName = EditorGUILayout.TextField("New type", _currentTypeName);

            RefreshSearch();

            EditorGUI.BeginChangeCheck();

            var newIndex = EditorGUILayout.Popup(new GUIContent(" "), _filteredIndex, _filteredLabels);

            if (EditorGUI.EndChangeCheck())
            {
                _selectedType = newIndex < _filteredTypes.Length ? _filteredTypes[newIndex] : null;
                _currentTypeName = _selectedType.FullName;
                _filteredIndex = newIndex;
            }

            if (_selectedType != null)
            {
                GUILayout.Space(20);
                GUILayout.Label($"You have selected {_selectedType.FullName}!", _customLabelStyle);
                if (GUILayout.Button("Swap"))
                {
                    SwapComponent();
                }
            }
        }

        private void SwapComponent()
        {
            var gameObject = _originalComponent.gameObject;
            var newComponent = gameObject.AddComponent(_selectedType);

            var originalFields = _originalComponent.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var newFields = newComponent.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            foreach (FieldInfo field in originalFields)
            {
                var newField = newFields.FirstOrDefault(info => info.Name.Equals(field.Name));
                if (newField == null) continue;
                
                newField.SetValue(newComponent, field.GetValue(_originalComponent));
            }
            DestroyImmediate(_originalComponent);
            MoveToTop(newComponent);
            Close();
        }

        private static void MoveToTop(Component newComponent)
        {
            for (int i = 0; i < 10; i++)
            {
                bool result = UnityEditorInternal.ComponentUtility.MoveComponentUp(newComponent);
                if (!result) break;
            }
        }

        private void RefreshSearch(bool force = false)
        {
            if (_currentTypeName.Equals(_lastTypeName) && !force) return;

            _filteredTypes = _derivedTypes.Where(type => type.AssemblyQualifiedName.Contains(_currentTypeName, StringComparison.InvariantCultureIgnoreCase)).ToArray();
            _filteredLabels = _filteredTypes.Select(type => new GUIContent(type.FullName)).Append(new GUIContent("None")).ToArray();
            var filteredFullNames = _filteredTypes.Select(type => type.FullName).ToArray();

            _filteredIndex = Array.IndexOf(filteredFullNames, _currentTypeName);
            if (_filteredIndex == -1) _filteredIndex = _filteredTypes.Length;

            _lastTypeName = _currentTypeName;
        }
    }
}