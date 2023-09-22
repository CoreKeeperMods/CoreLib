using System;
using System.IO;

namespace CoreLib.Util.Extensions
{
    public static class ModPath
    {
        public static string Combine(params string[] paths)
        {
            return Path.Combine(paths);
        }
        
        public static string GetRelativePath(this string relativeTo, string path)
        {
            var uri = new Uri(relativeTo);
            var rel = Uri.UnescapeDataString(uri.MakeRelativeUri(new Uri(path)).ToString())
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (rel.Contains(Path.DirectorySeparatorChar.ToString()) == false)
            {
                rel = $".{Path.DirectorySeparatorChar}{rel}";
            }

            return rel;
        }
    }
}