using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CoreLib.Editor
{
    internal static class ModNameHelper
    {
        private static string[] _allMods;
        private static bool _initialized;

        public static string[] AllMods
        {
            get {
                Initialize();
                return _allMods;
            }
        }
        
        private static void Initialize(bool force = false)
        {
            if (_initialized && !force) return;
            
            string[] resultGUIDs = AssetDatabase.FindAssets("t:ModBuilderSettings");
            string[] submoduleInfoGUIDs = AssetDatabase.FindAssets("t:SubmodulesData");
            _allMods = resultGUIDs
                .Select(guid => LoadAsset<ModBuilderSettings>(guid).metadata.name)
                .Union(
                    submoduleInfoGUIDs.SelectMany(guid =>
                    {
                        var asset = LoadAsset<ScriptableObject>(guid);
                        return (string[])asset
                            .GetType()
                            .GetField("submoduleNames")
                            .GetValue(asset);
                    }))
                .Prepend("<None>")
                .ToArray();
            _initialized = true;
        }
        
        private static T LoadAsset<T>(string guid) where T : Object
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}