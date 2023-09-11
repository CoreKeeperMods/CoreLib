using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace CoreLib.Submodules.JsonLoader.Converters
{
    public class GameObjectConverter : JsonConverter<GameObject>
    {
        public override GameObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return null;
        }

        public override void Write(Utf8JsonWriter writer, GameObject value, JsonSerializerOptions options)
        {
            writer.WriteNullValue();
        }
    }
}