using System.Linq;
using System.Text;
using PugMod;
using Unity.Burst;
using UnityEngine;

namespace CoreLib.Util.Extensions
{
    public static class ModExtensions
    {
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
            if (platform != null)
            {
                string directory = API.ModLoader.GetDirectory(modInfo.ModId);
                string fileExtension = GetPlatformExtension(platform);
                string ID = modInfo.Metadata.name;
                bool success = BurstRuntime.LoadAdditionalLibrary($"{directory}/{ID}_burst_generated_{platform}.{fileExtension}");
                if (!success)
                    CoreLibMod.Log.LogWarning($"Failed to load burst assembly for mod {ID}");
            }
        }
    }
}