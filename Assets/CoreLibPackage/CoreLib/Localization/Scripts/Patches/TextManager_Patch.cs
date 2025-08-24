using HarmonyLib;
using I2.Loc;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Localization.Patches
{
    /// <summary>
    /// Provides a Harmony patch for the <c>TextManager</c> class to enhance functionality related to localization.
    /// </summary>
    /// <remarks>
    /// This patch extends the initialization process of the <c>TextManager</c> by injecting custom translation terms
    /// from the <c>LocalizationModule</c> into the appropriate <c>LanguageSourceData</c>. If no localization source is found,
    /// a warning message is logged instead.
    /// </remarks>
    [HarmonyPatch]
    public static class TextManager_Patch
    {
        /// Handles the initialization process after the TextManager's Init method is executed.
        /// Populates the localization system with additional translations if a localization source is available.
        /// <param name="__instance">The instance of the TextManager being initialized.</param>
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
                LocalizationModule.Log.LogWarning("No localization source found! Skipping applying translations!");
            }
        }
    }
}