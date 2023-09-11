using System.Collections.Generic;
using System.Text.Json;
using CoreLib.Submodules.ModEntity;
using CoreLib.Util.Extensions;

namespace CoreLib.Submodules.JsonLoader.Readers
{
    [RegisterReader("bundle")]
    public class BundleJsonLoader : IJsonReader
    {
        public void ApplyPre(JsonElement jObject, FileContext context)
        {
            string itemId = jObject.GetProperty("itemId").GetString();
            List<string> paths = new List<string>();
            if (jObject.TryGetProperty("prefab", out var prefabElement))
            {
                paths.Add(prefabElement.GetString());
            }else if (jObject.TryGetProperty("prefabs", out var prefabsElement))
            {
                paths.AddRange(prefabsElement.Deserialize<List<string>>(JsonLoaderModule.options));
            }

            ObjectID objectID = EntityModule.AddEntityWithVariations(itemId, paths.ToArray());
            ItemJsonReader.ReadLocalization(jObject, objectID);
        }

        public void ApplyPost(JsonElement jObject, FileContext context)
        {
            ItemJsonReader.ReadRecipes(jObject);
        }
    }
}