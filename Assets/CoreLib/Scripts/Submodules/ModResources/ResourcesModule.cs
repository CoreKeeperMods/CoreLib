﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreLib.Submodules.ModResources
{
    public class ResourcesModule : BaseSubmodule
    {
        #region Public Interface

        public static Object[] LoadSprites(string assetPath)
        {
            foreach (AssetBundle bundle in modAssetBundles)
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
        [Obsolete("To ensure correct results use the generic or typed version")]
        public static Object LoadAsset(string assetPath)
        {
            return LoadAsset(assetPath, typeof(Object));
        }

        /// <summary>
        /// Load asset from mod asset bundles
        /// </summary>
        /// <param name="assetPath">path to the asset</param>
        /// <param name="typeHint">Expected type of the asset</param>
        public static Object LoadAsset(string assetPath, Type typeHint)
        {
            RefreshResources();
            foreach (AssetBundle bundle in modAssetBundles)
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

        internal override GameVersion Build => new GameVersion(0, 0, 0, 0, "");
        internal static ResourcesModule Instance => CoreLibMod.GetModuleInstance<ResourcesModule>();

        internal static List<AssetBundle> modAssetBundles = new List<AssetBundle>();
        internal static int lastModCount = 0;
        
        internal static string[] spriteFileExtensions = { ".jpg", ".png", ".tif" };
        internal static string[] audioClipFileExtensions = { ".mp3", ".ogg", ".wav", ".aif", ".flac" };
        
        internal override void Load()
        {
            RefreshResources();
        }

        internal static void RefreshResources()
        {
            int currentModCount = API.ModLoader.LoadedMods.Count();
            if (currentModCount != lastModCount)
            {
                modAssetBundles = API.ModLoader.LoadedMods.SelectMany(mod => mod.AssetBundles).ToList();
                lastModCount = currentModCount;
            }
        }

        internal static Sprite LoadNewSprite(string filePath, float pixelsPerUnit = 100.0f)
        {
            // Load a PNG or JPG image from disk to a Texture2D, assign this texture to a new sprite and return its reference
            Texture2D spriteTexture = LoadTexture(filePath);
            if (spriteTexture == null) return null;

            spriteTexture.filterMode = FilterMode.Point;
            Sprite newSprite = Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);

            return newSprite;
        }

        internal static Sprite LoadNewSprite(string filePath, float pixelsPerUnit, Rect? rect, Vector2 pivot)
        {
            // Load a PNG or JPG image from disk to a Texture2D, assign this texture to a new sprite and return its reference
            Texture2D spriteTexture = LoadTexture(filePath);
            if (spriteTexture == null) return null;

            rect ??= new Rect(0, 0, spriteTexture.width, spriteTexture.height);

            spriteTexture.filterMode = FilterMode.Point;
            Sprite newSprite = Sprite.Create(spriteTexture, rect.Value, pivot, pixelsPerUnit);

            return newSprite;
        }

        internal static Texture2D LoadTexture(string filePath)
        {
            // Load a PNG or JPG file from disk to a Texture2D
            // Returns null if load fails

            if (File.Exists(filePath))
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                Texture2D tex2D = new Texture2D(2, 2);
                if (tex2D.LoadImage(fileData)) // Load the imagedata into the texture (size is set automatically)
                    return tex2D; // If data = readable -> return texture
            }

            return null; // Return null if load failed
        }

        #endregion
    }
    
}