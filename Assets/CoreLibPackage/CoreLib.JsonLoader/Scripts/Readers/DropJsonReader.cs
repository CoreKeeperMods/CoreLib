using System;
using System.Text.Json;
using CoreLib.Drops;

namespace CoreLib.JsonLoader.Readers
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
                    return DropTablesModule.AddLootTable(lootTableId);
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