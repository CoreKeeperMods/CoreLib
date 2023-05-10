using System.Text.Json;
using System.Text.Json.Nodes;
using Il2CppSystem.Collections.Generic;

namespace CoreLib.Submodules.JsonLoader.Readers
{
    [RegisterReader("modify")]
    public class ModificationJsonReader : IJsonReader
    {
        public void ApplyPre(JsonNode jObject)
        {
            string file = jObject["file"].GetValue<string>();
            string targetId = jObject["targetId"].GetValue<string>();
            JsonLoaderModule.entityModificationFiles.Add(new ModifyFile(file, JsonLoaderModule.context.loadPath, targetId));
        }

        public static void ModifyApply(JsonNode jObject, EntityMonoBehaviourData entity)
        {
            JsonLoaderModule.PopulateObject(entity.objectInfo, jObject, ItemJsonReader.excludedProperties);
            ItemJsonReader.ReadComponents(jObject, entity);
        }

        public void ApplyPost(JsonNode jObject)
        {
        }
    }
}