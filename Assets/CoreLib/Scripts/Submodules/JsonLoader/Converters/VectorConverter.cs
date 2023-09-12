using System;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unity.Mathematics;
using UnityEngine;

namespace CoreLib.Submodules.JsonLoader.Converters
{
    public class VectorConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(Vector2) ||
                   typeToConvert == typeof(Vector3) ||
                   typeToConvert == typeof(Vector2Int) ||
                   typeToConvert == typeof(Vector3Int) ||
                   typeToConvert == typeof(float2) ||
                   typeToConvert == typeof(float3) ||
                   typeToConvert == typeof(int2) ||
                   typeToConvert == typeof(int3);
        }

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(VectorConverterInner<>).MakeGenericType(type),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: new object[] { options },
                culture: null)!;

            return converter;
        }

        private class VectorConverterInner<TValue> : JsonConverter<TValue>
        {
            private readonly Type _valueType;

            private readonly string[] nameList = { "x", "y", "z" };
            private readonly string[] altNameList = { "m_X", "m_Y", "m_Z" };

            public VectorConverterInner(JsonSerializerOptions options)
            {
                // Cache the key and value types.
                _valueType = typeof(TValue);
            }
            
            public override TValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    object inst = Activator.CreateInstance<TValue>();
                    ParseFields(ref reader, inst, (ref Utf8JsonReader jsonReader, string key) => jsonReader.GetSingle());
                    return (TValue)inst;
                }

                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    JsonElement element = JsonDocument.ParseValue(ref reader).RootElement;
                    
                    object inst = Activator.CreateInstance<TValue>();
                    ParseFields(ref reader, inst, (ref Utf8JsonReader jsonReader, string key) => element.GetProperty(key).GetSingle());
                    return (TValue)inst;
                }
                throw new InvalidOperationException($"Vector converter can't convert  starting from token {reader.TokenType}!");
            }

            delegate float ReadFunc(ref Utf8JsonReader reader, string key);
            
            private void ParseFields(ref Utf8JsonReader reader, object inst, ReadFunc readFunc)
            {
                int index = 0;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        return;
                    }

                    var fieldInfo = _valueType.GetField(nameList[index]);
                    if (fieldInfo == null)
                        fieldInfo = _valueType.GetField(altNameList[index]);

                    if (fieldInfo != null)
                    {
                        float value = readFunc(ref reader, nameList[index]);

                        if (fieldInfo.FieldType == typeof(float))
                            fieldInfo.SetValue(inst, value);
                        else
                            fieldInfo.SetValue(inst, Mathf.RoundToInt(value));
                    }

                    index++;
                }
            }

            public override void Write(Utf8JsonWriter writer, TValue value, JsonSerializerOptions options)
            {
                writer.WriteStartArray();
                FieldInfo[] fields = _valueType.GetFields(BindingFlags.Instance | BindingFlags.Public);

                foreach (FieldInfo field in fields)
                {
                    int index = Array.IndexOf(nameList, field.Name);
                    if (index == -1)
                        index = Array.IndexOf(altNameList, field.Name);

                    if (index >= 0)
                    {
                        if (field.FieldType == typeof(float))
                        {
                            float val =  (float)field.GetValue(value);
                            WriteNumberSafe(writer, val);
                        }
                        else
                        {
                            int val =  (int)field.GetValue(value);
                            writer.WriteNumberValue(val);
                        }
                    }
                }
                
                writer.WriteEndArray();
            }

            private static void WriteNumberSafe(Utf8JsonWriter writer, float value)
            {
                if (!float.IsInfinity(value) &&
                    !float.IsNaN(value))
                {
                    writer.WriteNumberValue(value);
                }
                else
                {
                    writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
                }
            }
        }
    }
}