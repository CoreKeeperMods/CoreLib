using System.Collections;
using System.Reflection;
using CoreLib.Components;
using CoreLib.Submodules;
using CoreLib.Submodules.Equipment;
using HarmonyLib;
using PugMod;
using Unity.Entities;
using UnityEngine;

[assembly: AssemblyVersion("1.0.0.0")]

namespace CoreLib
{
    public class CoreLibMod : IMod
    {
        public static ScriptableObject materialSwapTable;

        public void EarlyInit()
        {
            API.Server.OnWorldCreated += WorldInitialize;
        }

        public void Init()
        { 
            materialSwapTable = Resources.Load<ScriptableObject>("ModSDK/MaterialSwapTable");
            IEnumerable matList = materialSwapTable.GetType().GetField("materials").GetValue(materialSwapTable) as IEnumerable;
            foreach (object matData in matList)
            {
                var matName = matData.GetType().GetField("materialName").GetValue(matData) as string;
                Logger.LogInfo($"Material: {matName}");
            }

        }

        public void Shutdown()
        {
        }

        public void ModObjectLoaded(Object obj)
        {
        }

        public bool CanBeUnloaded()
        {
            return false;
        }

        public void Update()
        {
        }

        public void WorldInitialize()
        {
            //PrefabCrawler.SetupPrefabIDMap(Manager.ecs.pugDatabase.prefabList);
        }
        
        private void OnObjectSpawned(Entity entity, EntityManager entitymanager, GameObject graphicalobject)
        {
        }

        public void MessageReceivedOnClient(int messageType, Entity entity, int value0, int value1)
        {
        }

        public void MessageReceivedOnServer(int messageType, Entity entity, int value0, int value1)
        {
        }
    }
}