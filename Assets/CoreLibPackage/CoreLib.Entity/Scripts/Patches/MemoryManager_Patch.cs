using System.Linq;
using CoreLib.Util;
using HarmonyLib;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity.Patches
{
    // ReSharper disable once InconsistentNaming
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

                var initialSize = 16;
                var maxSize = 64;

                var settings = prefab.GetComponent<PoolSettings>();
                if (settings != null)
                {
                    initialSize = settings.initialSize;
                    maxSize = settings.maxSize;
                }
                
                bank.poolInitializers.Add(new PoolablePrefabBank.PoolablePrefab
                {
                    prefab = prefab,
                    initialSize = initialSize,
                    maxSize = maxSize
                });

                CoreLibMod.Log.LogInfo($"Added Poolable Prefab {prefab.name}");
            }

            EntityModule.hasInjected = true;
        }
    }
}