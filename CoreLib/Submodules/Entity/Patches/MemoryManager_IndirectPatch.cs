using CoreLib.Util;
using HarmonyLib;
using Il2CppSystem;

namespace CoreLib.Submodules.ModEntity.Patches;

public static class MemoryManager_IndirectPatch
{
    internal static void InjectNewPrefabs(MemoryManager __instance)
    {
        if (!EntityModule.Loaded) return;
        
        int count = 0;

        foreach (var prefabs in EntityModule.entitiesToAdd.Values)
        {
            if (prefabs.Count <= 0) continue;
            if (MonoBehaviourUtils.HasPrefabOverrides(prefabs)) continue;

            EntityMonoBehaviourData entity = prefabs[0];

            foreach (PrefabInfo prefabInfo in entity.objectInfo.prefabInfos)
            {
                if (prefabInfo.prefab == null) continue;
                EntityMonoBehaviour prefabMono = prefabInfo.prefab.GetComponent<EntityMonoBehaviour>();
                if (prefabMono == null) continue;
                
                Type prefabType = prefabMono.GetIl2CppType();
                if (EntityModule.loadedPrefabTypes.Contains(prefabType)) continue;

                MonoBehaviourUtils.ApplyPrefabModAuthorings(entity, prefabInfo.prefab.gameObject);

                PoolablePrefabBank.PoolablePrefab prefab = new PoolablePrefabBank.PoolablePrefab
                {
                    prefab = prefabInfo.prefab.gameObject,
                    initialSize = 16,
                    maxSize = 1024
                };

                __instance.poolablePrefabBank.poolInitializers.Add(prefab);
                EntityModule.loadedPrefabTypes.Add(prefabType);
                count++;
            }
        }

        CoreLibPlugin.Logger.LogDebug($"Added {count} custom prefab pools!");
    }
}