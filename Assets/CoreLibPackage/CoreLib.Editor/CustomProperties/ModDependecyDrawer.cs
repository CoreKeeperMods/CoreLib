using System;
using System.Collections.Generic;
using System.Linq;
using PugMod;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreLib.Editor
{
    [CustomPropertyDrawer(typeof(ModMetadata.Dependency))]
    public class ModDependecyDrawer : PropertyDrawer
    {
        private static Dictionary<string, int> selectedIndicies = new Dictionary<string, int>();

        private static int TryGetSelectedIndex(string path)
        {
            if (selectedIndicies.ContainsKey(path))
                return selectedIndicies[path];
            return 0;
        }

        private static void SetSelectedIndex(string path, int index)
        {
            selectedIndicies[path] = index;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var mainProp = property.Copy();
            var name = property.displayName;

            //get the values
            property.Next(true);
            var modName = property.Copy();
            property.Next(false);
            var required = property.Copy();

            var foldRect = new Rect(position.x, position.y, position.width, 18);

            mainProp.isExpanded = EditorGUI.Foldout(foldRect, mainProp.isExpanded, name);
            if (!mainProp.isExpanded) return;

            EditorGUI.indentLevel += 1;

            var textRect = new Rect(position.x, foldRect.y + foldRect.height + 4, position.width, 18);
            var dropDownRect = new Rect(position.x, textRect.y + textRect.height + 4, position.width, 18);

            var requiredRect = new Rect(dropDownRect.x, dropDownRect.y + dropDownRect.height + 4, position.width, 18);

            // Begin/end property & change check make each field
            // behave correctly when multi-object editing.
            EditorGUI.BeginProperty(textRect, label, modName);
            {
                EditorGUI.BeginChangeCheck();
                var currentValue = modName.stringValue;
                var newValue = EditorGUI.TextField(textRect, modName.name, currentValue);
                var index = TryGetSelectedIndex(modName.propertyPath);
                
                if (currentValue != null && !currentValue.Equals(""))
                {
                    index = Array.IndexOf(ModNameHelper.AllMods, currentValue);
                    if (index == -1) index = 0;
                    SetSelectedIndex(modName.propertyPath, index);
                }

                var newIndex = EditorGUI.Popup(dropDownRect, " ", index, ModNameHelper.AllMods);

                if (EditorGUI.EndChangeCheck())
                {
                    if (newIndex != index)
                    {
                        if (newIndex != 0)
                            modName.stringValue = ModNameHelper.AllMods[newIndex];
                        SetSelectedIndex(modName.propertyPath, newIndex);
                    }
                    else
                    {
                        modName.stringValue = newValue;
                    }
                }
            }
            EditorGUI.EndProperty();

            EditorGUI.BeginProperty(requiredRect, label, required);
            {
                EditorGUI.BeginChangeCheck();
                bool newValue = EditorGUI.Toggle(requiredRect, required.name, required.boolValue);
                if (EditorGUI.EndChangeCheck())
                    required.boolValue = newValue;
            }
            EditorGUI.EndProperty();

            EditorGUI.indentLevel -= 1;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded)
                return 21 * 4;
            return 20;
        }
    }
}