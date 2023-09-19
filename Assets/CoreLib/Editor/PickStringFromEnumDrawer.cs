using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(PickStringFromEnumAttribute))]
public class PickStringFromEnumDrawer : PropertyDrawer
{
    private string clickName;
    private string lastClick;
    private bool didClick;

    public static bool Contains(string source, string toCheck)
    {
        if (toCheck == null) return false;

        return source?.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        PickStringFromEnumAttribute pickStringFromEnumAttribute = (PickStringFromEnumAttribute)attribute;
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "PickStringFromEnumAttribute used on non-string type.");
            return;
        }

        Rect dropDownRect = new Rect(position.x, position.y, position.width * 0.4f, position.height);
        Rect textRect = new Rect(position.x + position.width * 0.4f, position.y, position.width * 0.6f, position.height);

        string currentValue = property.stringValue;

        if (EditorGUI.DropdownButton(dropDownRect, new GUIContent(currentValue), FocusType.Passive))
        {
            GenericMenu menu = new GenericMenu();
            List<string> items = Enum.GetNames(pickStringFromEnumAttribute.EnumType)
                .Where(s => Contains(s, currentValue)).ToList();

            foreach (string item in items)
            {
                menu.AddItem(new GUIContent(item), currentValue.Equals(item), data =>
                {
                    lastClick = (string)data;
                    clickName = property.propertyPath;
                    didClick = true;
                }, item);
            }

            menu.ShowAsContext();
        }

        if (didClick && property.propertyPath.Equals(clickName))
        {
            property.stringValue = lastClick;
            didClick = false;
        }

        EditorGUI.BeginChangeCheck();

        string text = EditorGUI.TextField(textRect, property.stringValue);

        if (EditorGUI.EndChangeCheck())
        {
            property.stringValue = text;
        }
    }
}