using System.Text.Json.Nodes;
using CoreLib.Submodules.CustomEntity;
using CoreLib.Util;
using UnityEngine;

namespace CoreLib.Submodules.JsonLoader.Readers
{
    [RegisterReader("block")]
    public class BlockJsonReader : ItemJsonReader
    {
        public override void ApplyPre(JsonNode jObject)
        {
            string itemId = jObject["itemId"].GetValue<string>();

            EntityMonoBehaviourData entityData = CustomEntityModule.LoadPrefab(itemId, "Assets/CoreLib/Objects/TemplateBlockItem");

            ReadObjectInfo(jObject, entityData);
            ReadComponents(jObject, entityData);

            MonoBehaviourUtils.CallAlloc(entityData);
            ObjectID objectID = CustomEntityModule.AddEntityWithVariations(itemId, new System.Collections.Generic.List<EntityMonoBehaviourData> { entityData });

            ReadLocalization(jObject, objectID);
        }
    }
}