using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreLib.Util;
using CoreLib.Util.Extensions;
using UnityEngine;

namespace CoreLib.JsonLoader.Converters
{
    public class Texture2DConverter : JsonConverter<Texture2D>
    {
        public override Texture2D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string spritePath = reader.GetString();
                string fullPath = $"{JsonLoaderModule.context.loadPath}/{spritePath}";
                Texture2D sprite = TextureUtil.LoadTexture(JsonLoaderModule.context.mod, fullPath);
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
                string filePath = $"{SpriteConverter.outputPath}/{texture.name}.png";
                JsonLoaderModule.fileAccess.WriteAllBytes(filePath, bytes);

                writer.WriteStringValue(filePath);
                return;
            }

            writer.WriteStringValue("N/A");
        }
    }
}