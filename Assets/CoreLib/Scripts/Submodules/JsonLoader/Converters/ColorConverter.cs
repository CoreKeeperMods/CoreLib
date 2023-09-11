using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace CoreLib.Submodules.JsonLoader.Converters
{
    public class ColorConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string data = reader.GetString();

                try
                {
                    if (ColorUtility.TryParseHtmlString(data, out Color color))
                    {
                        return color;
                    }
                }
                catch (Exception e)
                {
                    CoreLibMod.Log.LogWarning($"Failed to parse color data '{data}':\n{e}");
                }
            }

            var converter = (JsonConverter<Color>)options.GetConverter(typeof(Color));
            return converter.Read(ref reader, typeToConvert, options);
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            string htmlStr;

            if (value.a == 1)
                htmlStr = ColorUtility.ToHtmlStringRGB(value);
            else
                htmlStr = ColorUtility.ToHtmlStringRGBA(value);

            writer.WriteStringValue($"#{htmlStr}");
        }
    }
}