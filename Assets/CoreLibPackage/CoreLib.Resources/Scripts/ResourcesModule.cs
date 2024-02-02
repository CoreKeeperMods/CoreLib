using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

[assembly:InternalsVisibleTo("CoreLib.Audio")]
[assembly:InternalsVisibleTo("CoreLib.Commands")]
[assembly:InternalsVisibleTo("CoreLib.Drops")]
[assembly:InternalsVisibleTo("CoreLib.Editor")]
[assembly:InternalsVisibleTo("CoreLib.Entity")]
[assembly:InternalsVisibleTo("CoreLib.Equipment")]
[assembly:InternalsVisibleTo("CoreLib.JsonLoader")]
[assembly:InternalsVisibleTo("CoreLib.Localization")]
[assembly:InternalsVisibleTo("CoreLib.ModderTools")]
[assembly:InternalsVisibleTo("CoreLib.Resources")]
[assembly:InternalsVisibleTo("CoreLib.RewiredExtension")]
[assembly:InternalsVisibleTo("CoreLib.Tilesets")]

namespace CoreLib.ModResources
{
    public class ResourcesModule : BaseSubmodule
    {
        #region Public Interface

        public static void RegisterBundles(LoadedMod mod)
        {
            modAssetBundles.AddRange(mod.AssetBundles);
        }
        
        public static Object[] LoadSprites(string assetPath)
        {
            foreach (AssetBundle bundle in GetBundles())
            {
                foreach (string extension in spriteFileExtensions)
                {
                    if (!bundle.Contains(assetPath.WithExtension(extension))) continue;

                    var sprites = bundle.LoadAssetWithSubAssets(assetPath.WithExtension(extension), typeof(Sprite));

                    CoreLibMod.Log.LogDebug(
                        $"Loading registered assets {assetPath}, count {sprites.Length.ToString()}: Success");

                    return sprites;
                }
            }

            return Array.Empty<Object>();
        }

        /// <summary>
        /// Load asset from mod asset bundles
        /// </summary>
        /// <param name="assetPath">path to the asset</param>
        /// <param name="typeHint">Expected type of the asset</param>
        public static Object LoadAsset(string assetPath, Type typeHint)
        {
            foreach (AssetBundle bundle in GetBundles())
            {
                //if (!assetPath.ToLower().Contains(resource.keyWord.ToLower()) || !resource.HasAssetBundle()) continue;
                
                if (bundle.Contains(assetPath.WithExtension(".prefab")))
                {
                    Object prefab = bundle.LoadAsset<GameObject>(assetPath.WithExtension(".prefab"));
                    CoreLibMod.Log.LogDebug($"Loading registered asset {assetPath}: {(prefab != null ? "Success" : "Failure")}");
                    return prefab;
                }

                if (bundle.Contains(assetPath.WithExtension(".asset")))
                {
                    Object prefab = bundle.LoadAsset<ScriptableObject>(assetPath.WithExtension(".asset"));
                    CoreLibMod.Log.LogDebug($"Loading registered asset {assetPath}: {(prefab != null ? "Success" : "Failure")}");
                    return prefab;
                }

                if (bundle.Contains(assetPath.WithExtension(".mat")))
                {
                    Object material = bundle.LoadAsset<Material>(assetPath.WithExtension(".mat"));
                    CoreLibMod.Log.LogDebug($"Loading registered asset {assetPath}: {(material != null ? "Success" : "Failure")}");
                    return material;
                }

                foreach (string extension in spriteFileExtensions)
                {
                    if (!bundle.Contains(assetPath.WithExtension(extension))) continue;

                    Object sprite = bundle.LoadAsset(assetPath.WithExtension(extension), typeHint);

                    CoreLibMod.Log.LogDebug($"Loading registered asset {assetPath}: {(sprite != null ? "Success" : "Failure")}");

                    return sprite;
                }

                foreach (string extension in audioClipFileExtensions)
                {
                    if (!bundle.Contains(assetPath.WithExtension(extension))) continue;

                    Object audioClip = bundle.LoadAsset<Object>(assetPath.WithExtension(extension));
                    CoreLibMod.Log.LogDebug($"Loading registered asset {assetPath}: {(audioClip != null ? "Success" : "Failure")}");
                    return audioClip;
                }
            }

            CoreLibMod.Log.LogWarning($"Failed to find asset '{assetPath}' in mod assets!");
            return Resources.Load(assetPath);
        }

        /// <summary>
        /// Load asset from mod asset bundles and cast it
        /// </summary>
        /// <param name="path">path to the asset</param>
        /// <exception cref="ArgumentException">Thrown if asset is not found or can't be cast to T</exception>
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

        internal override GameVersion Build => new GameVersion(0, 7, 3, "a28f");
        internal override string Version => "3.1.0";
        
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

        internal static List<AssetBundle> modAssetBundles = new List<AssetBundle>();
        internal static List<AssetBundle> modulesAssetBundles = new List<AssetBundle>();

        internal static void RefreshModuleBundles()
        {
            modulesAssetBundles = API.ModLoader.LoadedMods
                .Where(mod => mod.Metadata.name.Contains("CoreLib"))
                .SelectMany(mod => mod.AssetBundles).ToList();
        }

        internal static string[] spriteFileExtensions = { ".jpg", ".png", ".tif" };
        internal static string[] audioClipFileExtensions = { ".mp3", ".ogg", ".wav", ".aif", ".flac" };

        #endregion
    }
    
}