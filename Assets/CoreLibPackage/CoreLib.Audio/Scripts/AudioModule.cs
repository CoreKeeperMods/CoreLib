using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Audio.Patches;
using UnityEngine;
using UnityEngine.AddressableAssets;
using MusicList = System.Collections.Generic.List<MusicManager.MusicTrack>;


namespace CoreLib.Audio
{
    public class AudioModule : BaseSubmodule
    {
        #region Public Interface

        public static bool IsVanilla(MusicRosterType rosterType)
        {
            return (int)rosterType <= maxVanillaRosterId;
        }

        /// <summary>
        /// Define new music roster.
        /// </summary>
        /// <returns>Unique ID of new music roster</returns>
        public static MusicRosterType AddCustomRoster()
        {
            Instance.ThrowIfNotLoaded();
            int id = lastFreeMusicRosterId;
            lastFreeMusicRosterId++;
            return (MusicRosterType)id;
        }

        /// <summary>
        /// Add new music track to music roster
        /// </summary>
        /// <param name="rosterType">Target roster ID</param>
        /// <param name="music">reference to music clip in asset bundle</param>
        /// <param name="intro">reference to intro clip in asset bundle</param>
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
        /// Add custom sound effect
        /// </summary>
        /// <param name="sfxClipPath">Path to AudioClip in mod asset bundle</param>
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

        internal override GameVersion Build => new GameVersion(1, 1, 0, "90bc");
        internal override string Version => "4.0.1";

        internal static AudioModule Instance => CoreLibMod.GetModuleInstance<AudioModule>();
        
        public static Dictionary<int, MusicManager.MusicRoster> customRosterMusic = new Dictionary<int, MusicManager.MusicRoster>();
        public static Dictionary<int, MusicManager.MusicRoster> vanillaRosterAddTracksInfos = new Dictionary<int, MusicManager.MusicRoster>();
        public static List<AudioField> customSoundEffects = new List<AudioField>();

        public static Dictionary<EffectID, IEffect> customEffects = new Dictionary<EffectID, IEffect>();

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

        private const int maxVanillaRosterId = 49;
        private static int lastFreeMusicRosterId = maxVanillaRosterId + 1;
        internal static int lastFreeSfxId;
        
        internal static int lastFreeEffectId = (int)Enum.GetValues(typeof(EffectID)).Cast<EffectID>().Last() + 1;
        
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