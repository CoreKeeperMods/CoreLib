// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: IEffect.cs
// Author: Minepatcher, Limoka
// Created: 2025-11-07
// Description: Defines a contract for handling and playing audio or visual effects
//              within the CoreLib audio submodule using the ECS (Entity Component System) framework.
// ========================================================

using Unity.Entities;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Audio
{
    /// Represents a contract for handling and playing audio or visual effects within the CoreLib framework.
    /// <remarks>
    /// Implementations of this interface provide the logic for initializing and playing
    /// sound or visual effects in response to in-game events. Each implementation can
    /// interpret <see cref="EffectEventCD"/> differently depending on the desired effect behavior.
    /// </remarks>
    /// <seealso cref="EffectEventCD"/>
    /// <seealso cref="Unity.Entities.World"/>
    /// <seealso cref="Unity.Entities.Entity"/>
    public interface IEffect
    {
        /// Plays an audio or visual effect based on the provided event data and game context.
        /// <param name="effectEvent">
        /// The event data containing all necessary information about the effect to be played.
        /// </param>
        /// <param name="callerEntity">
        /// The entity responsible for triggering the effect, typically the in-game actor or object.
        /// </param>
        /// <param name="world">
        /// The ECS <see cref="World"/> instance in which the effect will be executed.
        /// </param>
        /// <remarks>
        /// This method serves as the core entry point for effect playback.
        /// Implementations should safely handle entity validation and world context checks.
        /// </remarks>
        /// <example>
        /// <code>
        /// public class ExplosionEffect: IEffect
        /// {
        ///     public void PlayEffect(EffectEventCD effectEvent, Entity callerEntity, World world)
        ///     {
        ///         // Custom logic for explosion visuals or audio
        ///         Debug.Log("Explosion triggered!");
        ///     }
        /// }
        /// </code>
        /// </example>
        void PlayEffect(EffectEventCD effectEvent, Unity.Entities.Entity callerEntity, World world);
    }
}