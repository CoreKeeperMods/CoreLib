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
        public static readonly List<Type> AllClassTypes = new();

        public ClassReferenceDropdownAttribute(Type classType)
        {
            AllClassTypes.Clear();
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
                        if (!AllClassTypes.Contains(type))
                            AllClassTypes.Add(type);
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            AllClassTypes.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
            AllClassTypes.Insert(0, null);
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
                string[] stringList = ClassReferenceDropdownAttribute.AllClassTypes.Select(t => t == null ? "<None>" : t.Name).ToArray();
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