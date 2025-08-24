using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Editor
{
    [CustomEditor(typeof(AssemblyDefinitionCreator.AssemblyDefinition))]
    public class AssemblyDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            Debug.Log("Test");
            
            // Add custom GUI elements here
            if (GUILayout.Button("Custom Action"))
            {
                // Implement your custom action here
                Debug.Log("Custom action executed for: ");
            }

            base.OnInspectorGUI();
        }
    }
}