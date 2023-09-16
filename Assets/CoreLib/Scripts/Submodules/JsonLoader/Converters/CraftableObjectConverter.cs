using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreLib.Util.Extensions;

namespace CoreLib.Submodules.JsonLoader.Converters
{
    public class CraftableObjectConverter : JsonConverter<CraftingAuthoring.CraftableObject>
    {
        public override CraftingAuthoring.CraftableObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                int objectIndex = reader.GetInt32();
                return new CraftingAuthoring.CraftableObject()
                {
                    objectID = (ObjectID)objectIndex,
                    amount = 1,
                    craftingConsumesEntityAmount = false
                };
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                string value = reader.GetString();
                return new CraftingAuthoring.CraftableObject()
                {
                    objectID = value.GetObjectID(),
                    amount = 1,
                    craftingConsumesEntityAmount = false
                };
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                JsonElement element = JsonDocument.ParseValue(ref reader).RootElement;
                return new CraftingAuthoring.CraftableObject()
                {
                    objectID = element.GetProperty("objectID").Deserialize<ObjectID>(),
                    amount = element.GetProperty("amount").GetInt32(),
                    craftingConsumesEntityAmount = element.GetProperty("craftingConsumesEntityAmount").GetBoolean(),
                    entityAmountToConsume = element.GetProperty("entityAmountToConsume").GetInt32()
                };
            }

            return new CraftingAuthoring.CraftableObject()
            {
                objectID = ObjectID.None,
                amount = 1,
                craftingConsumesEntityAmount = false
            };
        }

        public override void Write(Utf8JsonWriter writer, CraftingAuthoring.CraftableObject value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString("objectID", value.objectID.ToString());
            writer.WriteNumber("amount", value.amount);
            writer.WriteBoolean("craftingConsumesEntityAmount", value.craftingConsumesEntityAmount);
            writer.WriteNumber("entityAmountToConsume", value.entityAmountToConsume);
            
            writer.WriteEndObject();
        }
    }
}