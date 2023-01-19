using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CoreLib.Submodules.JsonLoader;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.Networking;
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

                CoreLibPlugin.Logger.LogDebug(
                    $"Loading registered assets {assetPath}, count {sprites?.Count.ToString()}: {(sprites != null ? "Success" : "Failure")}");

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
            string fullPath = Path.Combine(JsonLoaderModule.context, assetPath);
            
            if (File.Exists(fullPath.WithExtension(".png")))
            {
                fullPath = fullPath.WithExtension(".png");
                Sprite sprite = LoadNewSprite(fullPath, 16);
                if (sprite != null)
                {
                    objectRetainer.Add(sprite);
                    CoreLibPlugin.Logger.LogDebug($"Loading asset {assetPath} from context");
                    return sprite;
                }
            }else if (File.Exists(fullPath.WithExtension(".wav")))
            {
                fullPath = fullPath.WithExtension(".wav");
                AudioClip clip = LoadNewAudioClip(fullPath);
                if (clip != null)
                {
                    objectRetainer.Add(clip);
                    CoreLibPlugin.Logger.LogDebug($"Loading asset {assetPath} from context");
                    return clip;
                }
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
    internal static string[] audioClipFileExtensions = { ".mp3", ".ogg", ".wav", ".aif", ".flac" };

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

    internal static AudioClip LoadNewAudioClip(string path)
    {
        try
        {
            OpenWav(path, out float[] left, out float[] right);

            string filename = Path.GetFileName(path);
            filename = filename[..^4];

            AudioClip clip = AudioClip.Create(filename, left.Length, 1, 44100, false);
            clip.SetData(left, 0);
            
            return clip;
        }
        catch (Exception e)
        {
            CoreLibPlugin.Logger.LogError($"Failed to load AudioClip at {path}:\n{e}");
            return null;
        }
    }
    
    // convert two bytes to one double in the range -1 to 1
    internal static double bytesToDouble(byte firstByte, byte secondByte) {
        // convert two bytes to one short (little endian)
        short s = (short)((secondByte << 8) | firstByte);
        // convert to range from -1 to (just below) 1
        return s / 32768.0;
    }

// Returns left and right double arrays. 'right' will be null if sound is mono.
    internal static void OpenWav(string filename, out float[] left, out float[] right)
    {
        byte[] wav = File.ReadAllBytes(filename);

        // Determine if mono or stereo
        int channels = wav[22];     // Forget byte 23 as 99.999% of WAVs are 1 or 2 channels

        // Get past all the other sub chunks to get to the data subchunk:
        int pos = 12;   // First Subchunk ID from 12 to 16

        // Keep iterating until we find the data chunk (i.e. 64 61 74 61 ...... (i.e. 100 97 116 97 in decimal))
        while(!(wav[pos]==100 && wav[pos+1]==97 && wav[pos+2]==116 && wav[pos+3]==97)) {
            pos += 4;
            int chunkSize = wav[pos] + wav[pos + 1] * 256 + wav[pos + 2] * 65536 + wav[pos + 3] * 16777216;
            pos += 4 + chunkSize;
        }
        pos += 8;

        // Pos is now positioned to start of actual sound data.
        int samples = (wav.Length - pos)/2;     // 2 bytes per sample (16 bit sound mono)
        if (channels == 2) samples /= 2;        // 4 bytes per sample (16 bit stereo)

        // Allocate memory (right will be null if only mono sound)
        left = new float[samples];
        if (channels == 2) right = new float[samples];
        else right = null;

        // Write to double array/s:
        int i=0;
        while (pos < wav.Length) {
            left[i] = (float)bytesToDouble(wav[pos], wav[pos + 1]);
            pos += 2;
            if (channels == 2) {
                right[i] = (float)bytesToDouble(wav[pos], wav[pos + 1]);
                pos += 2;
            }
            i++;
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