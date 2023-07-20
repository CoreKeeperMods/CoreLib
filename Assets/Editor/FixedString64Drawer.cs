using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace EditorKit.Editor
{
    [CustomPropertyDrawer(typeof(FixedString64Bytes))]
    public class FixedString64Drawer : PropertyDrawer
    {

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

        private static void MainLogic(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            FixedString64Bytes stringObject = (FixedString64Bytes)property.GetValue();

            EditorGUI.BeginChangeCheck();

            string newString = EditorGUI.TextField(position, label, stringObject.Value);

            if (EditorGUI.EndChangeCheck())
            {
                property.SetValue(new FixedString64Bytes(newString));
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 20;
        }

    }
}