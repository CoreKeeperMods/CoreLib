using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreLib.Submodules.CustomEntity;

namespace CoreLib.Submodules.JsonLoader.Converters
{
    public class ObjectIDConverter : JsonConverter<ObjectID>
    {
        public override ObjectID Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                int objectIndex = reader.GetInt32();
                return (ObjectID)objectIndex;
            }
            if (reader.TokenType == JsonTokenType.String)
            {
                string value = reader.GetString();
                if (Enum.TryParse(value, true, out ObjectID objectID))
                {
                    return objectID;
                }

                return CustomEntityModule.GetObjectId(value);
            }

            return ObjectID.None;
        }

        public override void Write(Utf8JsonWriter writer, ObjectID value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}