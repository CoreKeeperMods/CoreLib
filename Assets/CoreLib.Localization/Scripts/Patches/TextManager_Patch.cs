using HarmonyLib;
using I2.Loc;

namespace CoreLib.Localization.Patches
{
    [HarmonyPatch]
    public static class TextManager_Patch
    {
        [HarmonyPatch(typeof(TextManager), nameof(TextManager.Init))]
        [HarmonyPostfix]
        public static void OnMemoryInit(TextManager __instance)
        {
            if (LocalizationManager.Sources.Count > 0)
            {
                LanguageSourceData source = LocalizationManager.Sources[1];

                foreach (var pair in LocalizationModule.addedTranslations)
                {
                    source.AddTerm(pair.Key, pair.Value);
                }

                source.UpdateDictionary();
                LocalizationModule.localizationSystemReady = true;
            }
            else
            {
                CoreLibMod.Log.LogWarning("No localization source found! Skipping applying translations!");
            }
        }
    }
}