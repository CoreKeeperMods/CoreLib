using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using CoreLib.Submodules.DropTables;

namespace CoreLib.Submodules.JsonLoader.Readers
{
    [RegisterReader("drop")]
    public class DropJsonReader : IJsonReader
    {
        public void ApplyPre(JsonNode jObject)
        {
        }

        private static LootTableID GetOrCreateDropTable(JsonNode jObject)
        {
            string lootTableId = jObject["lootTableId"].GetValue<string>();
            if (!Enum.TryParse(lootTableId, true, out LootTableID tableID))
            {
                if (!DropTablesModule.HasLootTableID(lootTableId))
                {
                    string areaLevelStr = jObject["areaLevel"].GetValue<string>();
                    bool result = Enum.TryParse(areaLevelStr, true, out AreaLevel areaLevel);
                    if (!result)
                        throw new ArgumentException($"'{areaLevelStr}' is not a valid AreaLevel!");

                    return DropTablesModule.AddLootTable(lootTableId, areaLevel);
                }

                return DropTablesModule.GetLootTableID(lootTableId);
            }

            return tableID;
        }

        public void ApplyPost(JsonNode jObject)
        {
            LootTableID tableID = GetOrCreateDropTable(jObject);

            if (jObject["add"] != null)
            {
                DropTableInfo[] addDrops = jObject["add"].Deserialize<DropTableInfo[]>(JsonLoaderModule.options);
                foreach (DropTableInfo drop in addDrops)
                    DropTablesModule.AddNewDrop(tableID, drop);
            }

            if (jObject["edit"] != null)
            {
                DropTableInfo[] editDrops = jObject["edit"].Deserialize<DropTableInfo[]>(JsonLoaderModule.options);
                foreach (DropTableInfo drop in editDrops)
                    DropTablesModule.EditDrop(tableID, drop);
            }

            if (jObject["remove"] != null)
            {
                ObjectID[] removeDrops = jObject["remove"].Deserialize<ObjectID[]>(JsonLoaderModule.options);
                foreach (ObjectID drop in removeDrops)
                    DropTablesModule.RemoveDrop(tableID, drop);
            }
        }
    }
}