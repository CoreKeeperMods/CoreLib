using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Editor
{
    public class ClassReferenceDropdownAttribute : PropertyAttribute
    {
        public static readonly List<Type> ALL_CLASS_TYPES = new();

        public ClassReferenceDropdownAttribute(Type classType)
        {
            ALL_CLASS_TYPES.Clear();
            var tmpList = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    tmpList.AddRange(assembly
                        .GetTypes()
                        .Where(t => t.IsSubclassOf(classType))
                    );
                    foreach (var type in tmpList)
                    {
                        if (!ALL_CLASS_TYPES.Contains(type))
                            ALL_CLASS_TYPES.Add(type);
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            ALL_CLASS_TYPES.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
            ALL_CLASS_TYPES.Insert(0, null);
        }
        
    }
    
    [CustomPropertyDrawer(typeof(ClassReferenceDropdownAttribute))]
    public class ClassReferenceDropdown : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            {
                EditorGUI.BeginChangeCheck();
                position.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(position, property, label);
                var newPosition = position;
                newPosition.y += newPosition.height + EditorGUIUtility.standardVerticalSpacing;
                string[] stringList = ClassReferenceDropdownAttribute.ALL_CLASS_TYPES.Select(t => t == null ? "<None>" : t.Name).ToArray();
                string[] filteredList = FilterAllClassTypes(stringList, property.stringValue);
                int index = Array.FindIndex(filteredList, (t) => t == property.stringValue);
                if (index == -1) index = 0;
                int newIndex = EditorGUI.Popup(newPosition, " ", index, filteredList);
                if (EditorGUI.EndChangeCheck())
                {
                    property.stringValue = newIndex != index ? newIndex == 0 ? "" : filteredList[newIndex] : property.stringValue;
                }
            }
            EditorGUI.EndProperty();
        }

        private static string[] FilterAllClassTypes(string[] unfilteredClassTypes, string filter)
        {
            return unfilteredClassTypes.Where(t => t.Contains(filter, StringComparison.CurrentCultureIgnoreCase) || t == "<None>").ToArray();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2;
        }
    }
}