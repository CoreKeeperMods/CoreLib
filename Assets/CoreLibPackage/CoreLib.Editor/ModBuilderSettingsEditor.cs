using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using CoreLib.Util.Extensions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CoreLib.Editor
{
    [CustomEditor(typeof(ModBuilderSettings))]
    public class ModBuilderSettingsEditor : UnityEditor.Editor
    {
        public bool buildBurst;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            if (!GUILayout.Button("Sync asmdef file")) return;
            var modInfo = (ModBuilderSettings)target;
            var asmdefPath = GetAsmDefFilePath(modInfo);

            var asmdefFileData = File.ReadAllText(asmdefPath);
            var asmdef = JsonUtility.FromJson<AsmDefModel>(asmdefFileData);

            if (IsInvalidAsmDefFile(asmdef, modInfo)) return;
            
            var asmReferences = asmdef.references.ToList();
            asmReferences.RemoveAll(assembly => ModNameHelper.AllMods.Contains(assembly));
            asmReferences.AddRange(modInfo.metadata.dependencies.Select(dependency => dependency.modName));
            asmdef.references = asmReferences.ToArray();
            var newFileData = JsonUtility.ToJson(asmdef, true);
            File.WriteAllText(asmdefPath, newFileData);
            
            AssetDatabase.Refresh();
            Debug.Log("Successfully edited asmdef file!");
        }

        private static bool IsInvalidAsmDefFile(AsmDefModel asmdef, ModBuilderSettings modInfo)
        {
            if (!asmdef.name.Equals(modInfo.name))
            {
                Debug.LogWarning("Mod asmdef file assembly name does not match mod name!");
                return true;
            }
            if (!asmdef.overrideReferences)
            {
                Debug.LogWarning("Mod asmdef does not have 'overrideReferences' enabled!");
                return true;
            }
            if (asmdef.references.Any(reference => reference.Contains("GUID")))
            {
                Debug.LogWarning("Mod asmdef file uses GUID assembly references! Please disable GUID assembly references!");
                return true;
            }

            return false;
        }

        private static string GetAsmDefFilePath(ModBuilderSettings modInfo)
        {
            var asmdefGuid = AssetDatabase.FindAssets($"{modInfo.metadata.name} t:AssemblyDefinitionAsset");
            if (asmdefGuid.Length <= 0) throw new Exception($"Failed to find asmdef file for {modInfo.metadata.name}!");

            var asmdefPath = AssetDatabase.GUIDToAssetPath(asmdefGuid[0]);
            return Path.Combine(Application.dataPath, "..", asmdefPath);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public class AsmDefModel
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
    
    /*
    [CustomPropertyDrawer(typeof(ModBuilderSettings))]
    public class OnChangedCallAttributePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position, property);
            if(EditorGUI.EndChangeCheck())
            {
                OnChangedCallAttribute at = attribute as OnChangedCallAttribute;
                MethodInfo method = property.serializedObject.targetObject.GetType().GetMethods().Where(m => m.Name == at.methodName).First();

                if (method != null && method.GetParameters().Count() == 0)// Only instantiate methods with 0 parameters
                    method.Invoke(property.serializedObject.targetObject, null);
            }
        }
    }*/
}