using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace CoreLib.Editor
{
    [CustomPropertyDrawer(typeof(ObjectID))]
    public class ObjectIDDrawer : PropertyDrawer
    {
        private class PropertyState
        {
            public string search; 
            public string lastSearch;

            public string[] searchResults;
        
            public bool isInitialized;
        }
        
        private readonly Dictionary<string, PropertyState> _states = new();
        
        private PropertyState GetOrCreateState(SerializedProperty property)
        {
            if (!_states.TryGetValue(property.propertyPath, out var state))
            {
                state = new PropertyState();
                _states[property.propertyPath] = state;
            }
            return state;
        }
        

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!property.editable)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            
            PropertyState state = GetOrCreateState(property);
            
            if (!state.isInitialized)
            {
                string name = property.enumNames[property.enumValueIndex];
                state.search = name == "None" ? "" : name;
                state.lastSearch = "";
                state.isInitialized = true;
            }
            
            EditorGUI.BeginProperty(position, label, property);
            {
                position.height = EditorGUIUtility.singleLineHeight;
                var newPosition = EditorGUI.PrefixLabel(position, label);

                EditorGUI.indentLevel = 0;
                var intPos = new Rect(newPosition.x, newPosition.y, newPosition.width * 0.12f,
                    EditorGUIUtility.singleLineHeight);
                property.intValue = EditorGUI.IntField(intPos, property.intValue);

                var textPos = new Rect(intPos.x + intPos.width + EditorGUIUtility.standardVerticalSpacing,
                    newPosition.y,
                    newPosition.width - intPos.width - EditorGUIUtility.standardVerticalSpacing,
                    EditorGUIUtility.singleLineHeight);
                state.search = EditorGUI.TextField(textPos, state.search);

                if (state.search != state.lastSearch)
                {
                    state.searchResults = property.enumNames
                        .Where(x => 
                            string.IsNullOrEmpty(state.search) || 
                            x.Contains(state.search, StringComparison.CurrentCultureIgnoreCase) || 
                            x == "None")
                        .OrderBy(x => x == "None" ? "" : x)
                        .ToArray();
                    state.lastSearch = state.search;
                }

                int enumIndexValue = Array.IndexOf(state.searchResults, property.enumNames[property.enumValueIndex]);
                
                newPosition.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                newPosition.x = position.x;
                newPosition.width = position.width;
                
                EditorGUI.BeginChangeCheck();
                int newEnumIndex = EditorGUI.Popup(newPosition, " ", enumIndexValue, state.searchResults);
                if (EditorGUI.EndChangeCheck())
                {
                    var name = state.searchResults.ElementAt(newEnumIndex);
                    property.enumValueIndex = Array.IndexOf(property.enumNames, name);
                    state.search = property.enumNames[property.enumValueIndex];
                }
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.editable)
                return base.GetPropertyHeight(property, label);
            
            return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}