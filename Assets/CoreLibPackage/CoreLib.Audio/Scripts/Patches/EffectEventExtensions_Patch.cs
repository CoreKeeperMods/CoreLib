using HarmonyLib;
using Unity.Entities;

namespace CoreLib.Audio.Patches
{
    /// <summary>
    /// Represents a Harmony patch for the EffectEventExtensions class, providing additional behavior
    /// when the PlayEffect method is executed.
    /// </summary>
    public class EffectEventExtensions_Patch
    {
        /// Executes custom play effect logic if the effect ID exists in the custom effects dictionary.
        /// Designed to be triggered after the PlayEffect method of EffectEventExtensions is invoked.
        /// <param name="effectEvent">The event data associated with the effect being played.</param>
        /// <param name="callerEntity">The entity initiating the effect playback.</param>
        /// <param name="world">The ECS world containing the entities and systems.</param>
        [HarmonyPatch(typeof(EffectEventExtensions), nameof(EffectEventExtensions.PlayEffect))]
        [HarmonyPostfix]
        public static void OnPlayEffect(EffectEventCD effectEvent, Entity callerEntity, World world)
        {
            if (!AudioModule.customEffects.ContainsKey(effectEvent.effectID)) return;
            
            var effect = AudioModule.customEffects[effectEvent.effectID];
            effect.PlayEffect(effectEvent, callerEntity, world);
        }
    }
}