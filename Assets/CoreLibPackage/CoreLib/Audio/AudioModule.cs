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
    /// <summary>
    /// Centralized audio manager for the CoreLib mod.
    /// Provides APIs to register and manage:
    /// <list type="bullet">
    /// <item> Custom music rosters and tracks</item>
    /// <item> Custom sound effects</item>
    /// <item> Custom effect implementations</item>
    /// </list>
    /// Integrates with vanilla systems through patches applied at module initialization.
    /// </summary>
    /// <seealso cref="MusicManager"/>
    /// <seealso cref="AudioField"/>
    /// <seealso cref="IEffect"/>
    public class AudioModule : BaseSubmodule
    {
        #region Fields
        /// <summary>
        /// Human-readable name of the audio module used for logging, diagnostics, and display.
        /// </summary>
        /// <remarks>
        /// This constant identifies the module within the CoreLib framework and is referenced by the module
        /// loader and the module-scoped <see cref="Logger"/> instance. Keep this stable to avoid confusion
        /// in logs and UI elements that display module names.
        /// </remarks>
        /// <seealso cref="Logger"/>
        public new const string Name = "Core Library - Audio";
        
        /// <summary>
        /// Module-scoped logger used for information, warning, and error messages produced by the audio subsystem.
        /// </summary>
        /// <seealso cref="Logger"/>
        internal new static Logger Log = new(Name);

        /// <summary>
        /// Returns the singleton instance of <see cref="AudioModule"/> as managed by <see cref="CoreLibMod"/>.
        /// </summary>
        /// <remarks>
        /// Accessing <see cref="Instance"/> may return <c>null</c> prior to module initialization.
        /// Many public methods call <c>Instance.ThrowIfNotLoaded()</c> to guard against early use.
        /// </remarks>
        /// <seealso cref="CoreLibMod"/>
        internal static AudioModule Instance => CoreLibMod.GetModuleInstance<AudioModule>();

        /// <summary>
        /// Maps custom roster IDs to their in-memory <see cref="MusicManager.MusicRoster"/> instances.
        /// </summary>
        /// <seealso cref="MusicManager.MusicRoster"/>
        internal static Dictionary<int, MusicManager.MusicRoster> CustomRosterMusic = new();

        /// <summary>
        /// Maps vanilla roster IDs that have additional modded tracks.
        /// </summary>
        /// <seealso cref="MusicManager.MusicRoster"/>
        internal static Dictionary<int, MusicManager.MusicRoster> VanillaRosterAddTracksInfos = new();

        /// <summary>
        /// Holds library-defined custom sound effects (<see cref="AudioField"/> instances).
        /// </summary>
        /// <seealso cref="AudioField"/>
        internal static List<AudioField> CustomSoundEffects = new();

        /// <summary>
        /// Maps custom <see cref="EffectID"/> values to their associated effect implementations.
        /// </summary>
        /// <seealso cref="EffectID"/>
        /// <seealso cref="IEffect"/>
        internal static Dictionary<EffectID, IEffect> CustomEffects = new();

        /// <summary>
        /// Numeric identifier of the last built-in vanilla roster. 
        /// Values ≤ these are considered vanilla roster values.
        /// </summary>
        /// <seealso cref="MusicRosterType"/>
        internal static int MaxVanillaRosterId = (int)Enum.GetValues(typeof(MusicRosterType)).Cast<MusicRosterType>().Last();

        /// <summary>
        /// Next free ID to assign to a new custom music roster.
        /// </summary>
        internal static int LastFreeMusicRosterId = MaxVanillaRosterId + 1;

        /// <summary>
        /// Next free ID to assign to a new custom sound effect.
        /// </summary>
        /// <seealso cref="SfxID"/>
        internal static int LastFreeSfxId = (int)Enum.Parse<SfxID>(nameof(SfxID.__max__)) + 1;

        /// <summary>
        /// Next free ID to assign to a new custom effect implementation.
        /// </summary>
        /// <seealso cref="EffectID"/>
        internal static int LastFreeEffectId = (int)Enum.GetValues(typeof(EffectID)).Cast<EffectID>().Last() + 1;

        #endregion

        #region BaseSubmodule Implementation

        /// <summary>
        /// Apply patches required for integrating custom audio with vanilla systems.
        /// </summary>
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

        /// <summary>
        /// Called when the module is loaded.
        /// </summary>
        /// <remarks>
        /// Currently does not perform initialization. Exists for future use.
        /// </remarks>
        internal override void Load() { }

        #endregion

        #region Public Interface

        /// <summary>
        /// Checks whether a roster type is part of vanilla content.
        /// </summary>
        /// <param name="rosterType">Roster type to check.</param>
        /// <returns><c>true</c> if vanilla; otherwise <c>false</c>.</returns>
        /// <seealso cref="MusicRosterType"/>
        public static bool IsVanilla(MusicRosterType rosterType) => (int)rosterType <= MaxVanillaRosterId;

        /// <summary>
        /// Creates and registers a new custom music roster identifier.
        /// </summary>
        /// <returns>New <see cref="MusicRosterType"/> representing the roster.</returns>
        /// <seealso cref="MusicManager.MusicRoster"/>
        public static MusicRosterType AddCustomRoster()
        {
            Instance.ThrowIfNotLoaded();
            int id = LastFreeMusicRosterId++;
            return (MusicRosterType)id;
        }

        /// <summary>
        /// Adds a new music track to the specified roster.
        /// </summary>
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

        /// <summary>
        /// Registers a custom sound effect clip and assigns a new ID.
        /// </summary>
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

            var sfxId = (SfxID)LastFreeSfxId++;
            effect.audioFieldName = $"sfx_{sfxId}";
            CustomSoundEffects.Add(effect);
            return sfxId;
        }

        /// <summary>
        /// Registers a custom effect implementation and returns its unique ID.
        /// </summary>
        /// <param name="effect">Effect implementation to add.</param>
        /// <returns>Assigned <see cref="EffectID"/> or <see cref="EffectID.None"/> if invalid.</returns>
        /// <seealso cref="IEffect"/>
        /// <seealso cref="EffectID"/>
        public static EffectID AddEffect(IEffect effect)
        {
            Instance.ThrowIfNotLoaded();
            if (effect == null) return EffectID.None;

            int effectIndex = LastFreeEffectId++;
            var effectID = (EffectID)effectIndex;
            CustomEffects.Add(effectID, effect);
            return effectID;
        }

        #endregion

        #region Internal / Private Methods

        /// <summary>
        /// Retrieves the roster for a given type, creating it if necessary.
        /// </summary>
        /// <param name="rosterType">Roster type to query.</param>
        /// <returns>Associated <see cref="MusicManager.MusicRoster"/> instance.</returns>
        /// <seealso cref="MusicManager.MusicRoster"/>
        internal static MusicManager.MusicRoster GetRosterTracks(MusicRosterType rosterType)
        {
            int rosterId = (int)rosterType;

            if (IsVanilla(rosterType))
            {
                if (VanillaRosterAddTracksInfos.TryGetValue(rosterId, out var tracks))
                    return tracks;

                var roster = new MusicManager.MusicRoster { tracks = new MusicList() };
                VanillaRosterAddTracksInfos.Add(rosterId, roster);
                return roster;
            }
            else
            {
                if (CustomRosterMusic.TryGetValue(rosterId, out var tracks))
                    return tracks;

                var roster = new MusicManager.MusicRoster { tracks = new MusicList() };
                CustomRosterMusic.Add(rosterId, roster);
                return roster;
            }
        }

        /// <summary>
        /// Registers a custom sound effect from an <see cref="AudioField"/>.
        /// </summary>
        /// <param name="effect">The sound effect field definition.</param>
        /// <returns>Newly assigned <see cref="SfxID"/> or <see cref="SfxID.__illegal__"/>.</returns>
        /// <seealso cref="AudioField"/>
        /// <seealso cref="SfxID"/>
        private static SfxID AddSoundEffect(AudioField effect)
        {
            if (effect == null || effect.audioPlayables.Count <= 0)
                return SfxID.__illegal__;

            CustomSoundEffects.Add(effect);
            int sfxId = LastFreeSfxId++;
            effect.audioFieldName = $"sfx_{sfxId}";
            return (SfxID)sfxId;
        }

        #endregion
    }
}
