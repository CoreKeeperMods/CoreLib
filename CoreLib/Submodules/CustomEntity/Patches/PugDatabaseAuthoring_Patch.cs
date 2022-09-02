using System;
using CoreLib;
using CoreLib.Submodules.CustomEntity;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreLib.Submodules.CustomEntity.Patches;

public static class PugDatabaseAuthoring_Patch
{
    [HarmonyPatch(typeof(PugDatabaseAuthoring), nameof(PugDatabaseAuthoring.DeclareReferencedPrefabs))]
    [HarmonyPostfix]
    public static void InitMaterials(PugDatabaseAuthoring __instance, List<GameObject> referencedPrefabs) 
    {
        if (!CustomEntityModule.hasInjected)
        {
            CoreLibPlugin.Logger.LogInfo($"Start crawling prefabs!");

            PrefabCrawler.CheckPrefabs(__instance);

            CoreLibPlugin.Logger.LogInfo($"Finished crawling prefabs, found {PrefabCrawler.materials.Count} materials!");
            CustomEntityModule.hasInjected = true;
        }

        foreach (var prefabs in CustomEntityModule.entitiesToAdd.Values)
        {
            if (!ApplyOverrides(prefabs)) continue;

            foreach (EntityMonoBehaviourData data in prefabs)
            {
                __instance.prefabList.Add(data);
                referencedPrefabs.Add(data.gameObject);
            }
        }

        CoreLibPlugin.Logger.LogInfo($"Added {CustomEntityModule.entitiesToAdd.Count} entities!");

        foreach (EntityMonoBehaviourData entity in __instance.prefabList)
        {
            if (CustomEntityModule.entityModifyFunctions.ContainsKey(entity.objectInfo.objectID))
            {
                CustomEntityModule.entityModifyFunctions[entity.objectInfo.objectID]?.Invoke(entity);
            }
        }
        CustomEntityModule.entityModifyFunctions.Clear();
        
        CoreLibPlugin.Logger.LogInfo("Finished modifying entities!");

    }

    private static bool ApplyOverrides(System.Collections.Generic.List<EntityMonoBehaviourData> prefabs)
    {
        foreach (EntityMonoBehaviourData data in prefabs)
        {
            Il2CppArrayBase<ModCDAuthoringBase> overrides = data.GetComponents<ModCDAuthoringBase>();
            foreach (ModCDAuthoringBase modOverride in overrides)
            {
                if (!ApplyOverride(modOverride, data)) return false;
            }
        }
        return true;
    }

    internal static bool ApplyOverride(ModCDAuthoringBase modOverride, EntityMonoBehaviourData data)
    {
        bool success;
        try
        {
            success = modOverride.Apply(data);
        }
        catch (Exception e)
        {
            CoreLibPlugin.Logger.LogWarning($"Exception in {modOverride.GetIl2CppType().FullName}:\n{e}");
            success = false;
        }

        if (!success)
        {
            CoreLibPlugin.Logger.LogWarning(
                $"Failed to add entity {data.objectInfo.objectID.ToString()}, variation {data.objectInfo.variation} prefab, because {modOverride.GetIl2CppType().FullName} override failed to apply!");
            return false;
        }

        return true;
    }
}