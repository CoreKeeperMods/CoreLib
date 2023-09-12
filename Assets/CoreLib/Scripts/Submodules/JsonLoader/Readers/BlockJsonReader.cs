﻿using System;
using System.Text.Json;
using CoreLib.Components;
using CoreLib.Submodules.ModEntity;
using CoreLib.Util.Extensions;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace CoreLib.Submodules.JsonLoader.Readers
{
    [RegisterReader("block")]
    public class BlockJsonReader : ItemJsonReader
    {
        public override void ApplyPre(JsonElement jObject, FileContext context)
        {
            string itemId = jObject.GetProperty("itemId").GetString();

            ObjectAuthoring objectAuthoring = EntityModule.LoadPrefab(itemId, "Assets/CoreLib/Objects/TemplateBlockItem");
            InventoryItemAuthoring itemAuthoring = objectAuthoring.GetComponent<InventoryItemAuthoring>();
            
            ReadObjectInfo(jObject, objectAuthoring, itemAuthoring);
            ReadComponents(jObject, objectAuthoring.gameObject);

            TemplateBlockCDAuthoring blockCdAuthoring = objectAuthoring.gameObject.GetComponent<TemplateBlockCDAuthoring>();
            if (blockCdAuthoring == null)
                throw new InvalidOperationException($"Missing required component '{typeof(TemplateBlockCDAuthoring).FullName}'");
            
            blockCdAuthoring.PostInit();
            var placeableAuthoring = objectAuthoring.GetComponent<PlaceableObjectAuthoring>();
            
            Vector2 colliderSize = placeableAuthoring.prefabTileSize;
            if (jObject.TryGetProperty("colliderSize", out var colliderSizeElement)) 
                colliderSize = colliderSizeElement.Deserialize<Vector2>(JsonLoaderModule.options);
            
            Vector2 colliderCenter = new Vector2(colliderSize.x / 2 - 0.5f, colliderSize.y / 2 - 0.5f);
            if (jObject.TryGetProperty("colliderCenter", out var colliderCenterElement))
                colliderCenter = colliderCenterElement.Deserialize<Vector2>(JsonLoaderModule.options);

            PhysicsShapeAuthoring physicsShapeAuthoring = objectAuthoring.GetComponent<PhysicsShapeAuthoring>();
         
            physicsShapeAuthoring.SetBox(new BoxGeometry()
            {
                Size = new float3(colliderSize.x, 1, colliderSize.y),
                Center = new float3(colliderCenter.x, 0.5f, colliderCenter.y),
                Orientation = quaternion.identity,
                BevelRadius = 0
            });
            
            ObjectID objectID = EntityModule.AddEntity(itemId, objectAuthoring);

            ReadLocalization(jObject, objectID);
        }
    }
}