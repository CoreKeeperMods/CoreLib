using System;
using CoreLib.Components;
using CoreLib.Util.Extensions;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using Unity.Entities;
using Unity.Entities.Conversion;
using UnityEngine;

namespace CoreLib.Submodules.CustomEntity.Patches;

public static class PugDatabaseAuthoring_Patch
{
    [HarmonyPatch(typeof(PugDatabaseAuthoring), nameof(PugDatabaseAuthoring.DeclareReferencedPrefabs))]
    [HarmonyPrefix]
    public static void InitMaterials(PugDatabaseAuthoring __instance, List<GameObject> referencedPrefabs)
    {
        if (!CustomEntityModule.hasConverted)
        {
            try
            {
                ApplyOnDB(__instance);
            }
            catch (Exception e)
            {
                CoreLibPlugin.Logger.LogInfo($"Second phase add failed!\n{e}");
            }

            CustomEntityModule.hasConverted = true;
        }
    }

    [HarmonyPatch(typeof(ECSManager), nameof(ECSManager.Init))]
    [HarmonyPrefix]
    public static void InitMaterials(ECSManager __instance)
    {
        PugDatabaseAuthoring authoring = __instance.pugDatabase;
        if (!CustomEntityModule.hasInjected)
        {
            PrefabCrawler.SetupPrefabIDMap(authoring.prefabList);

            foreach (var pair in CustomEntityModule.modEntityModifyFunctions)
            {
                ObjectID objectID = CustomEntityModule.GetObjectId(pair.Key);
                if (objectID == ObjectID.None)
                {
                    CoreLibPlugin.Logger.LogWarning($"Failed to resolve mod entity target: {pair.Key}!");
                    continue;
                }

                CustomEntityModule.entityModifyFunctions.AddDelegate(objectID, pair.Value);
            }

            CustomEntityModule.modEntityModifyFunctions.Clear();
        }

        ApplyOnDB(authoring);

        CustomEntityModule.hasInjected = true;
    }

    public static void ApplyOnDB(PugDatabaseAuthoring authoring)
    {
        foreach (var prefabs in CustomEntityModule.entitiesToAdd.Values)
        {
            if (!ApplyModAuthorings(prefabs)) continue;

            foreach (EntityMonoBehaviourData data in prefabs)
            {
                authoring.prefabList.Add(data);
            }
        }

        CoreLibPlugin.Logger.LogInfo(
            $"Added {CustomEntityModule.entitiesToAdd.Count} entities!");

        if (!CustomEntityModule.hasInjected)
        {
            Action<EntityMonoBehaviourData> allAction = null;

            if (CustomEntityModule.entityModifyFunctions.ContainsKey(ObjectID.None))
            {
                allAction = CustomEntityModule.entityModifyFunctions[ObjectID.None];
            }

            foreach (EntityMonoBehaviourData entity in authoring.prefabList)
            {
                allAction?.Invoke(entity);

                if (CustomEntityModule.entityModifyFunctions.ContainsKey(entity.objectInfo.objectID))
                {
                    CustomEntityModule.entityModifyFunctions[entity.objectInfo.objectID]?.Invoke(entity);
                }
            }

            CoreLibPlugin.Logger.LogInfo("Finished modifying entities!");
        }
    }

    private static bool ApplyModAuthorings(System.Collections.Generic.List<EntityMonoBehaviourData> prefabs)
    {
        foreach (EntityMonoBehaviourData data in prefabs)
        {
            if (data == null)
            {
                CoreLibPlugin.Logger.LogInfo("EntityMonoBehaviourData is null!");
                continue;
            }

            Il2CppArrayBase<ModCDAuthoringBase> overrides = data.GetComponents<ModCDAuthoringBase>();
            foreach (ModCDAuthoringBase modOverride in overrides)
            {
                if (!ApplyModAuthoring(modOverride, data)) return false;
            }
        }

        return true;
    }

    internal static bool ApplyModAuthoring(ModCDAuthoringBase modAuthoring, EntityMonoBehaviourData data)
    {
        bool success;
        try
        {
            success = modAuthoring.Apply(data);
        }
        catch (Exception e)
        {
            CoreLibPlugin.Logger.LogWarning($"Exception in {modAuthoring.GetIl2CppType().FullName}:\n{e}");
            success = false;
        }

        if (!success)
        {
            CoreLibPlugin.Logger.LogWarning(
                $"Failed to add entity {data.objectInfo.objectID.ToString()}, variation {data.objectInfo.variation} prefab, because {modAuthoring.GetIl2CppType().FullName} mod authoring failed to apply!");
            return false;
        }

        return true;
    }
}