﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreLib.Submodules.CustomEntity;

namespace CoreLib.Submodules.JsonLoader.Converters
{
    public class ObjectTypeConverter : JsonConverter<ObjectType> 
    {
        public override ObjectType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                int objectIndex = reader.GetInt32();
                return (ObjectType)objectIndex;
            }
            if (reader.TokenType == JsonTokenType.String)
            {
                string value = reader.GetString();
                if (Enum.TryParse(value, true, out ObjectType objectID))
                {
                    return objectID;
                }

                return CustomEntityModule.GetObjectType(value);
            }

            return ObjectType.NonUsable;
        }

        public override void Write(Utf8JsonWriter writer, ObjectType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}