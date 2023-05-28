using System;
using PugTilemap;
using UnityEditor;
using UnityEngine;

namespace EditorKit.Editor
{
    public class TiletypeEnumDrawer : MaterialPropertyDrawer
    {
        public override void OnGUI (Rect position, MaterialProperty prop, String label, MaterialEditor editor)
        {
            // Setup
            int value = prop.intValue;
            string[] names = Enum.GetNames(typeof(TileType));

            EditorGUI.BeginChangeCheck();
            //EditorGUI.showMixedValue = prop.hasMixedValue;

            value = EditorGUI.Popup(position, label, value, names);

            //EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                // Set the new value if it has changed
                prop.intValue = value;
            }
        }
    }
}