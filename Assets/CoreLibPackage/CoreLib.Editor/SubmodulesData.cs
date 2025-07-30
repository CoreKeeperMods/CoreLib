using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CoreLib.Editor
{
    [CreateAssetMenu(fileName = "SubmodulesData", menuName = "CoreLib/New SubmodulesData", order = 2)]
    public class SubmodulesData : ScriptableObject
    {
        public string[] submoduleNames;

        public void UpdateSubmoduleNames()
        {
            submoduleNames = AssetDatabase.FindAssets("t:ModBuilderSettings")
                .Select(guid =>
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    return AssetDatabase.LoadAssetAtPath<ModBuilderSettings>(path).metadata.name;
                }).ToArray();
        }
    }
    
    [CustomEditor(typeof(SubmodulesData))]
    public class SubmodulesDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var data = (SubmodulesData)target;
            if (!GUILayout.Button("Refresh")) return;
            data.UpdateSubmoduleNames();
            EditorUtility.SetDirty(data);
        }
    }
}