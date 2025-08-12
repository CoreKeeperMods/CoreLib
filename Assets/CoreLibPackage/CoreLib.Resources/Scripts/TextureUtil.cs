using PugMod;
using UnityEngine;

namespace CoreLib.Util
{
    /// <summary>
    /// Provides utility methods for loading and creating textures and sprites in Unity.
    /// </summary>
    public static class TextureUtil
    {
        /// Loads a new sprite from a specified file path, using a provided mod to access the file system.
        /// <param name="mod">The mod instance used to load the file from the disk.</param>
        /// <param name="filePath">The relative file path of the image to load, within the mod's directory.</param>
        /// <param name="pixelsPerUnit">The number of pixels per unit for the resulting sprite. Default is 100.0f.</param>
        /// <returns>A new Sprite created from the specified image file, or null if the image could not be loaded.</returns>
        internal static Sprite LoadNewSprite(LoadedMod mod, string filePath, float pixelsPerUnit = 100.0f)
        {
            // Load a PNG or JPG image from disk to a Texture2D, assign this texture to a new sprite and return its reference
            Texture2D spriteTexture = LoadTexture(mod, filePath);
            if (spriteTexture == null) return null;

            spriteTexture.filterMode = FilterMode.Point;
            Sprite newSprite = Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);

            return newSprite;
        }

        /// Loads a new sprite from a specified file path in the given mod context and returns it as a Sprite object.
        /// <param name="mod">The mod context providing access to the file system.</param>
        /// <param name="filePath">The relative path to the image file within the mod's directory.</param>
        /// <param name="pixelsPerUnit">The number of pixels that correspond to one unit in the Unity world. Defaults to 100.0f.</param>
        /// <param name="rect">An optional rectangular region of the texture to use when creating the sprite. Defaults to the full texture size if not provided.</param>
        /// <param name="pivot">The pivot point of the sprite, specified as a normalized value where (0,0) is the bottom-left and (1,1) is the top-right.</param>
        /// <returns>A Sprite object created from the specified texture, or null if loading the texture fails.</returns>
        internal static Sprite LoadNewSprite(LoadedMod mod, string filePath, float pixelsPerUnit, Rect? rect, Vector2 pivot)
        {
            // Load a PNG or JPG image from disk to a Texture2D, assign this texture to a new sprite and return its reference
            Texture2D spriteTexture = LoadTexture(mod, filePath);
            if (spriteTexture == null) return null;

            rect ??= new Rect(0, 0, spriteTexture.width, spriteTexture.height);

            spriteTexture.filterMode = FilterMode.Point;
            Sprite newSprite = Sprite.Create(spriteTexture, rect.Value, pivot, pixelsPerUnit);

            return newSprite;
        }

        /// Loads a PNG or JPG file from disk as a Texture2D object.
        /// <param name="mod">The loaded mod instance that contains the file.</param>
        /// <param name="filePath">The relative file path to the texture within the mod's file structure.</param>
        /// <returns>The loaded Texture2D object if successful; otherwise, null if the file is missing or the load operation fails.
        internal static Texture2D LoadTexture(LoadedMod mod, string filePath)
        {
            // Load a PNG or JPG file from disk to a Texture2D
            // Returns null if load fails

            byte[] fileData = mod.GetFile(filePath);
            if (fileData == null) return null;
            
            Texture2D tex2D = new Texture2D(2, 2);
            if (tex2D.LoadImage(fileData)) // Load the imagedata into the texture (size is set automatically)
                return tex2D; // If data = readable -> return texture
            return null;
        }
    }
}