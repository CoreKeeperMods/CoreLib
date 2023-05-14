using System;
using CoreLib.Submodules.ModComponent;
using CoreLib.Submodules.ModSystem;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace CoreLib.Submodules.MigrationModule
{
    public class IDMigrationSystem : MonoBehaviour, IPseudoServerSystem
    {
        internal World serverWorld;
        private bool actionHasBeenPerformed = false;
    
        public IDMigrationSystem(IntPtr ptr) : base(ptr) { }

        // This will be called once the world is ready
        public void OnServerStarted(World world)
        {
            serverWorld = world;
            
            if (actionHasBeenPerformed) return;
            
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            EntityPass(ecb);
            InventoryPass(ecb);

            ecb.Playback(serverWorld.EntityManager);
            ecb.Dispose();
            actionHasBeenPerformed = true;
            CoreLibPlugin.Logger.LogInfo("Missing object fixup has been performed!");
        }

        // At this point the world is about to be destroyed, remove reference
        public void OnServerStopped()
        {
            serverWorld = null;
        }

        private void EntityPass(EntityCommandBuffer ecb)
        {
            EntityQuery query = serverWorld.EntityManager.CreateEntityQuery(new EntityQueryDesc()
            {
                All = new[] { ComponentModule.ReadOnly<ObjectDataCD>() },
                Options = EntityQueryOptions.IncludeDisabled
            });
            NativeArray<Entity> result = query.ToEntityArray(Allocator.Temp);

            foreach (Entity entity in result)
            {
                ObjectDataCD objectDataCd = serverWorld.EntityManager.GetComponentData<ObjectDataCD>(entity);
                if (!PugDatabase.HasObject(objectDataCd.objectID))
                {
                    CoreLibPlugin.Logger.LogInfo($"Found missing object with ID: {objectDataCd.objectID}, removing!");
                    ecb.DestroyEntity(entity);
                }
            }

            result.Dispose();
        }
        
        private void InventoryPass(EntityCommandBuffer ecb)
        {
            EntityQuery query = serverWorld.EntityManager.CreateEntityQuery(new EntityQueryDesc()
            {
                All = new[] { ComponentModule.ReadOnly<InventoryCD>() },
                Options = EntityQueryOptions.IncludeDisabled
            });
            NativeArray<Entity> result = query.ToEntityArray(Allocator.Temp);

            foreach (Entity entity in result)
            {
                ModDynamicBuffer<ContainedObjectsBuffer> objectsBuffer = serverWorld.EntityManager.GetModBuffer<ContainedObjectsBuffer>(entity);

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

            result.Dispose();
        }
        
    }
}