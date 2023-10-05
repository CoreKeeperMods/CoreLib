using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreLib.Util.Extensions;

namespace CoreLib.JsonLoader.Converters
{
    public class CraftingObjectConverter : JsonConverter<InventoryItemAuthoring.CraftingObject>
    {
        public override InventoryItemAuthoring.CraftingObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                int objectIndex = reader.GetInt32();
                return new InventoryItemAuthoring.CraftingObject()
                {
                    objectName = ((ObjectID)objectIndex).ToString(),
                    amount = 1
                };
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                return new InventoryItemAuthoring.CraftingObject()
                {
                    objectName = reader.GetString(),
                    amount = 1
                };
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                JsonElement element = JsonDocument.ParseValue(ref reader).RootElement;
                return new InventoryItemAuthoring.CraftingObject()
                {
                    objectName = element.GetProperty("objectID").GetString(),
                    amount = element.GetProperty("amount").GetInt32()
                };
            }

            throw new InvalidOperationException("Failed to deserialize CraftingObject. Unsupported token found!");
        }

        public override void Write(Utf8JsonWriter writer, InventoryItemAuthoring.CraftingObject value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString("objectID", value.objectName);
            writer.WriteNumber("amount", value.amount);
            
            writer.WriteEndObject();
        }
    }
}