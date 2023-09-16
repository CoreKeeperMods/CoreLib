using System.Collections.Generic;
using System.Text.Json;
using CoreLib.Submodules.ModEntity;
using CoreLib.Util.Extensions;
using UnityEngine;

namespace CoreLib.Submodules.JsonLoader.Readers
{
    [RegisterReader("modWorkbench")]
    public class ModWorkbenchJsonReader : IJsonReader
    {
        public static readonly string[] excludedProperties =
        {
            "requiredObjectsToCraft"
        };
        
        public void ApplyPre(JsonElement jObject, FileContext context)
        {
            WorkbenchDefinition workbenchDefinition = ScriptableObject.CreateInstance<WorkbenchDefinition>();
            JsonLoaderModule.PopulateObject(workbenchDefinition, jObject, excludedProperties);
            JsonLoaderModule.FillArrays(workbenchDefinition);

            ObjectID objectID = EntityModule.AddModWorkbench(workbenchDefinition);
            
            ItemJsonReader.ReadLocalization(jObject, objectID);
        }

        public void ApplyPost(JsonElement jObject, FileContext context)
        {
            ItemJsonReader.ReadRecipes(jObject);
            string itemId = jObject.GetProperty("itemId").GetString();
            ObjectID objectID = EntityModule.GetObjectId(itemId);

            List<ObjectID> canCraft = jObject.GetProperty("canCraft").Deserialize<List<ObjectID>>(JsonLoaderModule.options);
            foreach (ObjectID recipe in canCraft)
            {
                EntityModule.AddWorkbenchItem(objectID, recipe);
            }
        }
    }
}