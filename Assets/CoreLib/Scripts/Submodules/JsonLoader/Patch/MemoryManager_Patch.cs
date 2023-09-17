using HarmonyLib;

namespace CoreLib.Submodules.JsonLoader.Patch
{
    public class MemoryManager_Patch
    {
        [HarmonyPatch(typeof(MemoryManager), nameof(MemoryManager.Init))]
        [HarmonyPrefix]
        internal static void PerformPostLoad()
        {
            JsonLoaderModule.PostApply();
        }
    }
}