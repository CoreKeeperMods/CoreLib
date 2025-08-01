using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace CoreLib.Editor
{
    [CustomPropertyDrawer(typeof(SfxUnityInspectorFriendlyID))]
    public class SfxUnityInspectorFriendlyIDDrawer : PropertyDrawer
    {
        private Dictionary<string, string> currentSearch = new Dictionary<string, string>();

        private string clickName;
        private int lastClick;
        private bool didClick;

        public string GetCurrentSearch(string path)
        {
            if (currentSearch.ContainsKey(path))
            {
                return currentSearch[path];
            }

            currentSearch.Add(path, "");
            return "";
        }


        public static bool Contains(string source, string toCheck)
        {
            if (toCheck == null) return false;

            return source?.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public bool CheckEnum(SfxUnityInspectorFriendlyID objectID, string value)
        {
            if (Enum.TryParse(value, out SfxUnityInspectorFriendlyID objectID1))
            {
                return objectID == objectID1;
            }

            return false;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var intRect = new Rect(position.x, position.y, position.width / 2, 18);
            var enumRect = new Rect(position.x + position.width / 2 + 10, position.y, position.width / 2 - 10, 18);

            var dropDownRect = new Rect(enumRect.x, enumRect.y + enumRect.height + 4, enumRect.width, enumRect.height);

            SfxUnityInspectorFriendlyID current = (SfxUnityInspectorFriendlyID)property.intValue;

            int value = EditorGUI.IntField(intRect, label, property.intValue);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            string lastSearch = EditorGUI.TextField(enumRect, GetCurrentSearch(property.propertyPath));
            currentSearch[property.propertyPath] = lastSearch;

            if (EditorGUI.DropdownButton(dropDownRect, new GUIContent(current.ToString()), FocusType.Passive))
            {
                GenericMenu menu = new GenericMenu();
                List<string> items = property.enumNames.Where(s => Contains(s, lastSearch)).ToList();

                foreach (string item in items)
                {
                    menu.AddItem(new GUIContent(item), CheckEnum(current, item), data =>
                    {
                        if (Enum.TryParse((string)data, out SfxUnityInspectorFriendlyID objectID1))
                        {
                            lastClick = (int)objectID1;
                            clickName = property.propertyPath;
                            didClick = true;
                        }
                    }, item);
                }

                menu.ShowAsContext();
            }

            if (didClick && property.propertyPath.Equals(clickName))
            {
                property.intValue = lastClick;
                didClick = false;

            }
            else
            {
                property.intValue = value;
            }

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 40;
        }
    }
}