using CoreLib.Submodules.ModEntity.Components;
using UnityEditor;
using UnityEngine;

namespace CoreLib.Editor
{
    [CustomEditor(typeof(RuntimeMaterial))]
    public class RuntimeMaterialV2Editor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            RuntimeMaterial runtimeMaterial = (RuntimeMaterial)target;

            base.OnInspectorGUI();

            if (GUILayout.Button("Use material name"))
            {
                runtimeMaterial.UseMaterialName();
            }
            
            if (GUILayout.Button("Reassign material"))
            {
                runtimeMaterial.ReassignMaterial();
            }
        }
    }
}