using System.Linq;
using CoreLib.Util;
using HarmonyLib;
using UnityEngine;

namespace CoreLib.Submodules.ModEntity.Patches
{
    public static class MemoryManager_Patch
    {
        [HarmonyPatch(typeof(MemoryManager), nameof(MemoryManager.Init))]
        [HarmonyPrefix]
        public static void InjectPoolablePrefabs(MemoryManager __instance)
        {
            if (__instance.poolablePrefabBanks == null) return;

            var bank = __instance.poolablePrefabBanks
                .FirstOrDefault(bank => bank is PooledGraphicalObjectBank) as PooledGraphicalObjectBank;
            if (bank == null) return;
            
            PrefabCrawler.FindMaterials(bank.poolInitializers);
            PrefabCrawler.SetupPrefabIDMap(Manager.ecs.pugDatabase.prefabList);
            EntityModule.ApplyAllModAuthorings();
            
            EntityModule.ApplyPrefabModifications(__instance);

            foreach (GameObject prefab in EntityModule.poolablePrefabs)
            {
                MonoBehaviourUtils.ApplyPrefabModAuthorings(null, prefab);
                
                bank.poolInitializers.Add(new PoolablePrefabBank.PoolablePrefab
                {
                    prefab = prefab,
                    initialSize = 16,
                    maxSize = 16
                });

                CoreLibMod.Log.LogInfo($"Added Poolable Prefab {prefab.name}");
            }

            EntityModule.hasInjected = true;
        }
    }
}