using System;
using System.Collections.Generic;
using HarmonyLib;
using PugMod;
using PugTilemap;

namespace CoreLib.Commands.Patches
{
    public static class ECSManager_Patch
    {
        [HarmonyPatch(typeof(ECSManager), nameof(ECSManager.Init))]
        [HarmonyPostfix]
        public static void AfterInit()
        {
            try
            {
                CheckModItemNames();
            }
            catch (Exception e)
            {
                CoreLibMod.Log.LogWarning($"Exception checking modded item names:\n{e}");
            }
        }

        private static void CheckModItemNames()
        {
            foreach (var pair in Manager.mod.Authoring.ObjectIDLookup)
            {
                if (Enum.IsDefined(typeof(ObjectID), pair.Value)) continue;
                
                CommandUtil.friendlyNameDict.Add(pair.Key, pair.Value);
            }
        }
    }
}