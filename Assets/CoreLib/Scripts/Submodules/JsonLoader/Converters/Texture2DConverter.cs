using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreLib.Util;
using UnityEngine;

namespace CoreLib.Submodules.JsonLoader.Converters
{
    public class Texture2DConverter : JsonConverter<Texture2D>
    {
        public override Texture2D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string spritePath = reader.GetString();
                string fullPath = Path.Combine(JsonLoaderModule.context.loadPath, spritePath);
                Texture2D sprite = TextureUtil.LoadTexture(fullPath);
                if (sprite == null)
                {
                    throw new JsonException($"Failed to load texture file at {fullPath}!");
                }

                return sprite;
            }

            throw new JsonException("Texture object must be a string!");
        }

        public override void Write(Utf8JsonWriter writer, Texture2D texture, JsonSerializerOptions options)
        {
            if (!string.IsNullOrEmpty(SpriteConverter.outputPath))
            {
                texture = SpriteConverter.GetReadableTexture(texture);
                byte[] bytes = texture.EncodeToPNG();
                string filePath = Path.Combine(SpriteConverter.outputPath, $"{texture.name}.png");
                File.WriteAllBytes(filePath, bytes);

                writer.WriteStringValue(filePath);
                return;
            }

            writer.WriteStringValue("N/A");
        }
    }
}