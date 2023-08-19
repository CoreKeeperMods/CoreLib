using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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
    
    }
}