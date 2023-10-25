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
            CheckModItemsSafe();
        }
        
        [HarmonyPatch(typeof(ECSManager), nameof(ECSManager.LoadSubScenes))]
        [HarmonyPostfix]
        public static void OnLoadSubScenes()
        {
            CheckModItemsSafe();
        }


        private static void CheckModItemsSafe()
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
            var objectIDLookup = Manager.mod.Authoring.ObjectIDLookup;
            CoreLibMod.Log.LogInfo($"Checking mod item ids! There are {objectIDLookup.Count} items!");
            
            foreach (var pair in objectIDLookup)
            {
                if (Enum.IsDefined(typeof(ObjectID), pair.Value)) continue;
                if (CommandsModule.friendlyNameDict.ContainsKey(pair.Key.ToLower())) continue;
                
                CoreLibMod.Log.LogInfo($"adding mapping: {pair.Key.ToLower()} -> {pair.Value}");
                CommandsModule.friendlyNameDict.Add(pair.Key.ToLower(), pair.Value);
            }
        }
    }
}