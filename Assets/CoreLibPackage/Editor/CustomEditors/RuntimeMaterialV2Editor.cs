using CoreLib.Submodule.Entity.Components;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Editor
{
    [CustomEditor(typeof(RuntimeMaterial))]
    public class RuntimeMaterialV2Editor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var runtimeMaterial = (RuntimeMaterial)target;
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            if (GUILayout.Button("Use Material Name", GUILayout.Height(25)))
                runtimeMaterial.UseMaterialName();
            EditorGUILayout.Separator();
            if (GUILayout.Button("Reassign Material", GUILayout.Height(25)))
                runtimeMaterial.ReassignMaterial();
        }
    }
}