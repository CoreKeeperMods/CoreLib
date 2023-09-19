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

            EntityModule.AddEntityWithVariations(itemId, paths.ToArray());

            if (EntityModule.GetMainEntity(itemId, out ObjectAuthoring entity))
            {
                ItemJsonReader.ReadLocalization(jObject, entity.gameObject, itemId);
            }
        }

        public void ApplyPost(JsonElement jObject, FileContext context)
        {
            ItemJsonReader.ReadRecipes(jObject);
        }
    }
}