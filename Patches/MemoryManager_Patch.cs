using System.Collections.Generic;
using HarmonyLib;
using I2.Loc;

namespace CoreLib.Patches;

[HarmonyPatch]
public static class MemoryManager_Patch
{
    [HarmonyPatch(typeof(MemoryManager), nameof(MemoryManager.Init))]
    [HarmonyPrefix]
    public static void OnMemoryInit(MemoryManager __instance)
    {
        LanguageSourceData source = LocalizationManager.Sources[0];
        
        foreach (var pair in Localization.addedTranslations)
        {
            source.AddTerm(pair.Key, pair.Value);
        }
        source.UpdateDictionary();
        Localization.localizationSystemReady = true;
    }
}