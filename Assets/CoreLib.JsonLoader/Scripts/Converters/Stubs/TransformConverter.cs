using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace CoreLib.JsonLoader.Converters
{
    public class TransformConverter : JsonConverter<Transform>
    {
        public override Transform Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return null;
        }

        public override void Write(Utf8JsonWriter writer, Transform value, JsonSerializerOptions options)
        {
            writer.WriteNullValue();
        }
    }
}