using System;
using System.IO;
using System.Linq;
using System.Text;
using PugMod;
using Unity.Burst;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Util.Extensions
{
    /// <summary>
    /// Contains extension methods for working with mod management, retrieval, and related functionalities.
    /// This static class provides utility extensions for handling mods, accessing their information, and
    /// performing common operations inside a loaded mod framework.
    /// </summary>
    public static class ModExtensions
    {
        /// <summary>
        /// Stores a randomly generated unique identifier as a string using Guid.NewGuid().
        /// This variable is commonly used to generate non-conflicting paths or temporary identifiers
        /// in scenarios where a unique value is required, especially for temporary directory or file operations.
        /// </summary>
        public static string RandomPath = Guid.NewGuid().ToString();

        /// <summary>
        /// Retrieves the loaded mod information associated with the specified mod instance.
        /// </summary>
        /// <param name="mod">The mod instance to retrieve information for.</param>
        /// <returns>The information of the loaded mod if found, or null if no matching mod is found.</returns>
        public static LoadedMod GetModInfo(this IMod mod)
        {
            return API.ModLoader.LoadedMods.FirstOrDefault(modInfo => modInfo.Handlers.Contains(mod));
        }

        /// <summary>
        /// Retrieves information about a loaded mod associated with the given mod instance.
        /// </summary>
        /// <param name="modId">The mod name to be loaded</param>
        /// <returns>The loaded mod information if found, or null if no match is found.</returns>
        public static LoadedMod GetModInfo(string modId)
        {
            return API.ModLoader.LoadedMods.FirstOrDefault(loadedMod => loadedMod.Metadata.name.Equals(modId));
        }

        /// <summary>
        /// Retrieves the content of a specified file within the given mod, decoding it as a UTF-8 string.
        /// </summary>
        /// <param name="mod">The mod containing the file to be read.</param>
        /// <param name="file">The relative path of the file inside the mod.</param>
        /// <returns>The content of the file as a UTF-8 decoded string.</returns>
        public static string GetAllText(this LoadedMod mod, string file)
        {
            byte[] fileData = mod.GetFile(file);
            return Encoding.UTF8.GetString(fileData);
        }

        /// <summary>
        /// Determines the platform string based on the current runtime platform.
        /// </summary>
        /// <returns>
        /// A string representing the platform ("Windows" or "Linux") based on the runtime platform,
        /// or null if the platform is not recognized.
        /// </returns>
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
        /// Retrieves the appropriate file extension for a given platform, which helps identify the correct
        /// binaries or libraries to use on that platform.
        /// </summary>
        /// <param name="platform">The name of the platform (e.g., "Windows", "Linux").</param>
        /// <returns>The file extension associated with the platform, or an empty string if the platform is not recognized.</returns>
        public static string GetPlatformExtension(string platform)
        {
            return platform switch
            {
                "Windows" => "dll",
                "Linux" => "so",
                _ => ""
            };
        }

        /// <summary>
        /// Tries to load the Burst assembly for the provided mod by copying it to a temporary directory
        /// and loading it using Unity's Burst runtime. This helps in resolving platform-specific assemblies
        /// required for optimized performance.
        /// </summary>
        /// <param name="modInfo">The mod information containing metadata and identifiers for the mod.</param>
        public static void TryLoadBurstAssembly(this LoadedMod modInfo)
        {
            string platform = GetPlatformString();
            if (platform == null) return;
            
            string directory = API.ModLoader.GetDirectory(modInfo.ModId);
            string fileExtension = GetPlatformExtension(platform);
            string id = modInfo.Metadata.name;

            string productName = Application.dataPath;
            string modLoaderDir = Path.Combine(Application.temporaryCachePath, "ModLoader");

            if (productName.ToLower().Contains("dedicated"))
            {
                modLoaderDir = Path.Combine(modLoaderDir, "DedicatedServer", RandomPath);
            }
            
            // need elevated access
            string tempDirectory = Path.Combine(modLoaderDir, id);
            string assemblyName = $"{id}_burst_generated_{platform}.{fileExtension}";
                
            string newAssemblyPath = Path.Combine(tempDirectory, assemblyName);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(newAssemblyPath)!);
                File.WriteAllBytes(newAssemblyPath, File.ReadAllBytes(Path.Combine(directory, assemblyName)));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
                
            bool success = BurstRuntime.LoadAdditionalLibrary(newAssemblyPath);
            if (!success)
                CoreLibMod.Log.LogWarning($"Failed to load burst assembly for mod {id}");
        }
        
        public static MemberInfo FindMember(this Type type, string memberName)
        {
            return type.GetMembersChecked().FirstOrDefault(info => info.GetNameChecked().Equals(memberName));
        }
    }
}