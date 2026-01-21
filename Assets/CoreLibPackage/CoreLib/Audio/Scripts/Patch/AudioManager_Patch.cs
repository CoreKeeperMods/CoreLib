// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: AudioManager_Patch.cs
// Author: Minepatcher, Limoka
// Created: 2025-11-07
// Description: Provides Harmony patches that modify AudioManager behavior,
//              including SFX validation and initialization handling for custom audio support.
// ========================================================

using System;
using CoreLib.Util.Extension;
using HarmonyLib;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Audio.Patch
{
    /// Contains Harmony patches that extend or modify the behavior of the game's <see cref="AudioManager"/>.
    /// <remarks>
    /// This patch class adds CoreLib’s custom sound effect validation and initialization logic
    /// to the base AudioManager system. It integrates custom sound effects into the existing
    /// <c>audioFieldMap</c> and ensures reuse maps are updated accordingly.
    /// </remarks>
    /// <seealso cref="AudioModule"/>
    /// <seealso cref="HarmonyPatch"/>
    /// <seealso cref="AudioManager"/>
    public static class AudioManagerPatch
    {
        #region Harmony Patch: IsLegalSfxID

        /// Validates whether a given sound effect identifier (<see cref="SfxID"/>) falls within a legal range.
        /// <param name="id">The sound effect identifier to validate.</param>
        /// <param name="__result">
        /// The output flag indicating whether the specified ID is valid. Set to <c>true</c> if valid; otherwise, <c>false</c>.
        /// </param>
        /// <remarks>
        /// This patch intercepts the <see cref="AudioManager.IsLegalSfxID"/> method using a
        /// <see cref="HarmonyPostfix"/> to override the original result.
        /// It ensures that the provided ID lies within the range of valid CoreLib-managed SFX indices.
        /// </remarks>
        /// <example>
        /// <code>
        /// bool isValid = AudioManager.IsLegalSfxID(SfxID.MyCustomSound);
        /// // Returns true if within the CoreLib-defined SFX ID range.
        /// </code>
        /// </example>
        [HarmonyPatch(typeof(AudioManager), nameof(AudioManager.IsLegalSfxID))]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        public static void IsSfxValid(SfxID id, ref bool __result)
        {
            __result = id >= 0 && (int)id < AudioModule.lastFreeSfxId;
        }

        #endregion

        #region Harmony Patch: Initialized (Setter)

        /// Executes after the <see cref="AudioManager.Initialized"/> property is set,
        /// enabling integration of custom sound effects into the game’s audio system.
        /// <param name="__instance">
        /// The instance of the <see cref="AudioManager"/> being initialized.
        /// </param>
        /// <remarks>
        /// This patch loads all CoreLib-defined custom audio clips and appends them to the AudioManager’s
        /// internal <c>audioFieldMap</c>. It also updates the reuse map and logs the number of loaded sounds.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Called automatically during AudioManager initialization
        /// // when Harmony patches are active.
        /// </code>
        /// </example>
        [HarmonyPatch(typeof(AudioManager), "Initialized", MethodType.Setter)]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        public static void OnPostInit(AudioManager __instance)
        {
            var customSoundEffect = AudioModule.customSoundEffects;

            if (customSoundEffect.Count == 0)
                return;

            var fieldMap = __instance.GetValue<AudioField[]>("audioFieldMap");
            int newSize = fieldMap.Length + customSoundEffect.Count;

            Array.Resize(ref fieldMap, newSize);
            fieldMap = fieldMap.AddRangeToArray(customSoundEffect.ToArray());

            __instance.SetValue("audioFieldMap", fieldMap);
            __instance.reuseMap = new PoolableAudioSource[fieldMap.Length];

            AudioModule.log.LogInfo(
                $"Loaded {customSoundEffect.Count} custom sound effect" +
                $"{(customSoundEffect.Count > 1 ? "s" : string.Empty)}!"
            );
        }

        #endregion
    }
}