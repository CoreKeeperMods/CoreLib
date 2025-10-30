using Unity.Entities;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Audio
{
    /// Represents a contract for handling and playing audio or visual effects within the system.
    public interface IEffect
    {
        /// Plays an audio or visual effect based on the provided effect event, caller entity, and world context.
        /// <param name="effectEvent">The event data containing information about the effect to be played.</param>
        /// <param name="callerEntity">The entity initiating the effect playback.</param>
        /// <param name="world">The ECS world instance within which the effect is to be played.</param>
        void PlayEffect(EffectEventCD effectEvent, Unity.Entities.Entity callerEntity, World world);
    }
}