// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: AudioModule.cs
// Author: Minepatcher, Limoka
// Created: 2025-11-07
// Description: Handles all audio functionality for the Core Library mod,
//              including music rosters, sound effects, and custom effect registration.
// ========================================================

using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Submodule.Audio.Patch;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Logger = CoreLib.Util.Logger;
using MusicList = System.Collections.Generic.List<MusicManager.MusicTrack>;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Audio
{
    /// Centralized audio manager for the CoreLib mod.
    /// Provides APIs to register and manage:
    /// <list type="bullet">
    /// <item> Custom music rosters and tracks</item>
    /// <item> Custom sound effects</item>
    /// <item> Custom effect implementations</item>
    /// </list>
    /// Integrates with vanilla systems through patches applied at module initialization.
    /// <seealso cref="MusicManager"/>
    /// <seealso cref="AudioField"/>
    /// <seealso cref="IEffect"/>
    public class AudioModule : BaseSubmodule
    {
        #region Fields
        /// Human-readable name of the audio module used for logging, diagnostics, and display.
        /// <remarks>
        /// This constant identifies the module within the CoreLib framework and is referenced by the module
        /// loader and the module-scoped <see cref="Logger"/> instance. Keep this stable to avoid confusion
        /// in logs and UI elements that display module names.
        /// </remarks>
        /// <seealso cref="Logger"/>
        public const string NAME = "Core Library - Audio";
        
        /// Module-scoped logger used for information, warning, and error messages produced by the audio subsystem.
        /// <seealso cref="Logger"/>
        internal static Logger log = new(NAME);

        /// Returns the singleton instance of <see cref="AudioModule"/> as managed by <see cref="CoreLibMod"/>.
        /// <remarks>
        /// Accessing <see cref="Instance"/> may return <c>null</c> prior to module initialization.
        /// Many public methods call <c>Instance.ThrowIfNotLoaded()</c> to guard against early use.
        /// </remarks>
        /// <seealso cref="CoreLibMod"/>
        internal static AudioModule Instance => CoreLibMod.GetModuleInstance<AudioModule>();

        /// Maps custom roster IDs to their in-memory <see cref="MusicManager.MusicRoster"/> instances.
        /// <seealso cref="MusicManager.MusicRoster"/>
        internal static Dictionary<int, MusicManager.MusicRoster> customRosterMusic = new();

        /// Maps vanilla roster IDs that have additional modded tracks.
        /// <seealso cref="MusicManager.MusicRoster"/>
        internal static Dictionary<int, MusicManager.MusicRoster> vanillaRosterAddTracksInfos = new();

        /// Holds library-defined custom sound effects (<see cref="AudioField"/> instances).
        /// <seealso cref="AudioField"/>
        internal static List<AudioField> customSoundEffects = new();

        /// Maps custom <see cref="EffectID"/> values to their associated effect implementations.
        /// <seealso cref="EffectID"/>
        /// <seealso cref="IEffect"/>
        internal static Dictionary<EffectID, IEffect> customEffects = new();

        /// Numeric identifier of the last built-in vanilla roster. 
        /// Values ≤ these are considered vanilla roster values.
        /// <seealso cref="MusicRosterType"/>
        internal static int maxVanillaRosterId = (int)Enum.GetValues(typeof(MusicRosterType)).Cast<MusicRosterType>().Last();

        /// Next free ID to assign to a new custom music roster.
        internal static int lastFreeMusicRosterId = maxVanillaRosterId + 1;

        /// Next free ID to assign to a new custom sound effect.
        /// <seealso cref="SfxID"/>
        internal static int lastFreeSfxId = (int)Enum.Parse<SfxID>(nameof(SfxID.__max__)) + 1;

        /// Next free ID to assign to a new custom effect implementation.
        /// <seealso cref="EffectID"/>
        internal static int lastFreeEffectId = (int)Enum.GetValues(typeof(EffectID)).Cast<EffectID>().Last() + 1;

        #endregion

        #region BaseSubmodule Implementation

        /// Apply patches required for integrating custom audio with vanilla systems.
        /// <remarks>
        /// Registers patches for:
        /// <list type="bullet">
        /// <item><see cref="MusicManagerPatch"/></item>
        /// <item><see cref="AudioManagerPatch"/></item>
        /// <item><see cref="EffectEventExtensionsPatch"/></item>
        /// </list>
        /// </remarks>
        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(MusicManagerPatch));
            CoreLibMod.Patch(typeof(AudioManagerPatch));
            CoreLibMod.Patch(typeof(EffectEventExtensionsPatch));
        }

        /// Called when the module is loaded.
        /// <remarks>
        /// Currently does not perform initialization. Exists for future use.
        /// </remarks>
        internal override void Load() { }

        #endregion

        #region Public Interface

        /// Checks whether a roster type is part of vanilla content.
        /// <param name="rosterType">Roster type to check.</param>
        /// <returns><c>true</c> if vanilla; otherwise <c>false</c>.</returns>
        /// <seealso cref="MusicRosterType"/>
        public static bool IsVanilla(MusicRosterType rosterType) => (int)rosterType <= maxVanillaRosterId;

        /// Creates and registers a new custom music roster identifier.
        /// <returns>New <see cref="MusicRosterType"/> representing the roster.</returns>
        /// <seealso cref="MusicManager.MusicRoster"/>
        public static MusicRosterType AddCustomRoster()
        {
            Instance.ThrowIfNotLoaded();
            int id = lastFreeMusicRosterId++;
            return (MusicRosterType)id;
        }

        /// Adds a new music track to the specified roster.
        /// <param name="rosterType">Target roster to modify.</param>
        /// <param name="music">Reference to the main music clip.</param>
        /// <param name="intro">Optional reference to an intro clip.</param>
        /// <seealso cref="AssetReferenceT{TObject}"/>
        /// <seealso cref="MusicManager.MusicTrack"/>
        public static void AddMusicToRoster(
            MusicRosterType rosterType,
            AssetReferenceT<AudioClip> music,
            AssetReferenceT<AudioClip> intro = null)
        {
            Instance.ThrowIfNotLoaded();
            var roster = GetRosterTracks(rosterType);
            var track = new MusicManager.MusicTrack
            {
                trackAssetReference = music,
                introAssetReference = intro ?? new AssetReferenceT<AudioClip>("")
            };
            roster.tracks.Add(track);
        }

        /// Registers a custom sound effect clip and assigns a new ID.
        /// <param name="effectClip">Audio clip to register.</param>
        /// <returns>
        /// Assigned <see cref="SfxID"/> or <see cref="SfxID.__illegal__"/> if invalid.
        /// </returns>
        /// <seealso cref="AudioField"/>
        /// <seealso cref="SfxID"/>
        public static SfxID AddSoundEffect(AudioClip effectClip)
        {
            Instance.ThrowIfNotLoaded();
            if (effectClip == null) return SfxID.__illegal__;

            var effect = new AudioField();
            effect.audioPlayables.Add(effectClip);

            var sfxId = (SfxID)lastFreeSfxId++;
            effect.audioFieldName = $"sfx_{sfxId}";
            customSoundEffects.Add(effect);
            return sfxId;
        }

        /// Registers a custom effect implementation and returns its unique ID.
        /// <param name="effect">Effect implementation to add.</param>
        /// <returns>Assigned <see cref="EffectID"/> or <see cref="EffectID.None"/> if invalid.</returns>
        /// <seealso cref="IEffect"/>
        /// <seealso cref="EffectID"/>
        public static EffectID AddEffect(IEffect effect)
        {
            Instance.ThrowIfNotLoaded();
            if (effect == null) return EffectID.None;

            int effectIndex = lastFreeEffectId++;
            var effectID = (EffectID)effectIndex;
            customEffects.Add(effectID, effect);
            return effectID;
        }

        #endregion

        #region Internal / Private Methods

        /// Retrieves the roster for a given type, creating it if necessary.
        /// <param name="rosterType">Roster type to query.</param>
        /// <returns>Associated <see cref="MusicManager.MusicRoster"/> instance.</returns>
        /// <seealso cref="MusicManager.MusicRoster"/>
        internal static MusicManager.MusicRoster GetRosterTracks(MusicRosterType rosterType)
        {
            int rosterId = (int)rosterType;

            if (IsVanilla(rosterType))
            {
                if (vanillaRosterAddTracksInfos.TryGetValue(rosterId, out var tracks))
                    return tracks;

                var roster = new MusicManager.MusicRoster { tracks = new MusicList() };
                vanillaRosterAddTracksInfos.Add(rosterId, roster);
                return roster;
            }
            else
            {
                if (customRosterMusic.TryGetValue(rosterId, out var tracks))
                    return tracks;

                var roster = new MusicManager.MusicRoster { tracks = new MusicList() };
                customRosterMusic.Add(rosterId, roster);
                return roster;
            }
        }

        /// Registers a custom sound effect from an <see cref="AudioField"/>.
        /// <param name="effect">The sound effect field definition.</param>
        /// <returns>Newly assigned <see cref="SfxID"/> or <see cref="SfxID.__illegal__"/>.</returns>
        /// <seealso cref="AudioField"/>
        /// <seealso cref="SfxID"/>
        private static SfxID AddSoundEffect(AudioField effect)
        {
            if (effect == null || effect.audioPlayables.Count <= 0)
                return SfxID.__illegal__;

            customSoundEffects.Add(effect);
            int sfxId = lastFreeSfxId++;
            effect.audioFieldName = $"sfx_{sfxId}";
            return (SfxID)sfxId;
        }

        #endregion
    }
}
