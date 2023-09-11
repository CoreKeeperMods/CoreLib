using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreLib.Scripts.Util;
using CoreLib.Util.Extensions;
using UnityEngine;

namespace CoreLib.Submodules.JsonLoader.Converters
{
    public class SpriteConverter : JsonConverter<Sprite>
    {
        public static string outputPath;

        public override Sprite Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string spritePath = reader.GetString();
                string fullPath = Path.Combine(JsonLoaderModule.context.loadPath, spritePath);
                Sprite sprite = TextureUtil.LoadNewSprite(fullPath, 16);
                if (sprite == null)
                {
                    throw new JsonException($"Failed to load sprite file at {fullPath}!");
                }

                return sprite;
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                JsonElement element = JsonDocument.ParseValue(ref reader).RootElement;
                string path = element.GetProperty("path").GetString();
                Rect? rect = null;

                if (element.TryGetProperty("type", out JsonElement typeElement))
                {
                    string typeString = typeElement.GetString();
                    switch (typeString)
                    {
                        case "icon-top":
                            rect = new Rect(0, 16, 16, 16);
                            break;
                        case "icon-bottom":
                            rect = new Rect(0, 0, 16, 16);
                            break;
                    }
                }
                else if (element.TryGetProperty("rect", out JsonElement rectElement))
                {
                    rect = rectElement.Deserialize<Rect>();
                }

                string fullPath = Path.Combine(JsonLoaderModule.context.loadPath, path);
                Sprite sprite = TextureUtil.LoadNewSprite(fullPath, 16, rect, new Vector2(0.5f, 0.5f));

                if (sprite == null)
                {
                    throw new JsonException($"Failed to load sprite file at {fullPath}!");
                }

                return sprite;
            }

            throw new JsonException("Failed to parse tokens!");
        }

        public override void Write(
            Utf8JsonWriter writer,
            Sprite sprite,
            JsonSerializerOptions options)
        {
            if (!string.IsNullOrEmpty(outputPath))
            {
                Texture2D texture = sprite.texture;
                if (texture != null)
                {
                    texture = GetReadableTexture(texture);
                    byte[] bytes = texture.EncodeToPNG();
                    string filePath = Path.Combine(outputPath, $"{texture.name}.png");
                    File.WriteAllBytes(filePath, bytes);
                    writer.WriteStartObject();

                    writer.WriteString("path", filePath);
                    Rect rect = sprite.rect;

                    if (rect.x == 0 &&
                        rect.y == 16 &&
                        rect.width == 16 &&
                        rect.height == 16)
                    {
                        writer.WriteString("type", "icon-top");
                    }
                    else if (rect.x == 0 &&
                             rect.y == 0 &&
                             rect.width == 16 &&
                             rect.height == 16)
                    {
                        writer.WriteString("type", "icon-bottom");
                    }
                    else
                    {
                        writer.WritePropertyName("rect");
                        JsonConverter<Rect> converter = (JsonConverter<Rect>)options.GetConverter(typeof(Rect));
                        converter.Write(writer, rect, options);
                    }

                    writer.WriteEndObject();

                    return;
                }
            }

            writer.WriteStringValue("N/A");
        }

        public static Texture2D GetReadableTexture(Texture2D texture)
        {
            if (texture.isReadable) return texture;

            // Create a temporary RenderTexture of the same size as the texture
            RenderTexture tmp = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);
            
            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(texture, tmp);
            
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;
            
            Texture2D myTexture2D = new Texture2D(texture.width, texture.height);
            
            // Copy the pixels from the RenderTexture to the new Texture
            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            myTexture2D.Apply();
            
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);
            myTexture2D.name = texture.name;
            return myTexture2D;
        }
    }
}