using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreLib.Submodules.ModResources;

[CoreLibSubmodule]
public static class ResourcesModule
{
    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
    public static bool Loaded
    {
        get => _loaded;
        internal set => _loaded = value;
    }
    
    /// <summary>
    /// Registers mod resources for loading
    /// </summary>
    /// <param name="resource"></param>
    public static void AddResource(ResourceData resource)
    {
        ThrowIfNotLoaded();
        modResources.Add(resource);
    }

    /// <summary>
    /// Load asset from mod asset bundles
    /// </summary>
    /// <param name="assetPath">path to the asset</param>
    public static Object LoadAsset(string assetPath)
    {
        ThrowIfNotLoaded();
        foreach (ResourceData resource in modResources)
        {
            if (!assetPath.ToLower().Contains(resource.keyWord.ToLower()) || !resource.HasAssetBundle()) continue;

            if (resource.bundle.Contains(assetPath.WithExtension(".prefab")))
            {
                Object prefab = resource.bundle.LoadAsset<GameObject>(assetPath.WithExtension(".prefab"));
                CoreLibPlugin.Logger.LogDebug($"Loading registered asset {assetPath}: {(prefab != null ? "Success" : "Failure")}");
                return prefab;
            }

            foreach (string extension in spriteFileExtensions)
            {
                if (!resource.bundle.Contains(assetPath.WithExtension(extension))) continue;

                Object sprite = resource.bundle.LoadAsset<Object>(assetPath.WithExtension(extension));

                CoreLibPlugin.Logger.LogDebug($"Loading registered asset {assetPath}: {(sprite != null ? "Success" : "Failure")}");

                return sprite;
            }

            foreach (string extension in audioClipFileExtensions)
            {
                if (!resource.bundle.Contains(assetPath.WithExtension(extension))) continue;

                Object audioClip = resource.bundle.LoadAsset<Object>(assetPath.WithExtension(extension));
                CoreLibPlugin.Logger.LogDebug($"Loading registered asset {assetPath}: {(audioClip != null ? "Success" : "Failure")}");
                return audioClip;
            }
        }

        CoreLibPlugin.Logger.LogWarning($"Failed to find asset '{assetPath}' in mod assets!");
        return Resources.Load(assetPath);
    }

    public static T LoadAsset<T>(string path)
    where T : Object
    {
        return LoadAsset(path).Cast<T>();
    }

    #region PrivateImplementation

    private static bool _loaded;
    internal static List<ResourceData> modResources = new List<ResourceData>();
    internal static string[] spriteFileExtensions = { ".jpg", ".png", ".tif" };
    internal static string[] audioClipFileExtensions = { ".mp3", ".ogg", ".waw", ".aif", ".flac" };

    internal static void ThrowIfNotLoaded()
    {
        if (!Loaded)
        {
            Type submoduleType = MethodBase.GetCurrentMethod().DeclaringType;
            string message = $"{submoduleType.Name} is not loaded. Please use [{nameof(CoreLibSubmoduleDependency)}(nameof({submoduleType.Name})]";
            throw new InvalidOperationException(message);
        }
    }

    private static string WithExtension(this string path, string extension)
    {
        if (path.EndsWith(extension))
        {
            return path;
        }

        return path + extension;
    }

    #endregion
}