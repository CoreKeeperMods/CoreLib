using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreLib.JsonLoader.Converters
{
    public class EntityMonoBehaviorDataConverter : JsonConverter<EntityMonoBehaviourData>
    {
        public override EntityMonoBehaviourData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return null;
        }

        public override void Write(Utf8JsonWriter writer, EntityMonoBehaviourData value, JsonSerializerOptions options)
        {
            writer.WriteNullValue();
        }
    }
}