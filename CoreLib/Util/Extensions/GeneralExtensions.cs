using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace CoreLib.Util.Extensions;

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
            var child = o.Cast<Transform>();
            transformList.Add(child);
            child.GetAllChildren(transformList);
        }
        return transformList;
    }
    
}