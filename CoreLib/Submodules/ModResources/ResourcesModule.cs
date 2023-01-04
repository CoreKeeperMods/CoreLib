using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CoreLib.Submodules.JsonLoader;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreLib.Submodules.ModResources;

[CoreLibSubmodule]
public static class ResourcesModule
{

    #region Public Interface
    
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
        modResources.Add(resource);
    }

    public static Il2CppReferenceArray<Object> LoadSprites(string assetPath)
    {
        foreach (ResourceData resource in modResources)
        {
            foreach (string extension in spriteFileExtensions)
            {
                if (!resource.bundle.Contains(assetPath.WithExtension(extension))) continue;

                Il2CppReferenceArray<Object> sprites = resource.bundle.LoadAssetWithSubAssets(assetPath.WithExtension(extension), Il2CppType.Of<Sprite>());
                foreach (Object sprite in sprites)
                {
                    objectRetainer.Add(sprite);
                }

                CoreLibPlugin.Logger.LogDebug($"Loading registered assets {assetPath}, count {sprites?.Count.ToString()}: {(sprites != null ? "Success" : "Failure")}");

                return sprites;
            }
        }

        return new Il2CppReferenceArray<Object>(0);
    }

    public static Sprite[] OrderSprites(this Il2CppReferenceArray<Object> sprites)
    {
        List<Sprite> list = new List<Sprite>(sprites.Count);
        foreach (Object o in sprites)
        {
            list.Add(o.Cast<Sprite>());
        }

        return list.OrderBy(sprite =>
            {
                if (sprite.name.Contains('_'))
                {
                    string index = sprite.name.Split("_")[^1];
                    return int.Parse(index);
                }

                return 0;
            }).ToArray();
    }

    /// <summary>
    /// Load asset from mod asset bundles
    /// </summary>
    /// <param name="assetPath">path to the asset</param>
    public static Object LoadAsset(string assetPath)
    {
        foreach (ResourceData resource in modResources)
        {
            if (!assetPath.ToLower().Contains(resource.keyWord.ToLower()) || !resource.HasAssetBundle()) continue;

            if (resource.bundle.Contains(assetPath.WithExtension(".prefab")))
            {
                Object prefab = resource.bundle.LoadAsset<GameObject>(assetPath.WithExtension(".prefab"));
                objectRetainer.Add(prefab);
                CoreLibPlugin.Logger.LogDebug($"Loading registered asset {assetPath}: {(prefab != null ? "Success" : "Failure")}");
                return prefab;
            }
            
            if (resource.bundle.Contains(assetPath.WithExtension(".asset")))
            {
                Object prefab = resource.bundle.LoadAsset<ScriptableObject>(assetPath.WithExtension(".asset"));
                objectRetainer.Add(prefab);
                CoreLibPlugin.Logger.LogDebug($"Loading registered asset {assetPath}: {(prefab != null ? "Success" : "Failure")}");
                return prefab;
            }

            foreach (string extension in spriteFileExtensions)
            {
                if (!resource.bundle.Contains(assetPath.WithExtension(extension))) continue;

                Object sprite = resource.bundle.LoadAsset<Object>(assetPath.WithExtension(extension));
                objectRetainer.Add(sprite);

                CoreLibPlugin.Logger.LogDebug($"Loading registered asset {assetPath}: {(sprite != null ? "Success" : "Failure")}");

                return sprite;
            }

            foreach (string extension in audioClipFileExtensions)
            {
                if (!resource.bundle.Contains(assetPath.WithExtension(extension))) continue;

                Object audioClip = resource.bundle.LoadAsset<Object>(assetPath.WithExtension(extension));
                objectRetainer.Add(audioClip);
                CoreLibPlugin.Logger.LogDebug($"Loading registered asset {assetPath}: {(audioClip != null ? "Success" : "Failure")}");
                return audioClip;
            }
        }

        if (!JsonLoaderModule.context.Equals(""))
        {
            string fullPath = Path.Combine(JsonLoaderModule.context, assetPath).WithExtension(".png");
            Sprite sprite = LoadNewSprite(fullPath, 16);
            if (sprite != null)
            {
                objectRetainer.Add(sprite);
                CoreLibPlugin.Logger.LogDebug($"Loading asset {assetPath} from context");
                return sprite;
            }
        }

        CoreLibPlugin.Logger.LogWarning($"Failed to find asset '{assetPath}' in mod assets!");
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
        Object asset = LoadAsset(path);
        if (asset == null)
        {
            throw new ArgumentException($"Found no asset at path: {path}");
        }

        T typedAsset = asset.TryCast<T>();
        if (typedAsset == null)
        {
            throw new ArgumentException($"Asset at path: {path} can't be cast to {typeof(T).FullName}!");
        }
        
        return typedAsset;
    }

    public static void Retain(Il2CppSystem.Object o)
    {
        objectRetainer.Add(o);
    }
    
    #endregion

    #region PrivateImplementation

    private static bool _loaded;
    internal static List<ResourceData> modResources = new List<ResourceData>();
    internal static string[] spriteFileExtensions = { ".jpg", ".png", ".tif" };
    internal static string[] audioClipFileExtensions = { ".mp3", ".ogg", ".waw", ".aif", ".flac" };

    internal static ResourceData internalResource;
    internal static ObjectRetainer objectRetainer;

    [CoreLibSubmoduleInit(Stage = InitStage.Load)]
    internal static void Load()
    {
        objectRetainer = CoreLibPlugin.Instance.AddComponent<ObjectRetainer>();
        string pluginfolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        internalResource = new ResourceData(CoreLibPlugin.GUID, "CoreLib", pluginfolder);
        internalResource.LoadAssetBundle("corelibbundle");
        AddResource(internalResource);
    }
    
    
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
    
    internal static Sprite LoadNewSprite(string filePath, float pixelsPerUnit = 100.0f) {
        // Load a PNG or JPG image from disk to a Texture2D, assign this texture to a new sprite and return its reference
        Texture2D spriteTexture = LoadTexture(filePath);
        if (spriteTexture == null) return null;

        spriteTexture.filterMode = FilterMode.Point;
        Sprite newSprite = Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height),new Vector2(0.5f,0.5f), pixelsPerUnit);

        return newSprite;
    }
    
    internal static Sprite LoadNewSprite(string filePath, float pixelsPerUnit, Rect? rect, Vector2 pivot) {
        
        // Load a PNG or JPG image from disk to a Texture2D, assign this texture to a new sprite and return its reference
        Texture2D spriteTexture = LoadTexture(filePath);
        if (spriteTexture == null) return null;

        rect ??= new Rect(0, 0, spriteTexture.width, spriteTexture.height);

        spriteTexture.filterMode = FilterMode.Point;
        Sprite newSprite = Sprite.Create(spriteTexture, rect.Value, pivot, pixelsPerUnit);

        return newSprite;
    }
 
    internal static Texture2D LoadTexture(string filePath) {
 
        // Load a PNG or JPG file from disk to a Texture2D
        // Returns null if load fails

        if (File.Exists(filePath)){
            byte[] fileData = File.ReadAllBytes(filePath);
            Texture2D tex2D = new Texture2D(2, 2);
            if (tex2D.LoadImage(fileData))           // Load the imagedata into the texture (size is set automatically)
                return tex2D;                 // If data = readable -> return texture
        }  
        return null;                     // Return null if load failed
    }

    #endregion
}