using CoreLib;
using CoreLib.Submodules.CustomEntity;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

namespace CoreLib.Submodules.CustomEntity.Patches;

public static class PugDatabaseAuthoring_Patch
{
    [HarmonyPatch(typeof(PugDatabaseAuthoring), nameof(PugDatabaseAuthoring.DeclareReferencedPrefabs))]
    [HarmonyPostfix]
    public static void InitMaterials(PugDatabaseAuthoring __instance, List<GameObject> referencedPrefabs) 
    {
        if (!CustomEntityModule.hasInjected)
        {
            CoreLibPlugin.Logger.LogInfo($"Start crawling materials!");

            MaterialCrawler.FindAllMaterials(__instance);

            CoreLibPlugin.Logger.LogInfo($"Finished crawling materials, found {MaterialCrawler.materials.Count} materials!");
            CustomEntityModule.hasInjected = true;
        }

        foreach (EntityMonoBehaviourData data in CustomEntityModule.entitiesToAdd)
        {
            __instance.prefabList.Add(data);
            referencedPrefabs.Add(data.gameObject);
        }
            
        CoreLibPlugin.Logger.LogInfo($"Added {CustomEntityModule.entitiesToAdd.Count} entities!");

    }
}