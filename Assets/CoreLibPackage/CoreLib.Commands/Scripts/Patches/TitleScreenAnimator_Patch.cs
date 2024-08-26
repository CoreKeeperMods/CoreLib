using System;
using System.Linq;
using HarmonyLib;
using I2.Loc;
using PugMod;
/*
namespace CoreLib.Commands.Patches
{
    public class TitleScreenAnimator_Patch
    {
        private static bool initialized;

        [HarmonyPatch(typeof(TitleScreenAnimator), nameof(TitleScreenAnimator.Start))]
        [HarmonyPostfix]
        public static void OnTitleStart()
        {
            if (!initialized)
            {
                if (LocalizationManager.Sources.Count > 0)
                {
                    AddItemFrendlyNames();
                }

                initialized = true;
            }
        }

        private static void AddItemFrendlyNames()
        {
            int count = 0;
            var modAuthorings = (API.Authoring as ModAPIAuthoring).ObjectIDLookup;
            foreach (var pair in modAuthorings)
            {
                if (LocalizationManager.TryGetTranslation($"Items/{pair.Key}", out var translation, overrideLanguage: "english"))
                {
                    if (!CommandsModule.friendlyNameDict.ContainsKey(translation.ToLower()))
                    {
                        CommandsModule.friendlyNameDict.Add(translation.ToLower(), pair.Value); 
                        count++;
                    }
                }
            }
            
            CoreLibMod.Log.LogInfo($"Got {count} friendly name entries!");
        }
    }
}*/