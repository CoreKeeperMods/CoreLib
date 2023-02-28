using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Components;
using UnityEditor;
using UnityEngine;

namespace EditorKit.Editor
{
    [CustomPropertyDrawer(typeof(CoreLib.Components.ModCraftData))]
    public class ModCraftingRecipeDrawer : PropertyDrawer
    {
        private Dictionary<string, int> currentInt = new Dictionary<string, int>();

        private string clickName;
        private ObjectID lastClick;
        private bool didClick;

        public int GetCurrentInt(string path)
        {
            if (currentInt.ContainsKey(path))
            {
                return currentInt[path];
            }

            currentInt.Add(path, 0);
            return 0;
        }


        public static bool Contains(string source, string toCheck)
        {
            if (toCheck == null) return false;

            return source?.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public bool CheckEnum(string name1, string name2)
        {
            if (Enum.TryParse(name1, true, out ObjectID objectID))
            {
                if (Enum.TryParse(name2, true, out ObjectID objectID1))
                {
                    return objectID == objectID1;
                }
            }

            return false;
        }

        public int GetInt(string name)
        {
            if (Enum.TryParse(name, true, out ObjectID objectID))
            {
                return (int)objectID;
            }

            return 0;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            try
            {
                MainLogic(position, property, label);
            }
            catch (Exception)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        private void MainLogic(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var intRect = new Rect(position.x, position.y, position.width / 2, 18);
            var enumRect = new Rect(position.x + position.width / 2 + 10, position.y, position.width / 2 - 10, 18);

            var dropDownRect = new Rect(enumRect.x, enumRect.y + enumRect.height + 4, enumRect.width, enumRect.height);

            ModCraftData modCraftData = property.GetValue() as ModCraftData;

            int intValue = GetCurrentInt(property.propertyPath);
            if (intValue == 0)
                intValue = GetInt(modCraftData.item.ToString());

            EditorGUI.BeginChangeCheck();

            int lastInt = EditorGUI.IntField(intRect, nameof(modCraftData.item), intValue);
            currentInt[property.propertyPath] = lastInt;

            if (EditorGUI.EndChangeCheck())
            {
                modCraftData.item = ((ObjectID)lastInt).ToString();
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            EditorGUI.BeginChangeCheck();

            modCraftData.item = EditorGUI.TextField(enumRect, modCraftData.item.ToString());

            if (EditorGUI.EndChangeCheck())
            {
                if (Enum.TryParse(modCraftData.item.ToString(), out ObjectID objectID1))
                {
                    currentInt[property.propertyPath] = (int)objectID1;
                }

                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }

            if (EditorGUI.DropdownButton(dropDownRect, new GUIContent(modCraftData.item.ToString()), FocusType.Passive))
            {
                GenericMenu menu = new GenericMenu();
                List<string> items = Enum.GetNames(typeof(ObjectID)).Where(s => Contains(s, modCraftData.item.ToString())).ToList();

                foreach (string item in items)
                {
                    menu.AddItem(new GUIContent(item), CheckEnum(modCraftData.item.ToString(), item), data =>
                    {
                        if (Enum.TryParse((string)data, out ObjectID objectID1))
                        {
                            lastClick = objectID1;
                            clickName = property.propertyPath;
                            didClick = true;
                        }
                    }, item);
                }

                menu.ShowAsContext();
            }

            if (didClick && property.propertyPath.Equals(clickName))
            {
                modCraftData.item = lastClick.ToString();
                currentInt[property.propertyPath] = (int)lastClick;
                didClick = false;
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }

            EditorGUI.indentLevel = indent;

            var amountRect = new Rect(position.x, position.y + 40, position.width, 18);

            EditorGUI.BeginChangeCheck();

            modCraftData.amount = EditorGUI.IntField(amountRect, nameof(modCraftData.amount), modCraftData.amount);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 60;
        }
    }
}