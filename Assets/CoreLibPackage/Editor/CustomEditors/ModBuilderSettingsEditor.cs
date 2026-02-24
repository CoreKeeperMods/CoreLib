using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Editor
{
    [CustomEditor(typeof(ModBuilderSettings))]
    public class ModBuilderSettingsEditor : UnityEditor.Editor
    {
        public bool buildBurst;
        public GUISkin customSkin;
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            customSkin = AssetDatabase.LoadAssetAtPath<GUISkin>("Assets/CoreLibPackage/GUISkinSetup.guiskin");
            var modInfo = (ModBuilderSettings)target;
            
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            if (GUILayout.Button(new GUIContent("Sync asmdef file", EditorGUIUtility.IconContent("d_Refresh").image), GUILayout.Height(25)))
                SyncAsmDefFile(modInfo);
            EditorGUILayout.Separator();
            if (GUILayout.Button(new GUIContent("Open asmdef file", EditorGUIUtility.IconContent("SceneLoadOut").image), GUILayout.Height(25)))
                GoToAsmDefFile(modInfo);
            
            /* Burst Code can now be removed via script, this is no longer required.* /
            /*if (serializedObject.FindProperty("buildBurst") is not null) return;
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.HelpBox("Add the burst boolean to create Burst Files.", MessageType.Info);
            var rect = EditorGUILayout.GetControlRect();
            var center = rect.center;
            rect.width = 72;
            rect.x = center.x - rect.width / 2;
            if(EditorGUI.LinkButton(rect, "Tutorial Link"))
                Application.OpenURL("https://corekeepermods.github.io/?version=4#/editor-utilities/burst-files");
            EditorGUILayout.Separator();
            if (GUILayout.Button(new GUIContent("Open Script", EditorGUIUtility.IconContent("MetaFile Icon").image), GUILayout.Height(25)))
                AssetDatabase.OpenAsset(MonoScript.FromScriptableObject((ScriptableObject)target));*/
        }

        private static void GoToAsmDefFile(ModBuilderSettings modInfo)
        {
            string assemblyDefinitionPath  = Path.Combine(modInfo.modPath, modInfo.metadata.name + ".asmdef");
            var assemblyDefinition  = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(assemblyDefinitionPath);
            if (assemblyDefinition is null)
            {
                Debug.LogError("Assembly Definition not found at: " + assemblyDefinitionPath);
                return;
            }
            EditorGUIUtility.PingObject(assemblyDefinition);
            Selection.activeObject = assemblyDefinition;
            EditorWindow.focusedWindow.Focus();
        }

        private static void SyncAsmDefFile(ModBuilderSettings modInfo)
        {
            string asmdefPath = Path.Combine(Application.dataPath, "..", modInfo.modPath, modInfo.metadata.name + ".asmdef");
            string asmdefFileData = File.ReadAllText(asmdefPath);
            var asmdef = JsonUtility.FromJson<AsmDefModel>(asmdefFileData);
            
            if (asmdef.references.Any(reference => reference.Contains("GUID")))
            {
                Debug.LogWarning("Mod asmdef file uses GUID assembly references! Please disable GUID assembly references!");
                return;
            }
            
            if (!asmdef.overrideReferences) asmdef.overrideReferences = true;
            var asmReferences = asmdef.references.ToList();
            asmReferences.RemoveAll(assembly => string.IsNullOrEmpty(assembly) || ModNameHelper.AllMods.Contains(assembly));
            var newDependencies = modInfo.metadata.dependencies.Where(x => !string.IsNullOrEmpty(x.modName))
                .Select(dependency => dependency.modName);
            asmReferences.AddRange(newDependencies);
            asmdef.references = asmReferences.ToArray();
            string newFileData = JsonUtility.ToJson(asmdef, true);
            File.WriteAllText(asmdefPath, newFileData);
            
            AssetDatabase.Refresh();
            Debug.Log("Successfully edited asmdef file!");
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        // ReSharper disable once ClassNeverInstantiated.Local
        private class AsmDefModel
        {
            public string name;
            public string rootNamespace;
            public string[] references;
            public string[] includePlatforms;
            public string[] excludePlatforms;
            public bool allowUnsafeCode;
            public bool overrideReferences;
            public string[] precompiledReferences;
            public bool autoReferenced;
            public string[] defineConstraints;
            public string[] versionDefines;
            public bool noEngineReferences;
        }
    }
}