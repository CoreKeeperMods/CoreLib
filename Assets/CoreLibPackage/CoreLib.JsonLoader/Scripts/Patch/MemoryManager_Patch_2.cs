using HarmonyLib;

namespace CoreLib.JsonLoader.Patch
{
    public class MemoryManager_Patch_2
    {
        [HarmonyPatch(typeof(MemoryManager), nameof(MemoryManager.Init))]
        [HarmonyPrefix]
        internal static void PerformPostLoad()
        {
            JsonLoaderModule.PostApply();
        }
    }
}