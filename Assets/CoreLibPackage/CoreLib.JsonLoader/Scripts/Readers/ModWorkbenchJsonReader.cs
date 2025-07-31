using System.Text.Json;
using CoreLib.Submodules.ModEntity;
using UnityEngine;

namespace CoreLib.JsonLoader.Readers
{
    [RegisterReader("modWorkbench")]
    public class ModWorkbenchJsonReader : IJsonReader
    {
        public void ApplyPre(JsonElement jObject, FileReference context)
        {
            WorkbenchDefinition workbenchDefinition = ScriptableObject.CreateInstance<WorkbenchDefinition>();
            JsonLoaderModule.PopulateObject(workbenchDefinition, jObject);
            JsonLoaderModule.FillArrays(workbenchDefinition);

            EntityModule.AddModWorkbench(workbenchDefinition);
            
            if (EntityModule.GetMainEntity(workbenchDefinition.itemId, out ObjectAuthoring entity))
            {
                ItemJsonReader.ReadLocalization(jObject, entity.gameObject, workbenchDefinition.itemId);
            }
        }

        public void ApplyPost(JsonElement jObject, FileReference context)
        {
            ItemJsonReader.ReadRecipes(jObject);
        }
    }
}