using System;
using System.Collections.Generic;
using System.Linq;
using PugMod;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Resource
{
    /// <summary>
    /// Represents a module that manages and interacts with mod-related resource asset bundles.
    /// </summary>
    public class ResourcesModule : BaseSubmodule
    {
        #region Public Interface
        
        public new const string Name = "Core Lib Resource";
        
        /// <summary>
        /// Registers asset bundles from a loaded mod into the module's list of asset bundles.
        /// </summary>
        /// <param name="mod">The loaded mod containing asset bundles to register.</param>
        public static void RegisterBundles(LoadedMod mod)
        {
            modAssetBundles.AddRange(mod.AssetBundles);
        }

        /// <summary>
        /// Loads all sprite assets from the specified asset path within the registered asset bundles.
        /// </summary>
        /// <param name="assetPath">The path to the asset to load sprites from.</param>
        /// <returns>An array of loaded sprite objects, or an empty array if no sprites are found.</returns>
        public static Object[] LoadSprites(string assetPath)
        {
            foreach (AssetBundle bundle in GetBundles())
            {
                foreach (string extension in spriteFileExtensions)
                {
                    if (!bundle.Contains(assetPath.WithExtension(extension))) continue;

                    var sprites = bundle.LoadAssetWithSubAssets(assetPath.WithExtension(extension), typeof(Sprite));

                    Log.LogInfo(
                        $"Loading registered assets {assetPath}, count {sprites.Length.ToString()}: Success");

                    return sprites;
                }
            }

            return Array.Empty<Object>();
        }

        /// <summary>
        /// Loads an asset from the mod's asset bundles or from Unity's Resources as a fallback.
        /// </summary>
        /// <param name="assetPath">The path to the asset within the asset bundle or Resources directory.</param>
        /// <param name="typeHint">The expected type of the asset to be loaded.</param>
        /// <returns>The loaded asset as an Object, or null if the asset could not be found.</returns>
        public static Object LoadAsset(string assetPath, Type typeHint)
        {
            foreach (AssetBundle bundle in GetBundles())
            {
                //if (!assetPath.ToLower().Contains(resource.keyWord.ToLower()) || !resource.HasAssetBundle()) continue;
                
                if (bundle.Contains(assetPath.WithExtension(".prefab")))
                {
                    Object prefab = bundle.LoadAsset<GameObject>(assetPath.WithExtension(".prefab"));
                    Log.LogInfo($"Loading registered asset {assetPath}: {(prefab != null ? "Success" : "Failure")}");
                    return prefab;
                }

                if (bundle.Contains(assetPath.WithExtension(".asset")))
                {
                    Object prefab = bundle.LoadAsset<ScriptableObject>(assetPath.WithExtension(".asset"));
                    Log.LogInfo($"Loading registered asset {assetPath}: {(prefab != null ? "Success" : "Failure")}");
                    return prefab;
                }

                if (bundle.Contains(assetPath.WithExtension(".mat")))
                {
                    Object material = bundle.LoadAsset<Material>(assetPath.WithExtension(".mat"));
                    Log.LogInfo($"Loading registered asset {assetPath}: {(material != null ? "Success" : "Failure")}");
                    return material;
                }

                foreach (string extension in spriteFileExtensions)
                {
                    if (!bundle.Contains(assetPath.WithExtension(extension))) continue;

                    Object sprite = bundle.LoadAsset(assetPath.WithExtension(extension), typeHint);

                    Log.LogInfo($"Loading registered asset {assetPath}: {(sprite != null ? "Success" : "Failure")}");

                    return sprite;
                }

                foreach (string extension in audioClipFileExtensions)
                {
                    if (!bundle.Contains(assetPath.WithExtension(extension))) continue;

                    Object audioClip = bundle.LoadAsset<Object>(assetPath.WithExtension(extension));
                    Log.LogInfo($"Loading registered asset {assetPath}: {(audioClip != null ? "Success" : "Failure")}");
                    return audioClip;
                }
            }

            Log.LogWarning($"Failed to find asset '{assetPath}' in mod assets!");
            return Resources.Load(assetPath);
        }

        /// <summary>
        /// Loads an asset from the specified path and casts it to the specified generic type.
        /// </summary>
        /// <typeparam name="T">The type to which the asset should be cast.</typeparam>
        /// <param name="path">The path to the asset to load.</param>
        /// <returns>The loaded and cast asset of type T.</returns>
        /// <exception cref="ArgumentException">Thrown if the asset is not found or cannot be cast to the specified type T.</exception>
        public static T LoadAsset<T>(string path)
            where T : Object
        {
            Object asset = LoadAsset(path, typeof(T));
            if (asset == null)
            {
                throw new ArgumentException($"Found no asset at path: {path}");
            }

            T typedAsset = (T)asset;
            if (typedAsset == null)
            {
                throw new ArgumentException($"Asset at path: {path} can't be cast to {typeof(T).FullName}!");
            }

            return typedAsset;
        }

        #endregion

        #region PrivateImplementation

        /// <summary>
        /// Initializes and configures the necessary resource-management components for the module.
        /// </summary>
        /// <remarks>
        /// This method applies a patch to extend the functionality of asset references, adds a custom resource locator
        /// for handling mod-specific resources, and registers a custom resource provider to the Addressables ResourceManager.
        /// It is an essential step to ensure proper integration of mod resources within the module.
        /// </remarks>
        internal override void Load()
        {
            CoreLibMod.Patch(typeof(AssetReference_Patch));
            
            Addressables.AddResourceLocator(new ModResourceLocator());
            Addressables.ResourceManager.ResourceProviders.Add(new ModResourceProvider());
        }

        /// <summary>
        /// Retrieves all asset bundles managed by the module, including both modules' and mods' asset bundles.
        /// </summary>
        /// <returns>An enumerable collection of asset bundles.</returns>
        internal static IEnumerable<AssetBundle> GetBundles()
        {
            foreach (AssetBundle bundle in modulesAssetBundles)
            {
                yield return bundle;
            }
            
            foreach (AssetBundle bundle in modAssetBundles)
            {
                yield return bundle;
            }
        }

        /// <summary>
        /// A static list that holds AssetBundle instances loaded from mod resources.
        /// This collection is used by the ResourcesModule to manage and access asset bundles
        /// provided by registered mods in the game.
        /// </summary>
        internal static List<AssetBundle> modAssetBundles = new List<AssetBundle>();

        /// <summary>
        /// A static list that holds AssetBundle instances associated with specific modules.
        /// This collection is used by the ResourcesModule to store and manage asset bundles
        /// originating from core library modules in the game.
        /// </summary>
        internal static List<AssetBundle> modulesAssetBundles = new List<AssetBundle>();

        /// <summary>
        /// Updates the list of module asset bundles by reloading them from all loaded mods
        /// whose metadata name contains "CoreLib".
        /// </summary>
        internal static void RefreshModuleBundles()
        {
            modulesAssetBundles = API.ModLoader.LoadedMods
                .Where(mod => mod.Metadata.name.Contains("CoreLib"))
                .SelectMany(mod => mod.AssetBundles).ToList();
        }

        /// <summary>
        /// An internal static array defining supported file extensions for sprite assets.
        /// This array is used by the ResourcesModule to identify and load sprite files
        /// from asset bundles when the asset path matches one of the specified extensions.
        /// </summary>
        internal static string[] spriteFileExtensions = { ".jpg", ".png", ".tif" };

        /// <summary>
        /// A static array containing supported file extensions for audio clip assets.
        /// This array is utilized by the ResourcesModule to identify and load audio assets
        /// from mod-related resource bundles based on their file type extensions.
        /// </summary>
        internal static string[] audioClipFileExtensions = { ".mp3", ".ogg", ".wav", ".aif", ".flac" };

        #endregion
    }
    
}