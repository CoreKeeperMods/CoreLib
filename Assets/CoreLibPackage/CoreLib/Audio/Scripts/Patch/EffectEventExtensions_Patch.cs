// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: EffectEventExtensions_Patch.cs
// Author: Minepatcher, Limoka
// Created: 2025-11-07
// Description: Provides a Harmony patch that extends the behavior of EffectEventExtensions.PlayEffect,
//              enabling custom CoreLib-defined effects to be triggered within the ECS context.
// ========================================================

using HarmonyLib;
using Unity.Entities;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Audio.Patch
{
    /// Provides a Harmony patch for <see cref="EffectEventExtensions"/>, allowing custom CoreLib effects
    /// to execute after the default <see cref="EffectEventExtensions.PlayEffect"/> method.
    /// <remarks>
    /// This patch intercepts effect event playback and enables CoreLib-managed custom effects
    /// (registered within <see cref="AudioModule.customEffects"/>) to be played automatically
    /// when their effect IDs match those in the event data.
    /// </remarks>
    /// <seealso cref="AudioModule"/>
    /// <seealso cref="HarmonyPatch"/>
    /// <seealso cref="EffectEventExtensions"/>
    public static class EffectEventExtensionsPatch
    {
        #region Harmony Patch: PlayEffect

        /// Executes CoreLib’s custom effect logic if the provided <see cref="EffectEventCD"/> references
        /// a registered custom effect within <see cref="AudioModule.customEffects"/>.
        /// <param name="effectEvent">The event data describing the effect to play.</param>
        /// <param name="callerEntity">The entity responsible for triggering the effect.</param>
        /// <param name="world">The ECS <see cref="World"/> context in which the effect is being executed.</param>
        /// <remarks>
        /// This postfix patch runs after <see cref="EffectEventExtensions.PlayEffect"/> finishes executing.
        /// It checks if the provided effect ID exists in the custom CoreLib dictionary, and if so,
        /// delegates playback to the registered <see cref="IEffect"/> instance.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Automatically executed after EffectEventExtensions.PlayEffect
        /// // if a custom effect is registered for the event’s ID.
        /// </code>
        /// </example>
        [HarmonyPatch(typeof(EffectEventExtensions), nameof(EffectEventExtensions.PlayEffect))]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        public static void OnPlayEffect(EffectEventCD effectEvent, Unity.Entities.Entity callerEntity, World world)
        {
            if (AudioModule.customEffects.TryGetValue(effectEvent.effectID, out var effect))
                effect.PlayEffect(effectEvent, callerEntity, world);
        }

        #endregion
    }
}