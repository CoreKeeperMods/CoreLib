using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace CoreLib.Editor
{
    [CustomPropertyDrawer(typeof(ObjectID))]
    public class ObjectIDDrawer : PropertyDrawer
    {
        private readonly Dictionary<string, string> _currentSearch = new();

        private string _clickName;
        private int _lastClick;
        private bool _didClick;

        public string GetCurrentSearch(string path)
        {
            if (_currentSearch.ContainsKey(path))
            {
                return _currentSearch[path];
            }

            _currentSearch.Add(path, "");
            return "";
        }


        public static bool Contains(string source, string toCheck)
        {
            if (toCheck == null) return false;

            return source?.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public bool CheckEnum(ObjectID objectID, string value)
        {
            if (Enum.TryParse(value, out ObjectID objectID1))
            {
                return objectID == objectID1;
            }

            return false;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Debug.Log($"{typeof(ObjectID)}");
            EditorGUI.BeginProperty(position, label, property);

            var intRect = new Rect(position.x, position.y, position.width / 2, 18);
            var enumRect = new Rect(position.x + position.width / 2 + 10, position.y, position.width / 2 - 10, 18);

            var dropDownRect = new Rect(enumRect.x, enumRect.y + enumRect.height + 4, enumRect.width, enumRect.height);

            ObjectID current = (ObjectID)property.intValue;

            int value = EditorGUI.IntField(intRect, label, property.intValue);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            string lastSearch = EditorGUI.TextField(enumRect, GetCurrentSearch(property.propertyPath));
            _currentSearch[property.propertyPath] = lastSearch;

            if (EditorGUI.DropdownButton(dropDownRect, new GUIContent(current.ToString()), FocusType.Passive))
            {
                GenericMenu menu = new GenericMenu();
                List<string> items = property.enumNames.Where(s => Contains(s, lastSearch)).ToList();

                foreach (string item in items)
                {
                    menu.AddItem(new GUIContent(item), CheckEnum(current, item), data =>
                    {
                        if (Enum.TryParse((string)data, out ObjectID objectID1))
                        {
                            _lastClick = (int)objectID1;
                            _clickName = property.propertyPath;
                            _didClick = true;
                        }
                    }, item);
                }

                menu.ShowAsContext();
            }

            if (_didClick && property.propertyPath.Equals(_clickName))
            {
                property.intValue = _lastClick;
                _didClick = false;

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