using System;
using System.Text.Json;
using CoreLib.Submodules.DropTables;
using CoreLib.Util.Extensions;

namespace CoreLib.Submodules.JsonLoader.Readers
{
    [RegisterReader("drop")]
    public class DropJsonReader : IJsonReader
    {
        public void ApplyPre(JsonElement jObject, FileReference context)
        {
        }

        private static LootTableID GetOrCreateDropTable(JsonElement jObject)
        {
            string lootTableId = jObject.GetProperty("lootTableId").GetString();
            if (!Enum.TryParse(lootTableId, true, out LootTableID tableID))
            {
                if (!DropTablesModule.HasLootTableID(lootTableId))
                {
                    string areaLevelStr = jObject.GetProperty("areaLevel").GetString();
                    bool result = Enum.TryParse(areaLevelStr, true, out AreaLevel areaLevel);
                    if (!result)
                        throw new ArgumentException($"'{areaLevelStr}' is not a valid AreaLevel!");

                    return DropTablesModule.AddLootTable(lootTableId, areaLevel);
                }

                return DropTablesModule.GetLootTableID(lootTableId);
            }

            return tableID;
        }

        public void ApplyPost(JsonElement jObject, FileReference context)
        {
            LootTableID tableID = GetOrCreateDropTable(jObject);

            if (jObject.TryGetProperty("add", out var addElement))
            {
                DropTableInfo[] addDrops = addElement.Deserialize<DropTableInfo[]>(JsonLoaderModule.options);
                foreach (DropTableInfo drop in addDrops)
                    DropTablesModule.AddNewDrop(tableID, drop);
            }

            if (jObject.TryGetProperty("edit", out var editElement))
            {
                DropTableInfo[] editDrops = editElement.Deserialize<DropTableInfo[]>(JsonLoaderModule.options);
                foreach (DropTableInfo drop in editDrops)
                    DropTablesModule.EditDrop(tableID, drop);
            }

            if (jObject.TryGetProperty("remove", out var removeElement) )
            {
                ObjectID[] removeDrops = removeElement.Deserialize<ObjectID[]>(JsonLoaderModule.options);
                foreach (ObjectID drop in removeDrops)
                    DropTablesModule.RemoveDrop(tableID, drop);
            }
        }
    }
}