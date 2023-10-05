using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace CoreLib.JsonLoader.Converters
{
    public class RectConverter : JsonConverter<Rect>
    {
        public override Rect Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                JsonElement element = JsonDocument.ParseValue(ref reader).RootElement;
                return new Rect(
                    element.GetProperty("x").GetSingle(),
                    element.GetProperty("y").GetSingle(),
                    element.GetProperty("width").GetSingle(),
                    element.GetProperty("height").GetSingle()
                );
            }

            throw new InvalidOperationException("Rect must be an object!");
        }

        public override void Write(Utf8JsonWriter writer, Rect value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            
            writer.WriteNumber("x", value.x);
            writer.WriteNumber("y", value.y);
            writer.WriteNumber("width", value.width);
            writer.WriteNumber("height", value.height);
            
            writer.WriteEndObject();
        }
    }
}