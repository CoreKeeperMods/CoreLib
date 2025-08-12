using System;
using HarmonyLib;

namespace CoreLib.Commands.Patches
{
    public static class ECSManager_Patch
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
                CoreLibMod.Log.LogWarning($"Exception checking modded item names:\n{e}");
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