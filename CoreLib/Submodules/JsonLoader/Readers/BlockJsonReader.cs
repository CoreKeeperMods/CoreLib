﻿using System.Text.Json;
using System.Text.Json.Nodes;
using CoreLib.Submodules.CustomEntity;
using CoreLib.Util;
using Unity.Mathematics;
using Unity.Physics.Authoring;
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
            MonoBehaviourUtils.CallAlloc(entityData);
            
            ReadObjectInfo(jObject, entityData);
            ReadComponents(jObject, entityData);

            Vector2 colliderSize = entityData.objectInfo.prefabTileSize;
            if (jObject["colliderSize"] !=  null) 
                colliderSize = jObject["colliderSize"].Deserialize<Vector2>(JsonLoaderModule.options);
            
            Vector2 colliderCenter = new Vector2(colliderSize.x / 2 - 0.5f, colliderSize.y / 2 - 0.5f);
            if (jObject["colliderCenter"] != null)
                colliderCenter = jObject["colliderCenter"].Deserialize<Vector2>(JsonLoaderModule.options);

            PhysicsShapeAuthoring physicsShapeAuthoring = entityData.GetComponent<PhysicsShapeAuthoring>();
            physicsShapeAuthoring.m_PrimitiveSize = new float3(colliderSize.x, 1, colliderSize.y);
            physicsShapeAuthoring.m_PrimitiveCenter = new float3(colliderCenter.x, 0.5f, colliderCenter.y);

            MonoBehaviourUtils.CallAlloc(entityData);
            ObjectID objectID = CustomEntityModule.AddEntityWithVariations(itemId, new System.Collections.Generic.List<EntityMonoBehaviourData> { entityData });

            ReadLocalization(jObject, objectID);
        }
    }
}