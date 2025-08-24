using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace CoreLib.Editor
{
    [CustomPropertyDrawer(typeof(ObjectID))]
    public class ObjectIDDrawer : PropertyDrawer
    {
        private string _search;
        private bool _isInitialized;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!_isInitialized)
            {
                string name = property.enumNames[property.intValue];
                _search = name == "None" ? "" : name;
                _isInitialized = true;
            }
            
            EditorGUI.BeginProperty(position, label, property);
            {
                position.height = EditorGUIUtility.singleLineHeight;
                var newPosition = EditorGUI.PrefixLabel(position, label);
                
                EditorGUI.indentLevel = 0;
                var intPos = new Rect(newPosition.x, newPosition.y, newPosition.width * 0.12f, EditorGUIUtility.singleLineHeight);
                property.intValue = EditorGUI.IntField(intPos, property.intValue);
                
                var textPos = new Rect(intPos.x + intPos.width + EditorGUIUtility.standardVerticalSpacing, 
                    newPosition.y, 
                    newPosition.width - intPos.width - EditorGUIUtility.standardVerticalSpacing, 
                    EditorGUIUtility.singleLineHeight);
                _search = EditorGUI.TextField(textPos, _search);
                string[] filter = property.enumNames
                    .Where(x => 
                        string.IsNullOrEmpty(_search) || 
                        x.Contains(_search, StringComparison.CurrentCultureIgnoreCase) || 
                        x == "None")
                    .OrderBy(x => x == "None" ? "" : x)
                    .ToArray();
                int intValue = filter.ToList().IndexOf(property.enumNames[property.intValue]);
                
                newPosition.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                newPosition.x = position.x;
                newPosition.width = position.width;
                
                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUI.Popup(newPosition, " ", intValue, filter);
                if (EditorGUI.EndChangeCheck())
                {
                    property.intValue = property.enumNames.ToList().IndexOf(filter.ToList().ElementAt(newIndex));
                    _search = property.enumNames[property.intValue];
                }
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}