using System;
using CoreLib.Submodules.ModComponent;
using CoreLib.Submodules.ModSystem;
using CoreLib.Submodules.ModSystem.Jobs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace CoreLib.Submodules.MigrationModule
{
    public struct IDMigrationJob : JobExtensions.IModJob
    {
        public NativeArray<Entity> objectsEntities;
        public NativeArray<Entity> inventoryEntities;
        public ModComponentDataFromEntity<ObjectDataCD> objectDataFromEntity;
        public ModBufferFromEntity<ContainedObjectsBuffer> containedObjectsFromEntity;

        public void Execute()
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            EntityPass(ecb);
            InventoryPass(ecb);

            ecb.Playback(Manager.ecs.ServerWorld.EntityManager);
            ecb.Dispose();
        }
        
        private void EntityPass(EntityCommandBuffer ecb)
        {
            foreach (Entity entity in objectsEntities)
            {
                ObjectDataCD objectDataCd = objectDataFromEntity[entity];
                if (!PugDatabase.HasObject(objectDataCd.objectID))
                {
                    CoreLibPlugin.Logger.LogInfo($"Found missing object with ID: {objectDataCd.objectID}, removing!");
                    ecb.DestroyEntity(entity);
                }
            }

            objectsEntities.Dispose();
        }
        
        private void InventoryPass(EntityCommandBuffer ecb)
        {
            foreach (Entity entity in inventoryEntities)
            {
                ModDynamicBuffer<ContainedObjectsBuffer> objectsBuffer = containedObjectsFromEntity[entity];

                for (int i = 0; i < objectsBuffer.Count; i++)
                {
                    ContainedObjectsBuffer item = objectsBuffer[i];
                    if (item.objectID == ObjectID.None) continue;
                    
                    if (!PugDatabase.HasObject(item.objectID))
                    {
                        CoreLibPlugin.Logger.LogInfo($"Found missing object with ID: {item.objectID} in inventory, removing!");
           
                        item.objectData.objectID = ObjectID.None;
                        item.objectData.amount = 0;
                        item.objectData.variation = 0;
                        item.auxDataIndex = 0;
                        objectsBuffer[i] = item;
                    }
                }
            }

            inventoryEntities.Dispose();
        }
        
    }
}