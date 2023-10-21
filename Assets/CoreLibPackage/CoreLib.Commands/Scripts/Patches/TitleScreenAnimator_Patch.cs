using System;
using System.Linq;
using HarmonyLib;
using I2.Loc;

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
                    CheckLocalizationSources();
                }

                initialized = true;
            }
        }

        private static void CheckLocalizationSources()
        {
            foreach (LanguageSourceData source in LocalizationManager.Sources)
            {
                int count = 0;
                TermData[] filteredTerms = source.mTerms.Where(data => data != null && data.Term != null && data.Term.StartsWith("Items/")).ToArray();
                foreach (TermData term in filteredTerms)
                {
                    try
                    {
                        if (term.Term.Contains("Desc")) continue;

                        string objIdName = term.Term[6..];
                        ObjectID objectID = (ObjectID)Enum.Parse(typeof(ObjectID), objIdName);
                        CommandsModule.friendlyNameDict.Add(term.Languages[0].ToLower(), objectID);
                        count++;
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }

                CoreLibMod.Log.LogInfo($"Got {count} friendly name entries!");
            }
        }
    }
}