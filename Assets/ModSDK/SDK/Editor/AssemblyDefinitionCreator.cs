using System;
using System.IO;
using UnityEngine;

public static class AssemblyDefinitionCreator
{
    public static bool WriteAssemblyDefinition(string assetPath, AssemblyDefinition assemblyDefinition)
    {
        try
        {
            var json = JsonUtility.ToJson(assemblyDefinition, true);
            File.WriteAllText(assetPath, json, System.Text.Encoding.UTF8);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public static AssemblyDefinition GetAssemblyDefinition(string assetPath)
    {
        try
        {
            var json = File.ReadAllText(assetPath);
            var asmdef = new AssemblyDefinition();
            JsonUtility.FromJsonOverwrite(json, asmdef);
            return asmdef;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public class AssemblyDefinition
    {
        public string name;
        public string[] references = Array.Empty<string>();
        public string[] includePlatforms = Array.Empty<string>();
        public string[] excludePlatforms = Array.Empty<string>();
        public bool allowUnsafeCode;
        public bool overrideReferences;
        public string[] precompiledReferences = Array.Empty<string>();
        public bool autoReferenced;
        public string[] defineConstraints = Array.Empty<string>();
        public VersionDefine[] versionDefines = Array.Empty<VersionDefine>();
        public bool useGUIDs;
    }

    [Serializable]
    public class VersionDefine
    {
        public string name;
        public string expression;
        public string define;
    }
}