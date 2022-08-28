using System;
using System.Security.Cryptography;
using System.Text;

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
    
}