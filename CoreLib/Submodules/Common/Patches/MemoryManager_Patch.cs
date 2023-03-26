using System.Diagnostics;
using HarmonyLib;

namespace CoreLib.Submodules.Common.Patches
{
    public static class MemoryManager_Patch
    {
        private static bool patchIsApplied;

        internal static void TryPatch()
        {
            if (patchIsApplied) return;
            
            CoreLibPlugin.harmony.PatchAll(typeof(MemoryManager_Patch));

            patchIsApplied = true;
        }
        
        
        [HarmonyPatch(typeof(MemoryManager), nameof(MemoryManager.Init))]
        [HarmonyPrefix]
        public static void OnMemoryInit(MemoryManager __instance)
        {
            if (__instance.poolablePrefabBank == null) return;
            
            PrefabCrawler.FindMaterials(__instance.poolablePrefabBank.poolInitializers);
            CoreLibPlugin.Logger.LogInfo($"Finished crawling prefabs, found {PrefabCrawler.materials.Count} materials!");

            ModEntity.Patches.MemoryManager_IndirectPatch.InjectNewPrefabs(__instance);
            Equipment.Patches.MemoryManager_IndirectPatch.InjectNewEquipmentSlots(__instance);
        }
    }
}