using System.IO;
using UnityEngine;

namespace CoreLib.Scripts.Util
{
    public static class TextureUtil
    {
        internal static Sprite LoadNewSprite(string filePath, float pixelsPerUnit = 100.0f)
        {
            // Load a PNG or JPG image from disk to a Texture2D, assign this texture to a new sprite and return its reference
            Texture2D spriteTexture = LoadTexture(filePath);
            if (spriteTexture == null) return null;

            spriteTexture.filterMode = FilterMode.Point;
            Sprite newSprite = Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);

            return newSprite;
        }

        internal static Sprite LoadNewSprite(string filePath, float pixelsPerUnit, Rect? rect, Vector2 pivot)
        {
            // Load a PNG or JPG image from disk to a Texture2D, assign this texture to a new sprite and return its reference
            Texture2D spriteTexture = LoadTexture(filePath);
            if (spriteTexture == null) return null;

            rect ??= new Rect(0, 0, spriteTexture.width, spriteTexture.height);

            spriteTexture.filterMode = FilterMode.Point;
            Sprite newSprite = Sprite.Create(spriteTexture, rect.Value, pivot, pixelsPerUnit);

            return newSprite;
        }

        internal static Texture2D LoadTexture(string filePath)
        {
            // Load a PNG or JPG file from disk to a Texture2D
            // Returns null if load fails

            if (File.Exists(filePath))
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                Texture2D tex2D = new Texture2D(2, 2);
                if (tex2D.LoadImage(fileData)) // Load the imagedata into the texture (size is set automatically)
                    return tex2D; // If data = readable -> return texture
            }

            return null; // Return null if load failed
        }
    }
}