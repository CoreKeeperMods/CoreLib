using System;
using System.Collections.Generic;
using CoreLib.Util.Extensions;
using HarmonyLib;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Audio.Patches
{
    /// <summary>
    /// A utility class that provides Harmony patches for modifying the behavior of the AudioManager class.
    /// </summary>
    public static class AudioManagerPatch
    {
        /// Determines whether the provided sound effect ID (SfxID) is valid based on its range and system rules.
        /// The method verifies that the provided ID is non-negative and falls within the valid range of IDs defined by the system.
        /// <param name="id">The sound effect ID to validate.</param>
        /// <param name="__result">The result of the validation. True if the ID is valid, false otherwise.</param>
        [HarmonyPatch(typeof(AudioManager), nameof(AudioManager.IsLegalSfxID))]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        public static void IsSfxValid(SfxID id, ref bool __result)
        {
            __result = id >= 0 && (int)id < AudioModule.LastFreeSfxId;
        }
        
        /// <summary>
        /// Handles operations that should occur when the "Initialized" property of the AudioManager is set.
        /// Automatically loads custom audio files and updates the internal audio field map and reuse map accordingly.
        /// </summary>
        /// <param name="__instance">The instance of the AudioManager that triggered the setter call.</param>
        [HarmonyPatch(typeof(AudioManager), "Initialized", MethodType.Setter)]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        public static void OnSetInitialized(AudioManager __instance)
        {
            var customSoundEffect = AudioModule.CustomSoundEffects;

            if (customSoundEffect.Count == 0) return;

            var fieldMap = __instance.GetValue<AudioField[]>("audioFieldMap");
            int oldSize = fieldMap.Length;
            int newSize = oldSize + customSoundEffect.Count;

            Array.Resize(ref fieldMap,  newSize);

            for (int i = 0; i < customSoundEffect.Count; i++)
            {
                fieldMap[oldSize + i] = customSoundEffect[i];
            }
            
            __instance.SetValue("audioFieldMap", fieldMap);
            __instance.reuseMap = new PoolableAudioSource[fieldMap.Length];

            BaseSubmodule.Log.LogInfo($"Loaded {customSoundEffect.Count} custom sound effects!");
        }
    }
}