using System;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using PugMod;
using Unity.Burst;
using UnityEngine;

namespace CoreLib.Util.Extensions
{
    public static class ModExtensions
    {
        public static string randomPath = Guid.NewGuid().ToString();
        
        public static LoadedMod GetModInfo(this IMod mod)
        {
            return API.ModLoader.LoadedMods.FirstOrDefault(modInfo => modInfo.Handlers.Contains(mod));
        }

        public static LoadedMod GetModInfo(string modId)
        {
            return API.ModLoader.LoadedMods.FirstOrDefault(loadedMod => loadedMod.Metadata.name.Equals(modId));
        }

        public static string GetAllText(this LoadedMod mod, string file)
        {
            var fileData = mod.GetFile(file);
            return Encoding.UTF8.GetString(fileData);
        }
        
        public static string GetPlatformString()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsServer:
                    return "Windows";
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxServer:
                    return "Linux";
            }

            return null;
        }

        public static string GetPlatformExtension(string platform)
        {
            if (platform == "Windows")
                return "dll";
            if (platform == "Linux")
                return "so";
            return "";
        }

        public static void TryLoadBurstAssembly(this LoadedMod modInfo)
        {
            var platform = GetPlatformString();
            if (platform == null) return;
            
            string directory = API.ModLoader.GetDirectory(modInfo.ModId);
            string fileExtension = GetPlatformExtension(platform);
            string ID = modInfo.Metadata.name;

            var productName = Application.dataPath;
            var modLoaderDir = Path.Combine(Application.temporaryCachePath, "ModLoader");

            if (productName.ToLower().Contains("dedicated"))
            {
                modLoaderDir = Path.Combine(modLoaderDir, "DedicatedServer", randomPath);
            }
            
            // need elevated access
            var tempDirectory = Path.Combine(modLoaderDir, ID);
            var assemblyName = $"{ID}_burst_generated_{platform}.{fileExtension}";
                
            string newAssemblyPath = Path.Combine(tempDirectory, assemblyName);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(newAssemblyPath));
                File.WriteAllBytes(newAssemblyPath, File.ReadAllBytes(Path.Combine(directory, assemblyName)));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
                
            bool success = BurstRuntime.LoadAdditionalLibrary(newAssemblyPath);
            if (!success)
                CoreLibMod.Log.LogWarning($"Failed to load burst assembly for mod {ID}");
        }
    }
}