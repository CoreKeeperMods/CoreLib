﻿using System;
using HarmonyLib;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Command.Patches
{
    public static class EcsManagerPatch
    {
        /// <summary>
        /// A method executed as a postfix patch to the ECSManager.Init() method.
        /// It performs additional operations after the ECSManager's initialization to ensure integrity or apply custom modifications.
        /// </summary>
        [HarmonyPatch(typeof(ECSManager), nameof(ECSManager.Init))]
        [HarmonyPostfix]
        public static void AfterInit()
        {
            CheckModItemsSafe();
        }

        /// <summary>
        /// Executes logic to validate and ensure modded items are correctly configured.
        /// Handles any exceptions that may occur during the validation process and logs warning messages if necessary.
        /// </summary>
        private static void CheckModItemsSafe()
        {
            try
            {
                CheckModItemNames();
            }
            catch (Exception e)
            {
                BaseSubmodule.Log.LogWarning($"Exception checking modded item names:\n{e}");
            }
        }

        /// <summary>
        /// Validates and updates the mapping of mod item names to identifiers.
        /// Ensures any unmapped mod item names in the object ID lookup are registered in the friendly name dictionary,
        /// facilitating consistent references to mod items.
        /// </summary>
        private static void CheckModItemNames()
        {
            var objectIDLookup = Manager.mod.Authoring.ObjectIDLookup;
            BaseSubmodule.Log.LogInfo($"Checking mod item ids! There are {objectIDLookup.Count} items!");
            
            foreach (var pair in objectIDLookup)
            {
                if (Enum.IsDefined(typeof(ObjectID), pair.Value)) continue;
                if (CommandsModule.FriendlyNameDict.ContainsKey(pair.Key.ToLower())) continue;
                
                BaseSubmodule.Log.LogInfo($"adding mapping: {pair.Key.ToLower()} -> {pair.Value}");
                CommandsModule.FriendlyNameDict.Add(pair.Key.ToLower(), pair.Value);
            }
        }
    }
}