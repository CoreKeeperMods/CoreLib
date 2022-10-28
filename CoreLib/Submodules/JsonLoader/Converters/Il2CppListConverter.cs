using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Il2CppSystem.Collections.Generic;

namespace CoreLib.Submodules.JsonLoader.Converters
{
    public class Il2CppListConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType)
            {
                return false;
            }

            return typeToConvert.GetGenericTypeDefinition() == typeof(List<>);
        }

        public override JsonConverter CreateConverter(
            Type type,
            JsonSerializerOptions options)
        {
            Type valueType = type.GetGenericArguments()[0];

            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(Il2CppListConverterInner<>).MakeGenericType(valueType),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: new object[] { options },
                culture: null)!;

            return converter;
        }

        private class Il2CppListConverterInner<TValue> :
            JsonConverter<List<TValue>>
        {
            private readonly JsonConverter<TValue> _valueConverter;
            private readonly Type _valueType;

            public Il2CppListConverterInner(JsonSerializerOptions options)
            {
                // For performance, use the existing converter.
                _valueConverter = (JsonConverter<TValue>)options
                    .GetConverter(typeof(TValue));

                // Cache the key and value types.
                _valueType = typeof(TValue);
            }

            public override List<TValue> Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new JsonException();
                }

                var list = new List<TValue>();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        return list;
                    }

                    // Get the value.
                    TValue value = _valueConverter.Read(ref reader, _valueType, options)!;

                    // Add to dictionary.
                    list.Add(value);
                }

                throw new JsonException();
            }

            public override void Write(
                Utf8JsonWriter writer,
                List<TValue> list,
                JsonSerializerOptions options)
            {
                writer.WriteStartArray();

                foreach (TValue value in list)
                {
                    _valueConverter.Write(writer, value, options);
                }

                writer.WriteEndArray();
            }
        }
    }
}