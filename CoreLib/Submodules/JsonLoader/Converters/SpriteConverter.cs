using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreLib.Submodules.ModResources;
using UnityEngine;

namespace CoreLib.Submodules.JsonLoader.Converters
{
    public class SpriteConverter : JsonConverter<Sprite>
    {
        public override Sprite Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string spritePath = reader.GetString();
                string fullPath = Path.Combine(JsonLoaderModule.context, spritePath);
                Sprite sprite = ResourcesModule.LoadNewSprite(fullPath, 16);
                if (sprite == null)
                {
                    throw new JsonException($"Failed to load sprite file at {fullPath}!");
                }

                ResourcesModule.Retain(sprite);
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

                string fullPath = Path.Combine(JsonLoaderModule.context, path);
                Sprite sprite = ResourcesModule.LoadNewSprite(fullPath, 16, rect, new Vector2(0.5f,0.5f));
                
                if (sprite == null)
                {
                    throw new JsonException($"Failed to load sprite file at {fullPath}!");
                }
                ResourcesModule.Retain(sprite);
                return sprite;
            }

            throw new JsonException("Failed to parse tokens!");
        }

        public override void Write(
            Utf8JsonWriter writer,
            Sprite dateTimeValue,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue("N/A");
        }
    }
}