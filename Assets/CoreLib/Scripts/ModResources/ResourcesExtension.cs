using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoreLib.ModResources
{
    internal static class ResourcesExtension
    {
        internal static string WithExtension(this string path, string extension)
        {
            if (path.EndsWith(extension))
            {
                return path;
            }

            return path + extension;
        }
        
        internal static Sprite[] OrderSprites(this Object[] sprites)
        {
            List<Sprite> list = sprites.Select(o => (Sprite)o).ToList();

            return list.OrderBy(sprite =>
            {
                if (sprite.name.Contains('_'))
                {
                    string index = sprite.name.Split('_').Last();
                    return int.Parse(index);
                }

                return 0;
            }).ToArray();
        }
    }
}