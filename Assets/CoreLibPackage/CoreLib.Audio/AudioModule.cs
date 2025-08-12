using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Audio.Patches;
using UnityEngine;
using UnityEngine.AddressableAssets;
using MusicList = System.Collections.Generic.List<MusicManager.MusicTrack>;


namespace CoreLib.Audio
{
    /// <summary>
    /// Manages audio configurations, custom music rosters, sound effects, and custom effects within the system.
    /// Provides functionality to add custom rosters, tracks, sound effects, and effect implementations,
    /// while maintaining support for existing vanilla systems.
    /// </summary>
    public class AudioModule : BaseSubmodule
    {
        #region Public Interface

        /// <summary>
        /// Determines whether the specified music roster type is a vanilla roster type.
        /// </summary>
        /// <param name="rosterType">The music roster type to check.</param>
        /// <returns>True if the roster is a vanilla type; otherwise, false.</returns>
        public static bool IsVanilla(MusicRosterType rosterType)
        {
            return (int)rosterType <= maxVanillaRosterId;
        }

        /// <summary>
        /// Adds a new custom music roster to the system.
        /// </summary>
        /// <returns>A unique identifier representing the newly created custom music roster.</returns>
        public static MusicRosterType AddCustomRoster()
        {
            Instance.ThrowIfNotLoaded();
            int id = lastFreeMusicRosterId;
            lastFreeMusicRosterId++;
            return (MusicRosterType)id;
        }

        /// <summary>
        /// Adds a new music track to the specified music roster.
        /// </summary>
        /// <param name="rosterType">The target music roster to which the track will be added.</param>
        /// <param name="music">A reference to the music clip asset to be added.</param>
        /// <param name="intro">An optional reference to the intro clip asset for the music track.</param>
        public static void AddMusicToRoster(
            MusicRosterType rosterType,
            AssetReferenceT<AudioClip> music,
            AssetReferenceT<AudioClip> intro = null)
        {
            Instance.ThrowIfNotLoaded();
            MusicManager.MusicRoster roster = GetRosterTracks(rosterType);
            MusicManager.MusicTrack track = new MusicManager.MusicTrack();

            intro ??= new AssetReferenceT<AudioClip>("");
            
            track.trackAssetReference = music;
            track.introAssetReference = intro;

            roster.tracks.Add(track);
        }

        /// <summary>
        /// Adds a custom sound effect to the audio system.
        /// </summary>
        /// <param name="effectClip">The AudioClip representing the sound effect to add.</param>
        /// <returns>The ID of the added custom sound effect.</returns>
        public static SfxID AddSoundEffect(AudioClip effectClip)
        {
            Instance.ThrowIfNotLoaded();
            AudioField effect = new AudioField();

            if (effectClip != null)
            {
                effect.audioPlayables.Add(effectClip);
            }

            return AddSoundEffect(effect);
        }

        /// <summary>
        /// Adds a custom effect to the system and assigns it a unique identifier.
        /// </summary>
        /// <param name="effect">The effect to be added, implementing the <see cref="IEffect"/> interface.</param>
        /// <returns>An <see cref="EffectID"/> representing the unique identifier for the added effect. If the effect is null, returns <see cref="EffectID.None"/>.</returns>
        public static EffectID AddEffect(IEffect effect)
        {
            Instance.ThrowIfNotLoaded();
            if (effect == null) return EffectID.None;
            
            int effectIndex = lastFreeEffectId;
            EffectID effectID = (EffectID)effectIndex;
            
            customEffects.Add(effectID, effect);
            lastFreeEffectId++;

            return effectID;
        }

        #endregion

        #region Private Implementation

        /// Gets the current build version of the module as a `GameVersion` instance.
        /// This property represents the release number, major version, minor version, and build hash
        /// that corresponds to the specific build of the module.
        internal override GameVersion Build => new GameVersion(1, 1, 0, "90bc");

        /// Gets the version of the AudioModule as a string.
        /// This property specifies the version identifier for the AudioModule, denoting major, minor, and patch level information.
        /// It is intended to help determine compatibility and updates specific to this module.
        internal override string Version => "4.0.1";

        /// Provides access to the singleton instance of the `AudioModule`.
        /// This property ensures that only one instance of the `AudioModule` is created and used
        /// throughout the application, enabling centralized management of audio features, such as
        /// custom music rosters, sound effects, and other audio-related functionality.
        internal static AudioModule Instance => CoreLibMod.GetModuleInstance<AudioModule>();

        /// Stores custom music rosters mapped by their respective roster IDs.
        /// This dictionary allows for the registration and management of user-defined music rosters,
        /// where each entry represents a unique roster that contains associated tracks and configurations.
        /// The custom rosters extend the functionality of the vanilla music management system by supporting
        /// additional, user-specified music collections.
        public static Dictionary<int, MusicManager.MusicRoster> customRosterMusic = new Dictionary<int, MusicManager.MusicRoster>();

        /// Stores information about vanilla music rosters with additional tracks added dynamically.
        /// Acts as a mapping between unique roster identifiers (keys) and their associated `MusicManager.MusicRoster` instances.
        /// This field is primarily used to integrate and extend vanilla music systems by adding new tracks without modifying core functionality.
        public static Dictionary<int, MusicManager.MusicRoster> vanillaRosterAddTracksInfos = new Dictionary<int, MusicManager.MusicRoster>();

        /// Represents a collection of custom audio fields that define custom sound effects
        /// added to the audio system. Each entry in the list corresponds to a unique
        /// sound effect and is utilized by the system to dynamically load and manage
        /// custom audio assets at runtime.
        public static List<AudioField> customSoundEffects = new List<AudioField>();

        /// Stores a mapping of custom effect identifiers to their respective effect implementations.
        /// This dictionary is used to register and manage custom effects added to the audio system,
        /// enabling extended functionality beyond the base system's capabilities.
        public static Dictionary<EffectID, IEffect> customEffects = new Dictionary<EffectID, IEffect>();

        /// <summary>
        /// Configures and applies the necessary hooks or patches for this module.
        /// </summary>
        /// <remarks>
        /// This method overrides the base behavior to apply specific patches related to
        /// audio management functionalities, ensuring that required modifications are
        /// active in the execution environment.
        /// </remarks>
        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(MusicManager_Patch));
            CoreLibMod.Patch(typeof(AudioManager_Patch));
            CoreLibMod.Patch(typeof(EffectEventExtensions_Patch));
        }
        
        internal override void Load()
        {
            lastFreeSfxId = (int)(SfxID)Enum.Parse(typeof(SfxID), nameof(SfxID.__max__));
            CoreLibMod.Log.LogInfo($"Max Sfx ID: {lastFreeSfxId}");
        }

        /// Represents the maximum identifier value that corresponds to vanilla music roster types.
        /// Music roster types with an identifier less than or equal to this value are considered
        /// part of the game's vanilla content. This constant is used to differentiate between
        /// vanilla and custom music rosters.
        private const int maxVanillaRosterId = 49;

        /// Represents the last assigned identifier for a custom music roster.
        /// This variable is incremented whenever a new custom music roster is added,
        /// ensuring unique identifiers for each newly created roster type.
        private static int lastFreeMusicRosterId = maxVanillaRosterId + 1;

        /// Represents the last unused sound effect ID in the system.
        /// This variable is used to assign unique identifiers to custom sound effects, ensuring no collisions with pre-existing IDs.
        /// It is incremented each time a new sound effect is added to guarantee uniqueness.
        /// Initially, it is set to the maximum predefined sound effect ID (`SfxID.__max__`).
        internal static int lastFreeSfxId;
        
        internal static int lastFreeEffectId = (int)Enum.GetValues(typeof(EffectID)).Cast<EffectID>().Last() + 1;

        /// <summary>
        /// Retrieves the music roster associated with the specified music roster type.
        /// </summary>
        /// <param name="rosterType">The type of the music roster to retrieve.</param>
        /// <returns>A <c>MusicManager.MusicRoster</c> object containing the tracks for the specified roster type.</returns>
        internal static MusicManager.MusicRoster GetRosterTracks(MusicRosterType rosterType)
        {
            int rosterId = (int)rosterType;
            if (IsVanilla(rosterType))
            {
                if (vanillaRosterAddTracksInfos.ContainsKey(rosterId))
                {
                    return vanillaRosterAddTracksInfos[rosterId];
                }

                MusicManager.MusicRoster roster = new MusicManager.MusicRoster
                {
                    tracks = new MusicList()
                };
                vanillaRosterAddTracksInfos.Add(rosterId, roster);
                return roster;
            }
            else
            {
                if (customRosterMusic.ContainsKey(rosterId))
                {
                    return customRosterMusic[rosterId];
                }

                MusicManager.MusicRoster roster = new MusicManager.MusicRoster
                {
                    tracks = new MusicList()
                };
                customRosterMusic.Add(rosterId, roster);
                return roster;
            }
        }

        /// <summary>
        /// Adds a new sound effect to the audio system.
        /// </summary>
        /// <param name="effectClip">The audio clip to be used for the sound effect.</param>
        /// <returns>An identifier for the newly added sound effect.</returns>
        private static SfxID AddSoundEffect(AudioField effect)
        {
            if (effect != null && effect.audioPlayables.Count > 0)
            {
                customSoundEffects.Add(effect);

                int sfxId = lastFreeSfxId;
                effect.audioFieldName = $"sfx_{sfxId}";
                lastFreeSfxId++;
                return (SfxID)sfxId;
            }

            return SfxID.__illegal__;
        }

        /// <summary>
        /// Retrieves the vanilla music roster of a specified type from the given music manager.
        /// </summary>
        /// <param name="manager">The music manager containing the music rosters.</param>
        /// <param name="rosterType">The type of the vanilla music roster to retrieve.</param>
        /// <returns>The music roster of the specified type if found; otherwise, null.</returns>
        internal static MusicManager.MusicRoster GetVanillaRoster(MusicManager manager, MusicRosterType rosterType)
        {
            foreach (MusicManager.MusicRoster roster in manager.musicRosters)
            {
                if (roster.rosterType == rosterType)
                {
                    return roster;
                }
            }

            return null;
        }

        #endregion
    }
}