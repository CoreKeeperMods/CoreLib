using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreLib.JsonLoader.Converters
{
    public class EntityMonoBehaviorConverter : JsonConverter<EntityMonoBehaviour>
    {
        public override EntityMonoBehaviour Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return null;
        }

        public override void Write(Utf8JsonWriter writer, EntityMonoBehaviour value, JsonSerializerOptions options)
        {
            writer.WriteNullValue();
        }
    }
}