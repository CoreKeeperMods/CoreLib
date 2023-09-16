using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CoreLib.Submodules.ModEntity;
using PugMod;
using Unity.Mathematics;
using UnityEngine;

namespace CoreLib.Util.Extensions
{
    public static class GeneralExtensions
    {
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
          
            foreach (var o in parent) {
                var child = (Transform)o;
                transformList.Add(child);
                child.GetAllChildren(transformList);
            }
            return transformList;
        }
    
        public static string GetRelativePath(this string relativeTo, string path)
        {
            var uri = new Uri(relativeTo);
            var rel = Uri.UnescapeDataString(uri.MakeRelativeUri(new Uri(path)).ToString()).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (rel.Contains(Path.DirectorySeparatorChar.ToString()) == false)
            {
                rel = $".{ Path.DirectorySeparatorChar }{ rel }";
            }
            return rel;
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

            return EntityModule.GetObjectId(value);
        }
        
        public static LoadedMod GetModInfo(this IMod mod)
        {
            return API.ModLoader.LoadedMods.FirstOrDefault(modInfo => modInfo.Handlers.Contains(mod));
        }
    }
}