using System;
using CoreLib.Util.Extension;
using I2.Loc;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Editor
{
    [CustomPropertyDrawer(typeof(GoogleLanguagesDropdownAttribute))]
    public class GoogleLanguageDropdownDrawer : PropertyDrawer 
    {
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
                throw new ArgumentException("GoogleLanguageDropdownAttribute can only be used on string properties");

            EditorGUI.BeginProperty(position, label, property);
            {
                // Filter language codes based on the search query
                var filteredCodes = GoogleLanguages.GetLanguagesForDropdown("", "");

                int selectedIndex = Mathf.Max(0, filteredCodes.IndexOf(property.stringValue));
                EditorGUI.BeginChangeCheck();
                selectedIndex = EditorGUI.Popup(position, "Language", selectedIndex, filteredCodes.ToArray());
                if (EditorGUI.EndChangeCheck())
                {
                    property.stringValue = filteredCodes[selectedIndex];
                }
            }
            EditorGUI.EndProperty();
            
        }
    }
}