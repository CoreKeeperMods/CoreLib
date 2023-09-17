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
        public void ApplyPre(JsonElement jObject, FileContext context)
        {
            WorkbenchDefinition workbenchDefinition = ScriptableObject.CreateInstance<WorkbenchDefinition>();
            JsonLoaderModule.PopulateObject(workbenchDefinition, jObject);
            JsonLoaderModule.FillArrays(workbenchDefinition);

            ObjectID objectID = EntityModule.AddModWorkbench(workbenchDefinition);
            
            ItemJsonReader.ReadLocalization(jObject, objectID);
        }

        public void ApplyPost(JsonElement jObject, FileContext context)
        {
            ItemJsonReader.ReadRecipes(jObject);
            string itemId = jObject.GetProperty("itemId").GetString();
            ObjectID objectID = EntityModule.GetObjectId(itemId);

            List<CraftingAuthoring.CraftableObject> canCraft = jObject.GetProperty("canCraft").Deserialize<List<CraftingAuthoring.CraftableObject>>(JsonLoaderModule.options);
            
            if (EntityModule.GetMainEntity(objectID, out var entity))
            {
                CraftingAuthoring craftingCdAuthoring = entity.gameObject.GetComponent<CraftingAuthoring>();
                
                foreach (CraftingAuthoring.CraftableObject recipe in canCraft)
                {
                    if (craftingCdAuthoring.canCraftObjects.Count < 18)
                    {
                        craftingCdAuthoring.canCraftObjects.Add(recipe);
                        return;
                    }
                }
            }
        }
    }
}