using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Submodules.ChatCommands;
using CoreLib.Submodules.ModEntity;
using HarmonyLib;
using I2.Loc;

namespace CoreLib.Submodules.ChatCommands.Patches
{
    [HarmonyPatch]
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

                if (EntityModule.Loaded)
                {
                    CheckModStringIDs();
                }

                initialized = true;
            }
        }

        private static void CheckModStringIDs()
        {
            var modIDs = EntityModule.modEntityIDs.ModIDs;
            int count = 0;
            
            foreach (KeyValuePair<string,int> pair in modIDs)
            {
                CommandUtil.friendlyNameDict.Add(pair.Key.ToLower(), (ObjectID)pair.Value);
                count++; 
            }
            
            CoreLibPlugin.Logger.LogInfo($"Got {count} mod id strings!");
        }

        private static void CheckLocalizationSources()
        {
            foreach (LanguageSourceData source in LocalizationManager.Sources)
            {
                int count = 0;
                TermData[] filteredTerms = source.mTerms._items.Where(data => data != null && data.Term != null && data.Term.StartsWith("Items/")).ToArray();
                foreach (TermData term in filteredTerms)
                {
                    try
                    {
                        if (term.Term.Contains("Desc")) continue;

                        string objIdName = term.Term[6..];
                        ObjectID objectID = Enum.Parse<ObjectID>(objIdName);
                        CommandUtil.friendlyNameDict.Add(term.Languages[0].ToLower(), objectID);
                        count++;
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }

                CoreLibPlugin.Logger.LogInfo($"Got {count} friendly name entries!");
            }
        }
    }
}