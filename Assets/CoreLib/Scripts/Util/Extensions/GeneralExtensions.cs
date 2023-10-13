using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PugMod;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace CoreLib.Util.Extensions
{
    public static class GeneralExtensions
    {
        private static readonly char[] separators =
        {
            '/',
            '\\'
        };

        public static string GetGUID(this string objectId)
        {
            using MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(objectId));
            Guid result = new Guid(hash);
            return result.ToString("N");
        }

        public static List<Transform> GetAllChildren(this Transform parent, List<Transform> transformList = null)
        {
            if (transformList == null) transformList = new List<Transform>();

            foreach (var o in parent)
            {
                var child = (Transform)o;
                transformList.Add(child);
                child.GetAllChildren(transformList);
            }

            return transformList;
        }

        public static void CopyFrom(this ref FixedArray64 fixedArray, byte[] bytes, int startIndex)
        {
            var size = math.min(fixedArray.Size, bytes.Length - startIndex);
            byte[] dataBytes = new byte[size];
            Array.Copy(bytes, startIndex, dataBytes, 0, size);
            fixedArray.Set(dataBytes);
        }

        public static void CopyTo(this ref FixedArray64 fixedArray, byte[] bytes, int startIndex)
        {
            var size = math.min(fixedArray.Size, bytes.Length - startIndex);
            var dataBytes = fixedArray.ToArray<byte>(size);
            Array.Copy(dataBytes, 0, bytes, startIndex, size);
        }

        public static ObjectID GetObjectID(this string value)
        {
            if (Enum.TryParse(value, true, out ObjectID objectID))
            {
                return objectID;
            }

            return API.Authoring.GetObjectID(value);
        }

        public static LoadedMod GetModInfo(this IMod mod)
        {
            return API.ModLoader.LoadedMods.FirstOrDefault(modInfo => modInfo.Handlers.Contains(mod));
        }

        public static string GetAllText(this LoadedMod mod, string file)
        {
            var fileData = mod.GetFile(file);
            return Encoding.UTF8.GetString(fileData);
        }

        public static string GetFileName(this string path)
        {
            return path.Substring(path.LastIndexOfAny(separators) + 1);
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