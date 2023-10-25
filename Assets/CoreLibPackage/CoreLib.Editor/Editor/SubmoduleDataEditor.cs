using System;
using System.Linq;
using CoreLib.Util;
using UnityEditor;
using UnityEngine;

namespace CoreLib.Editor
{
    [CustomEditor(typeof(SubmodulesData))]
    public class SubmodulesDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Refresh"))
            {
                var data = (SubmodulesData)target;
                string[] resultGUIDs = AssetDatabase.FindAssets("t:ModBuilderSettings");
                data.submoduleNames = resultGUIDs
                    .Select(guid =>
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        return AssetDatabase.LoadAssetAtPath<ModBuilderSettings>(path).metadata.name;
                    })
                    .Where(name => name.Contains("CoreLib", StringComparison.InvariantCultureIgnoreCase))
                    .ToArray();
                EditorUtility.SetDirty(data);
            }
        }
    }
}