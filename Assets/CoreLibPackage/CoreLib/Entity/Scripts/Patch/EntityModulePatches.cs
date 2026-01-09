// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: EntityModulePatches.cs
// Author: Minepatcher, Limoka, Moorowl
// Created: 2025-11-19
// Description: Handles all HarmonyLib patches related to entities.
// ========================================================

/* Edited from Moorowl's Paintable Double Chest https://mod.io/g/corekeeper/m/doublechest#description */
using System.Linq;
using CoreLib.Submodule.Entity.Component;
using CoreLib.Util.Extension;
using HarmonyLib;
using Interaction;
using Pug.ECS.Hybrid;
using UnityEngine;
using Logger = CoreLib.Util.Logger;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Patch
{
    public class EntityModulePatches
    {
        private static Logger Log => EntityModule.Log;
        [HarmonyPatch(typeof(MemoryManager), nameof(MemoryManager.Init)), HarmonyPrefix]
        public static void InjectPoolablePrefabs(MemoryManager __instance)
        {
            if (__instance.poolablePrefabBanks == null || EntityModule.HasInjected) return;
            if (EntityModule.PoolablePrefabs.Count <= 0) return;
            Log.LogInfo("Injecting Poolable Prefabs");

            var bank = __instance.poolablePrefabBanks.Find(bank => bank is PooledGraphicalObjectBank) as PooledGraphicalObjectBank;
            if (bank == null) return;
            
            MaterialCrawler.Initialize();
            MaterialCrawler.OnMaterialSwapReady();
            
            foreach (var prefab in EntityModule.PoolablePrefabs)
            {
                if (bank.poolInitializers.Contains(prefab)) continue;
                Log.LogInfo($"Adding {prefab.prefab} to poolable prefabs");
                bank.poolInitializers.Add(prefab);
            }
            
            EntityModule.ApplyPrefabModifications(bank);
            
            EntityModule.HasInjected = true;
            Log.LogInfo($"Injected {EntityModule.PoolablePrefabs.Count} Poolable Prefabs");
		}
		
		
		[HarmonyPatch(typeof(InteractablePostConverter), nameof(InteractablePostConverter.PostConvert)), HarmonyPrefix]
        public static void PostConvertPre(InteractablePostConverter __instance, GameObject authoring)
        {
            if (!CheckHasSupportsPooling(authoring)) return;
            var entityMonoBehaviourData = authoring.AddComponent<EntityMonoBehaviourData>();
            entityMonoBehaviourData.objectInfo = authoring.GetComponent<ObjectAuthoring>().ObjectInfo;
        }
        
        [HarmonyPatch(typeof(InteractablePostConverter), nameof(InteractablePostConverter.PostConvert)), HarmonyPostfix]
        public static void PostConvertPost(InteractablePostConverter __instance, GameObject authoring)
        {
            if (CheckHasSupportsPooling(authoring) && authoring.TryGetComponent(out EntityMonoBehaviourData entityMonoBehaviourData))
                Object.DestroyImmediate(entityMonoBehaviourData);
        }
        
        [HarmonyPatch(typeof(GraphicalObjectConversion), nameof(GraphicalObjectConversion.Convert)), HarmonyPrefix]
        public static bool Convert(GraphicalObjectConversion __instance, GameObject authoring) => !CheckHasSupportsPooling(authoring);
        

        /// <summary>
        /// Applies custom logic to modify the result of the GetObjectName method of the PlayerController class.
        /// This method checks for applicable dynamic item handlers and applies text modifications based on the provided object data.
        /// </summary>
        /// <param name="containedObject">The buffer containing the object data whose name is being retrieved.</param>
        /// <param name="localize">A flag indicating if the object name should be localized.</param>
        /// <param name="__result">The result object used for storing the modified text and format fields.</param>
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.GetObjectName)), HarmonyPostfix]
        public static void GetObjectName(ContainedObjectsBuffer containedObject, bool localize, TextAndFormatFields __result)
        {
            var handler = EntityModule.DynamicItemHandlers.FirstOrDefault(handler => handler.ShouldApply(containedObject.objectData));
            handler?.ApplyText(containedObject.objectData, __result);
        }
        /// <summary>
        /// Updates the <see cref="ColorReplacer"/> instance based on the provided object's data.
        /// Uses dynamic item handlers to determine if colors should be applied for the current object data.
        /// If a handler is applicable and successfully applies the colors, an active color replacement is set.
        /// </summary>
        /// <param name="__instance">The instance of <see cref="ColorReplacer"/> being updated.</param>
        /// <param name="containedObject">The object data encapsulated in a <see cref="ContainedObjectsBuffer"/> used to determine color changes.</param>
        [HarmonyPatch(typeof(ColorReplacer), nameof(ColorReplacer.UpdateColorReplacerFromObjectData)), HarmonyPostfix]
        public static void UpdateReplacer(ColorReplacer __instance, ContainedObjectsBuffer containedObject)
        {
            var handler = EntityModule.DynamicItemHandlers.FirstOrDefault(handler => handler.ShouldApply(containedObject.objectData));
            if (handler == null) return;

            handler.ApplyColors(containedObject.objectData, __instance);
        }
        
        [HarmonyPatch(typeof(CraftingBuilding), "GetCraftingUISettings"), HarmonyPrefix]
        public static bool GetCraftingUISettings(CraftingBuilding __instance, ref CraftingBuilding.CraftingUISettings __result)
        {
            var objectId = __instance.objectData.objectID;
            if(__instance is not ModWorkbenchBuilding modWorkbenchBuilding) return true;
            
            if (!modWorkbenchBuilding.ModdedEntity.TryGetComponent(out ModRefreshCraftingBuildingTitles refreshCraftingUI)
                || !refreshCraftingUI.refreshBuildingTitles) return true;
            
            var window = Manager.ui.GetCraftingCategoryWindowInfo();
            if (window == null) return true;
            /* TODO rework
            int index = Manager.ui.GetCraftingCategoryWindowInfos().FindIndex(win => win == window) - 1;

            if (index != -1)
                __result = __instance.craftingUIOverrideSettings[index];
            else
                __result = new CraftingBuilding.CraftingUISettings(
                    objectId,
                    __instance.craftingUITitle,
                    _instance.craftingUITitleLeftBox,
                    __instance.craftingUITitleRightBox,
                    __instance.craftingUIBackgroundVariation
                );
            return false;*/

            return true;
        }

        /// <summary>
        /// Checks if the provided GameObject has a SupportsPooling component.
        /// </summary>
        /// <param name="authoring">The GameObject to check.</param>
        /// <returns>True if the GameObject has a SupportsPooling component; otherwise, false.</returns>
        private static bool CheckHasSupportsPooling(GameObject authoring) =>
            authoring.TryGetComponent(out ObjectAuthoring objectAuthoring) &&
            objectAuthoring.graphicalPrefab != null &&
            objectAuthoring.graphicalPrefab.TryGetComponent(out MonoBehaviour _) &&
            objectAuthoring.graphicalPrefab.TryGetComponent(out SupportsPooling _);
    }
}