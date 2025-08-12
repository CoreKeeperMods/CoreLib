using System.Linq;
using CoreLib.Util;
using HarmonyLib;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity.Patches
{
    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// Provides a Harmony patch for the MemoryManager class, enabling the injection
    /// of additional poolable prefabs into its pooling system. This patch specifically
    /// targets the initialization process of the MemoryManager to modify or augment
    /// its behavior with custom prefab handling logic.
    /// </summary>
    public static class MemoryManager_Patch
    {
        /// Adds poolable prefabs to the memory manager's pooled objects.
        /// This method is a Harmony prefix patch for the `Init` method of the `MemoryManager` class.
        /// It injects additional poolable prefabs into the memory manager's poolablePrefabBanks,
        /// applying modifications and settings as needed.
        /// <param name="__instance">
        /// The instance of the `MemoryManager` being initialized.
        /// </param>
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