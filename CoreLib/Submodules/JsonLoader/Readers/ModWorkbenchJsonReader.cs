using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using CoreLib.Submodules.ModEntity;
using CoreLib.Submodules.ModResources;
using UnityEngine;

namespace CoreLib.Submodules.JsonLoader.Readers
{
    [RegisterReader("modWorkbench")]
    public class ModWorkbenchJsonReader : IJsonReader
    {
        public void ApplyPre(JsonNode jObject)
        {
            string itemId = jObject["itemId"].GetValue<string>();
            Sprite icon = jObject["icon"].Deserialize<Sprite>(JsonLoaderModule.options);
            Sprite smallIcon = jObject["smallIcon"].Deserialize<Sprite>(JsonLoaderModule.options);
            Texture2D variantsTexture = jObject["variations"].Deserialize<Texture2D>(JsonLoaderModule.options);
            List<CraftingData> recipe = jObject["requiredObjectsToCraft"].Deserialize<List<CraftingData>>(JsonLoaderModule.options);

            ObjectID objectID = EntityModule.AddModWorkbench(itemId, icon, smallIcon, variantsTexture, recipe, true);
            
            ItemJsonReader.ReadLocalization(jObject, objectID);
            
        }

        public void ApplyPost(JsonNode jObject)
        {
            string itemId = jObject["itemId"].GetValue<string>();
            ObjectID objectID = EntityModule.GetObjectId(itemId);

            List<ObjectID> canCraft = jObject["canCraft"].Deserialize<List<ObjectID>>(JsonLoaderModule.options);
            foreach (ObjectID recipe in canCraft)
            {
                EntityModule.AddWorkbenchItem(objectID, recipe);
            }
        }
    }
}