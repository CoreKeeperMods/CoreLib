using HarmonyLib;
using I2.Loc;

namespace CoreLib.Submodules.Localization.Patches;

[HarmonyPatch]
public static class MemoryManager_Patch
{
    [HarmonyPatch(typeof(MemoryManager), nameof(MemoryManager.Init))]
    [HarmonyPrefix]
    public static void OnMemoryInit(MemoryManager __instance)
    {
        if (LocalizationManager.Sources.Count > 0)
        {
            LanguageSourceData source = LocalizationManager.Sources._items[0];

            foreach (var pair in LocalizationModule.addedTranslations)
            {
                source.AddTerm(pair.Key, pair.Value);
            }

            source.UpdateDictionary();
            LocalizationModule.localizationSystemReady = true;
        }
        else
        {
            CoreLibPlugin.Logger.LogWarning("No localization source found! Skipping applying translations!");
        }
    }
}