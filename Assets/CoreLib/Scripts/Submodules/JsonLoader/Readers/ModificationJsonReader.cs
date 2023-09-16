using System.Text.Json;
using UnityEngine;

namespace CoreLib.Submodules.JsonLoader.Readers
{
    [RegisterReader("modify")]
    public class ModificationJsonReader : IJsonReader
    {
        public void ApplyPre(JsonElement jObject, FileContext context)
        {
            string targetId = jObject.GetProperty("targetId").GetString();

            JsonLoaderModule.entityModificationFiles.Add(new ModifyFile(context.file, JsonLoaderModule.context.loadPath, targetId));
        }

        public static void ModifyApply(JsonElement jObject, MonoBehaviour entity)
        {
            var entityMonoBehaviorData = entity.GetComponent<EntityMonoBehaviourData>();
            var objectAuthoring = entity.GetComponent<ObjectAuthoring>();
            var inventoryItemAuthoring = entity.GetComponent<InventoryItemAuthoring>();

            if (entityMonoBehaviorData != null)
                JsonLoaderModule.PopulateObject(entityMonoBehaviorData.objectInfo, jObject, ItemJsonReader.restrictedProperties);

            if (objectAuthoring != null)
                JsonLoaderModule.PopulateObject(objectAuthoring, jObject, ItemJsonReader.restrictedProperties);

            if (inventoryItemAuthoring != null)
                JsonLoaderModule.PopulateObject(inventoryItemAuthoring, jObject, ItemJsonReader.restrictedProperties);

            ItemJsonReader.ReadComponents(jObject, entity.gameObject);
        }

        public void ApplyPost(JsonElement jObject, FileContext context) { }
    }
}