using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Resource
{
    /// <summary>
    /// Provides extension methods for working with resources, including string manipulation,
    /// sprite ordering, and creating Addressable Asset references within the Addressables system.
    /// </summary>
    public static class ResourcesExtension
    {
        /// Appends the specified extension to the given path if the path does not already end with that extension.
        /// <param name="path">The file path to which the extension may be added.</param>
        /// <param name="extension">The extension to append to the path if not already present.</param>
        /// <returns>The updated path including the specified extension, or the original path if it already ends with the extension.</returns>
        internal static string WithExtension(this string path, string extension)
        {
            if (path.EndsWith(extension))
            {
                return path;
            }

            return path + extension;
        }

        /// Orders an array of sprites based on their names, expecting each sprite name to include
        /// an underscore ('_') followed by an integer index. The method sorts sprites numerically
        /// by this index. If no valid index is found in the sprite name, the sprite is treated as having an index of 0.
        /// <param name="sprites">An array of Unity objects that are expected to be sprites.</param>
        /// <returns>An array of sprites, sorted by their numerical index derived from their names.</returns>
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

        /// <summary>
        /// Converts the given path to an Addressable Asset Reference of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the asset to be referenced, which must derive from UnityEngine.Object.</typeparam>
        /// <param name="path">The resource path to be converted into an addressable asset reference.</param>
        /// <returns>An AssetReferenceT instance constructed using the provided resource path and the mod protocol.</returns>
        public static AssetReferenceT<T> AsAddress<T>(this string path) where T : Object
        {
            return new AssetReferenceT<T>(ModResourceLocator.PROTOCOL + path);
        }

        /// <summary>
        /// Converts a given path string to an <see cref="AssetReferenceGameObject"/> formatted path.
        /// </summary>
        /// <param name="path">The input path string representing the resource location.</param>
        /// <returns>An <see cref="AssetReferenceGameObject"/> instance constructed with the input path.</returns>
        public static AssetReferenceGameObject AsAddressGameObject(this string path)
        {
            return new AssetReferenceGameObject(ModResourceLocator.PROTOCOL + path);
        }
    }
}