using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreLib.Submodules.DropTables;
using CoreLib.Submodules.ModEntity;

namespace CoreLib.Submodules.JsonLoader.Converters
{
    public class LootTableIDConverter : JsonConverter<LootTableID>
    {
        public override LootTableID Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                int objectIndex = reader.GetInt32();
                return (LootTableID)objectIndex;
            }
            if (reader.TokenType == JsonTokenType.String)
            {
                string value = reader.GetString();
                if (Enum.TryParse(value, true, out LootTableID objectID))
                {
                    return objectID;
                }

                return DropTablesModule.GetLootTableID(value);
            }

            return LootTableID.Empty;
            
        }

        public override void Write(Utf8JsonWriter writer, LootTableID value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}