// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: ModExtensions.cs
// Author: Minepatcher, Limoka
// Created: 2025-11-07
// Description: Provides extension methods for mod information retrieval, file access,
//              and Burst assembly handling across different platforms within the CoreLib environment.
// ========================================================

using System;
using System.IO;
using System.Linq;
using System.Text;
using PugMod;
using Unity.Burst;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace CoreLib.Util.Extension
{
    /// <summary>
    /// Contains utility extension methods for mod management, retrieval, and file operations.
    /// </summary>
    /// <remarks>
    /// This class simplifies working with <see cref="LoadedMod"/> and <see cref="IMod"/> instances,
    /// providing easy access to mod metadata, file content, and Burst assembly loading
    /// for runtime optimization.
    /// </remarks>
    /// <seealso cref="LoadedMod"/>
    /// <seealso cref="IMod"/>
    /// <seealso cref="BurstRuntime"/>
    public static class ModExtensions
    {
        #region Fields

        /// <summary>
        /// Stores a randomly generated unique identifier used for temporary file paths or isolated directories.
        /// </summary>
        public static readonly string RandomPath = Guid.NewGuid().ToString();

        #endregion

        #region Mod Information Retrieval

        /// <summary>
        /// Retrieves the <see cref="LoadedMod"/> metadata associated with a specific mod instance.
        /// </summary>
        /// <param name="mod">The mod instance to retrieve information for.</param>
        /// <param name="modName">An optional parameter specifying the mod's name. Defaults to <c>null</c>.</param>
        public static LoadedMod GetModInfo(this IMod mod, string modName = null)
        {
            var loadedMods = API.ModLoader.LoadedMods;
            return modName != null
                ? loadedMods.FirstOrDefault(loadedMod =>
                    loadedMod.Metadata.name.Equals(modName, StringComparison.OrdinalIgnoreCase))
                : loadedMods.FirstOrDefault(modInfo => modInfo.Handlers.Contains(mod));
        }
        
        /// <summary>
        /// Retrieves an asset from a specific mod by name.
        /// </summary>
        /// <param name="mod">The mod instance to retrieve the asset from.</param>
        /// <param name="modName">The name of the mod whose asset should be retrieved. Defaults to <c>null</c>.</param>
        /// <param name="assetName">The name of the asset to retrieve. Defaults to <c>null</c>.</param>
        /// <typeparam name="T">The expected type of the asset.</typeparam>
        /// <exception cref="ArgumentException">Thrown when either the specified mod or its assets cannot be located.</exception>
        public static T LoadAsset<T>(this IMod mod, string modName = null, string assetName = null) where T : Object
        {
            if(assetName == null) throw new ArgumentNullException($"Asset Name cannot be null!");
            var useMod = mod.GetModInfo(modName);
            return useMod == null ? throw new ArgumentException($"Mod {modName} not found!") : useMod.LoadAsset<T>(assetName);
        }
        
        /// <summary>
        /// Retrieves an asset from a specific mod by name.
        /// </summary>
        /// <param name="mod">The mod instance to retrieve the asset from.</param>
        /// <param name="assetName">The name of the asset to retrieve. Defaults to <c>null</c>.</param>
        /// <typeparam name="T">The expected type of the asset.</typeparam>
        /// <exception cref="ArgumentException">Thrown when the specified asset cannot be located.</exception>
        public static T LoadAsset<T>(this LoadedMod mod, string assetName) where T : Object
        {
            if(assetName == null) throw new ArgumentNullException($"Asset Name cannot be null!");
            var asset = mod.Assets.Find(x =>  x.name == assetName) as T;
            return asset ?? throw new ArgumentException($"Found no asset in mod {mod.Metadata.name} with name {assetName}!");
        }

        /// <summary>
        /// Loads all sprite assets from the specified mod within the registered assets.
        /// </summary>
        /// <param name="mod">The mod instance to retrieve the asset from.</param>
        /// <returns>An array of loaded sprite objects, or an empty array if no sprites are found.</returns>
        public static Sprite[] LoadSprites(this LoadedMod mod)
        {
            var sprites = mod.Assets.OfType<Sprite>().ToArray();
            return sprites.Length > 0 ? sprites : Array.Empty<Sprite>();
        }
        
        /// <summary>
        /// Loads all audio clip assets from the specified asset path within the registered assets.
        /// </summary>
        /// <param name="mod">The mod instance to retrieve the asset from.</param>
        /// <returns>An array of loaded audio clip objects, or an empty array if no audio clips are found.</returns>
        public static AudioClip[] LoadAudioFiles(this LoadedMod mod)
        {
            var audioClips = mod.Assets.OfType<AudioClip>().ToArray();
            return audioClips.Length > 0 ? audioClips : Array.Empty<AudioClip>();
        }

        #endregion

        #region File Handling

        /// <summary>
        /// Retrieves and decodes the contents of a file stored within a mod.
        /// </summary>
        /// <param name="mod">The mod from which to load the file.</param>
        /// <param name="file">The relative path of the file within the mod’s directory.</param>
        /// <returns>
        /// The decoded UTF-8 string contents of the requested file.
        /// </returns>
        /// <remarks>
        /// This extension wraps <see cref="LoadedMod.GetFile(string)"/> and automatically decodes the resulting bytes.
        /// </remarks>
        /// <example>
        /// <code>
        /// var text = myMod.GetAllText("config/settings.json");
        /// Debug.Log(text);
        /// </code>
        /// </example>
        /// <seealso cref="LoadedMod.GetFile(string)"/>
        public static string GetAllText(this LoadedMod mod, string file)
        {
            byte[] fileData = mod.GetFile(file);
            return Encoding.UTF8.GetString(fileData);
        }

        #endregion

        #region Platform Helpers

        /// <summary>
        /// Returns the current platform name string based on the running environment.
        /// </summary>
        /// <returns>
        /// A string indicating the platform type, such as <c>"Windows"</c> or <c>"Linux"</c>;
        /// returns <c>null</c> if the platform is unrecognized.
        /// </returns>
        /// <seealso cref="Application.platform"/>
        public static string GetPlatformString()
        {
            return Application.platform switch
            {
                RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsServer => "Windows",
                RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxServer => "Linux",
                _ => null
            };
        }

        /// <summary>
        /// Returns the appropriate file extension for the given platform.
        /// </summary>
        /// <param name="platform">The name of the platform, e.g., <c>"Windows"</c> or <c>"Linux"</c>.</param>
        /// <returns>
        /// The file extension string (e.g., <c>"dll"</c> for Windows or <c>"so"</c> for Linux),
        /// or an empty string if the platform is unrecognized.
        /// </returns>
        public static string GetPlatformExtension(string platform)
        {
            return platform switch
            {
                "Windows" => "dll",
                "Linux" => "so",
                _ => string.Empty
            };
        }

        #endregion

        #region Burst Assembly Handling

        /// <summary>
        /// Attempts to load the platform-specific Burst-compiled assembly for the given mod.
        /// </summary>
        /// <param name="modInfo">The <see cref="LoadedMod"/> containing metadata and assembly references.</param>
        /// <remarks>
        /// This method ensures that platform-optimized Burst binaries are copied into a temporary directory
        /// and dynamically loaded at runtime for enhanced performance.
        /// If the loading process fails, a warning is logged to the CoreLib logger.
        /// </remarks>
        /// <example>
        /// <code>
        /// var mod = API.ModLoader.LoadedMods.First();
        /// mod.TryLoadBurstAssembly();
        /// </code>
        /// </example>
        /// <seealso cref="BurstRuntime.LoadAdditionalLibrary(string)"/>
        /// <seealso cref="Application.temporaryCachePath"/>
        public static void TryLoadBurstAssembly(this LoadedMod modInfo)
        {
            string platform = GetPlatformString();
            if (platform == null)
                return;

            string directory = API.ModLoader.GetDirectory(modInfo.ModId);
            string extension = GetPlatformExtension(platform);
            string id = modInfo.Metadata.name;

            string modLoaderDir = Path.Combine(Application.temporaryCachePath, "ModLoader");

            if (Application.dataPath.ToLower().Contains("dedicated"))
                modLoaderDir = Path.Combine(modLoaderDir, "DedicatedServer", RandomPath);

            string tempDirectory = Path.Combine(modLoaderDir, id);
            string assemblyName = $"{id}_burst_generated_{platform}.{extension}";
            string newAssemblyPath = Path.Combine(tempDirectory, assemblyName);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(newAssemblyPath)!);
                File.WriteAllBytes(newAssemblyPath, File.ReadAllBytes(Path.Combine(directory, assemblyName)));
            }
            catch (Exception ex)
            {
                CoreLibMod.Log.LogError($"Exception copying Burst assembly for mod '{id}': {ex.Message}");
            }

            bool success = BurstRuntime.LoadAdditionalLibrary(newAssemblyPath);
            if (!success)
                CoreLibMod.Log.LogWarning($"Failed to load Burst assembly for mod '{id}'.");
        }

        #endregion

        #region Reflection Utilities

        /// <summary>
        /// Searches a type for a member by name using CoreLib’s safe reflection utilities.
        /// </summary>
        /// <param name="type">The target <see cref="Type"/> to search for the member.</param>
        /// <param name="memberName">The name of the member to locate.</param>
        /// <returns>
        /// A <see cref="MemberInfo"/> instance representing the member if found; otherwise, <c>null</c>.
        /// </returns>
        /// <seealso cref="Type.GetMembers()"/>
        /// <seealso cref="MemberInfo"/>
        public static MemberInfo FindMember(this Type type, string memberName)
        {
            return type.GetMembersChecked()
                       .FirstOrDefault(info => info.GetNameChecked().Equals(memberName, StringComparison.Ordinal));
        }

        #endregion
    }
}