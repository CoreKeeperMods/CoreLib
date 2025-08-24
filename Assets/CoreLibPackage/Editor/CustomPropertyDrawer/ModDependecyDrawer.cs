using System;
using PugMod;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Editor
{
    [CustomPropertyDrawer(typeof(ModMetadata.Dependency))]
    public class ModDependencyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var mainProp = property.Copy();
            var textRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            mainProp.isExpanded = EditorGUI.Foldout(textRect, mainProp.isExpanded, property.displayName, true);
            if (!mainProp.isExpanded) return;
            EditorGUI.indentLevel++;
            for (bool enterChildren = true; property.NextVisible(enterChildren); enterChildren = false)
            {
                if (property.depth <= mainProp.depth) break;
                textRect.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(textRect, property, new GUIContent(property.displayName));
                if (property.name != "modName") continue;
                int index = Array.IndexOf(ModNameHelper.AllMods, property.stringValue ?? "<None>");
                if (index == -1) index = 0;
                textRect.y += EditorGUIUtility.singleLineHeight + 2;

                int newIndex = EditorGUI.Popup(textRect, " ", index, ModNameHelper.AllMods);

                if (EditorGUI.EndChangeCheck())
                    property.stringValue = newIndex != index
                        ? newIndex == 0 ? "" : ModNameHelper.AllMods[newIndex]
                        : property.stringValue;
            }
            EditorGUI.indentLevel--;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * (property.isExpanded ? 4 : 1) + 2;
        }
    }
}