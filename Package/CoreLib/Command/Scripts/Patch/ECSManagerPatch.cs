using System;
using HarmonyLib;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Command.Patch
{
    public static class EcsManagerPatch
    {
        /// A method executed as a postfix patch to the ECSManager.Init() method.
        /// It performs additional operations after the ECSManager's initialization to ensure integrity or apply custom modifications.
        [HarmonyPatch(typeof(ECSManager), nameof(ECSManager.Init))]
        [HarmonyPostfix]
        public static void AfterInit()
        {
            CheckModItemsSafe();
        }

        /// Executes logic to validate and ensure modded items are correctly configured.
        /// Handles any exceptions that may occur during the validation process and logs warning messages if necessary.
        private static void CheckModItemsSafe()
        {
            try
            {
                CheckModItemNames();
            }
            catch (Exception e)
            {
                CommandModule.log.LogWarning($"Exception checking modded item names:\n{e}");
            }
        }

        /// Validates and updates the mapping of mod item names to identifiers.
        /// Ensures any unmapped mod item names in the object ID lookup are registered in the friendly name dictionary,
        /// facilitating consistent references to mod items.
        private static void CheckModItemNames()
        {
            var objectIDLookup = Manager.mod.Authoring.ObjectIDLookup;
            CommandModule.log.LogInfo($"Checking mod item ids! There are {objectIDLookup.Count} items!");
            
            foreach (var pair in objectIDLookup)
            {
                if (Enum.IsDefined(typeof(ObjectID), pair.Value)) continue;
                if (CommandModule.friendlyNameDict.ContainsKey(pair.Key.ToLower())) continue;
                
                CommandModule.log.LogInfo($"adding mapping: {pair.Key.ToLower()} -> {pair.Value}");
                CommandModule.friendlyNameDict.Add(pair.Key.ToLower(), pair.Value);
            }
        }
    }
}