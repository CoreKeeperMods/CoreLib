using System.Linq;
using UnityEditor;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
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
        
        private static void Initialize()
        {
            if (_initialized) return;
            
            string[] resultGUIDs = AssetDatabase.FindAssets("t:ModBuilderSettings");
            var submoduleInfoGUIDs = AssetDatabase.FindAssets("t:SubmodulesBuilder")
                .SelectMany(guid =>
                {
                    var asset = LoadAsset<SubmodulesBuilder>(guid);
                    return asset.submoduleNames;
                });
            _allMods = resultGUIDs
                .Select(guid => LoadAsset<ModBuilderSettings>(guid).metadata.name)
                .Union(submoduleInfoGUIDs)
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