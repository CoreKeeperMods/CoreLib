using System.Linq;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Editor
{
    [CreateAssetMenu(fileName = "Submodules Builder", menuName = "CoreLib/Submodules Builder")]
    public class SubmodulesBuilder : ScriptableObject
    {
        public string[] submoduleNames;
        
        public void UpdateSubmoduleNames()
        {
            submoduleNames = AssetDatabase.FindAssets("t:ModBuilderSettings")
                .Select(guid =>
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string metadataName = AssetDatabase.LoadAssetAtPath<ModBuilderSettings>(path).metadata.name;
                    return !string.IsNullOrWhiteSpace(metadataName) ? metadataName : null;
                })
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .OrderBy(n => n)
                .ToArray();
        }
    }
    
    [CustomEditor(typeof(SubmodulesBuilder))]
    public class SubmodulesDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var data = (SubmodulesBuilder)target;
            if (!GUILayout.Button("Update")) return;
            data.UpdateSubmoduleNames();
            EditorUtility.SetDirty(data);
            Debug.Log($"Updated Submodules");
        }
    }
}