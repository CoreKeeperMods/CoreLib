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
using HarmonyLib;
using Logger = CoreLib.Util.Logger;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Patch
{
    public class EntityModulePatches
    {
        private static Logger Log => EntityModule.log;

        [HarmonyPatch(typeof(MemoryManager), nameof(MemoryManager.Init)), HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        public static void InjectPoolablePrefabs(MemoryManager __instance)
        {
            if (__instance.poolablePrefabBanks == null || EntityModule.hasInjected) return;
            Log.LogInfo("Applying Materials & Prefab Modifications");

            var bank = __instance.poolablePrefabBanks.FindAll(bank => bank is PooledGraphicalObjectBank);
            if (bank.Count <= 0) return;

            MaterialCrawler.Initialize();
            MaterialCrawler.OnMaterialSwapReady();

            bank.ForEach(EntityModule.ApplyPrefabModifications);

            EntityModule.hasInjected = true;
        }
        
        /// Applies custom logic to modify the result of the GetObjectName method of the PlayerController class.
        /// This method checks for applicable dynamic item handlers and applies text modifications based on the provided object data.
        /// <param name="containedObject">The buffer containing the object data whose name is being retrieved.</param>
        /// <param name="localize">A flag indicating if the object name should be localized.</param>
        /// <param name="__result">The result object used for storing the modified text and format fields.</param>
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.GetObjectName)), HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        public static void GetObjectName(ContainedObjectsBuffer containedObject, bool localize, TextAndFormatFields __result)
        {
            var handler = EntityModule.dynamicItemHandlers.FirstOrDefault(handler => handler.ShouldApply(containedObject.objectData));
            handler?.ApplyText(containedObject.objectData, __result);
        }
        /// Updates the <see cref="ColorReplacer"/> instance based on the provided object's data.
        /// Uses dynamic item handlers to determine if colors should be applied for the current object data.
        /// If a handler is applicable and successfully applies the colors, an active color replacement is set.
        /// <param name="__instance">The instance of <see cref="ColorReplacer"/> being updated.</param>
        /// <param name="containedObject">The object data encapsulated in a <see cref="ContainedObjectsBuffer"/> used to determine color changes.</param>
        [HarmonyPatch(typeof(ColorReplacer), nameof(ColorReplacer.UpdateColorReplacerFromObjectData)), HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        public static void UpdateReplacer(ColorReplacer __instance, ContainedObjectsBuffer containedObject)
        {
            var handler = EntityModule.dynamicItemHandlers.FirstOrDefault(handler => handler.ShouldApply(containedObject.objectData));
            if (handler == null) return;

            handler.ApplyColors(containedObject.objectData, __instance);
        }
        
        [HarmonyPatch(typeof(CraftingBuilding), "GetCraftingUISettings"), HarmonyPrefix]
        // ReSharper disable all InconsistentNaming
        public static bool GetCraftingUISettings(CraftingBuilding __instance, ref CraftingBuilding.CraftingUISettings __result)
        {
            var objectId = __instance.objectData.objectID;
            if(__instance is not ModWorkbenchBuilding modWorkbenchBuilding) return true;
            
            if (!modWorkbenchBuilding.moddedEntity.TryGetComponent(out ModRefreshCraftingBuildingTitles refreshCraftingUI)
                || !refreshCraftingUI.refreshBuildingTitles) return true;
            
            var window = Manager.ui.GetCraftingCategoryWindowInfo();
            if (window == null) return true;
            int index = Manager.ui.GetCraftingCategoryWindowInfos().FindIndex(win => win == window) - 1;

            __result = index != -1 ? __instance.buildingSpecificUISettings[index].settings : 
                new CraftingBuilding.CraftingUISettings(__instance.defaultUISettings.craftingUIBackgroundVariation, __instance.defaultUISettings.titles.ToArray());
            return false;
        }
    }
}