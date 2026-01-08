using System;
using System.Threading.Tasks;
using PugMod;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpawnNamedPet
{
    public class SpawnNamedPet : IMod
    {
        public ContainedObjectsBuffer NamedPetObject;

        public Entity NewEntity = new();
        
        public bool NeedsUpdate;
        
        public const string Name = "Spawn Named Pet";

        public void EarlyInit() { }

        public void Init() { }

        public void Shutdown() { }

        public void ModObjectLoaded(Object obj) { }

        public void Update()
        {
            if(API.Input is null || Manager.main?.player?.world is null) return;
            if (API.Input.GetButtonUp((int)KeyCode.H)) SpawnRandomCattleNamed();
            if (NeedsUpdate) UpdateInventoryList();
        }

        private void UpdateInventoryList()
        {
            try
            {
                var player = Manager.main.player;
                var inv = Manager.main.player.playerInventoryHandler;
                var containedObjectData = new ContainedObjectsBuffer();
                int num = inv.startPosInBuffer;
                if (EntityUtility.TryGetBuffer<ContainedObjectsBuffer>(inv.inventoryEntity, player.world, out var dynamicBuffer))
                {
                    for (int i = num; i < dynamicBuffer.Length; i++)
                    {
                        var buffer = dynamicBuffer[i];
                        Debug.Log($"Contained: {buffer.objectID} {buffer.amount}");
                        
                    }
                }
                Debug.Log("Updating Inventory List!");
            } catch (Exception e)
            {
                Debug.LogException(e);
            }
            
        }

        public void SpawnRandomCattleNamed()
        {
            try
            {
                var player = Manager.main.player;
                if (player is null) return;
                var inv = player.playerInventoryHandler;
                var world = API.Server.World;
                var server = API.Server;
                var entityManager = world.EntityManager;
                float3 pos = player.GetEntityPosition();
                const ObjectID spawnID = ObjectID.PetBird;
                
                var entity = server.DropObject((int)spawnID, 0, 1, pos);
                var buffer = entityManager.GetBuffer<ContainedObjectsBuffer>(entity);
                foreach (var item in buffer)
                {
                    NamedPetObject = item;
                    Debug.Log($"Got Pet: {item.objectID}");
                }
                NeedsUpdate = true;
                Debug.Log("Spawned Pet!");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
    
    
}
