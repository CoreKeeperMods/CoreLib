using HarmonyLib;
using Unity.Entities;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Audio.Patches
{
    /// Represents a Harmony patch for the EffectEventExtensions class, providing additional behavior
    /// when the PlayEffect method is executed.
    public class EffectEventExtensionsPatch
    {
        /// Executes custom play effect logic if the effect ID exists in the custom effect dictionary.
        /// Designed to be triggered after the PlayEffect method of EffectEventExtensions is invoked.
        /// <param name="effectEvent">The event data associated with the effect being played.</param>
        /// <param name="callerEntity">The entity initiating the effect playback.</param>
        /// <param name="world">The ECS world containing the entities and systems.</param>
        [HarmonyPatch(typeof(EffectEventExtensions), nameof(EffectEventExtensions.PlayEffect))]
        [HarmonyPostfix]
        public static void OnPlayEffect(EffectEventCD effectEvent, Unity.Entities.Entity callerEntity, World world)
        {
            if (!AudioModule.CustomEffects.TryGetValue(effectEvent.effectID, out var effect)) return;

            effect.PlayEffect(effectEvent, callerEntity, world);
        }
    }
}