﻿using System;
using CoreLib.Util;

namespace CoreLib.Submodules.ModEntity.Patches;

public static class MemoryManager_IndirectPatch
{
    internal static void ModifyPrefabs(MemoryManager memoryManager)
    {
        if (!EntityModule.Loaded) return;

        foreach (var prefab in memoryManager.poolablePrefabBank.poolInitializers)
        {
            EntityMonoBehaviour prefabMono = prefab.prefab.GetComponent<EntityMonoBehaviour>();
            if (prefabMono == null) continue;

            Il2CppSystem.Type cppType = prefabMono.GetIl2CppType();
            if (EntityModule.prefabModifyFunctions.ContainsKey(cppType))
            {
                try
                {
                    EntityModule.prefabModifyFunctions[cppType]?.Invoke(prefabMono);
                }
                catch (Exception e)
                {
                    CoreLibPlugin.Logger.LogError($"Error while executing prefab modification for type {cppType.FullName}!\n{e}");
                }
            }
        }
        CoreLibPlugin.Logger.LogInfo("Finished Modifying Prefabs!");
    }
    
    internal static void InjectNewPrefabs(MemoryManager memoryManager)
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
                
                Il2CppSystem.Type prefabType = prefabMono.GetIl2CppType();
                if (EntityModule.loadedPrefabTypes.Contains(prefabType)) continue;

                MonoBehaviourUtils.ApplyPrefabModAuthorings(entity, prefabInfo.prefab.gameObject);

                PoolablePrefabBank.PoolablePrefab prefab = new PoolablePrefabBank.PoolablePrefab
                {
                    prefab = prefabInfo.prefab.gameObject,
                    initialSize = 16,
                    maxSize = 1024
                };

                memoryManager.poolablePrefabBank.poolInitializers.Add(prefab);
                EntityModule.loadedPrefabTypes.Add(prefabType);
                count++;
            }
        }

        CoreLibPlugin.Logger.LogDebug($"Added {count} custom prefab pools!");
    }
}