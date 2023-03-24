using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using CoreLib.Submodules.ModEntity;

namespace CoreLib.Submodules.JsonLoader.Readers
{
    [RegisterReader("bundle")]
    public class BundleJsonLoader : IJsonReader
    {
        public void ApplyPre(JsonNode jObject)
        {
            string itemId = jObject["itemId"].GetValue<string>();
            List<string> paths = new List<string>();
            if (jObject["prefab"] != null)
            {
                paths.Add(jObject["prefab"].GetValue<string>());
            }else if (jObject["prefabs"] != null)
            {
                paths.AddRange(jObject["prefabs"].Deserialize<List<string>>(JsonLoaderModule.options));
            }

            ObjectID objectID = EntityModule.AddEntityWithVariations(itemId, paths.ToArray());
            ItemJsonReader.ReadLocalization(jObject, objectID);
        }

        public void ApplyPost(JsonNode jObject)
        {
            ItemJsonReader.ReadRecipes(jObject);
        }
    }
}