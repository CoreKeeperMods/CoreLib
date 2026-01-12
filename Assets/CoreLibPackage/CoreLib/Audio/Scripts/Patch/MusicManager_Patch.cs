// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: MusicManager_Patch.cs
// Author: Minepatcher, Limoka
// Created: 2025-11-07
// Description: Provides Harmony patches that extend MusicManager functionality,
//              enabling custom music rosters and dynamic soundtrack integration
//              for CoreLib’s audio system.
// ========================================================

using CoreLib.Util.Extension;
using HarmonyLib;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Audio.Patch
{
    /// Contains Harmony patches for <see cref="MusicManager"/>, extending its ability
    /// to register and handle custom CoreLib music rosters.
    /// <remarks>
    /// This patch allows mods to inject new music tracks into existing rosters
    /// or to define fully custom playlists without overwriting vanilla ones.
    /// It integrates directly into the music initialization and playlist selection logic.
    /// </remarks>
    /// <seealso cref="AudioModule"/>
    /// <seealso cref="HarmonyPatch"/>
    /// <seealso cref="MusicManager"/>
    public static class MusicManagerPatch
    {
        #region Harmony Patch: Init

        /// Initializes the <see cref="MusicManager"/> and appends any additional
        /// tracks from CoreLib-defined custom rosters to the existing vanilla playlists.
        /// <param name="__instance">The instance of <see cref="MusicManager"/> being initialized.</param>
        /// <remarks>
        /// This patch is invoked after the vanilla <see cref="MusicManager.Init"/> method.
        /// It iterates through <see cref="AudioModule.VanillaRosterAddTracksInfos"/> and injects
        /// tracks into matching vanilla music rosters.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Automatically runs when MusicManager.Init is called.
        /// // Adds CoreLib-defined music tracks to vanilla music playlists.
        /// </code>
        /// </example>
        [HarmonyPatch(typeof(MusicManager), nameof(MusicManager.Init))]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        public static void Init(MusicManager __instance)
        {
            foreach (var pair in AudioModule.VanillaRosterAddTracksInfos)
            {
                var roster = __instance.musicRosters.Find(r => r.rosterType == (MusicRosterType)pair.Key);
                if (roster == null)
                {
                    AudioModule.Log.LogWarning(
                        $"Failed to get roster list for type {((MusicRosterType)pair.Key).ToString()}");
                    continue;
                }

                foreach (var track in pair.Value.tracks)
                    roster.tracks.Add(track);
            }
        }

        #endregion

        #region Harmony Patch: SetNewMusicPlaylist

        /// Intercepts and manages playlist changes for <see cref="MusicManager"/>,
        /// enabling CoreLib’s custom music rosters to override vanilla playback behavior.
        /// <param name="__instance">The active <see cref="MusicManager"/> instance.</param>
        /// <param name="m">The <see cref="MusicRosterType"/> to switch to.</param>
        /// <returns>
        /// <c>true</c> to allow vanilla handling; <c>false</c> to suppress default behavior
        /// when a valid custom playlist is found.
        /// </returns>
        /// <remarks>
        /// This prefix patch checks if the requested roster is custom (non-vanilla).  
        /// If valid, it pauses the current track, resets the playlist index, and replaces
        /// the current music roster with the CoreLib custom roster.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Automatically runs when MusicManager.SetNewMusicPlaylist is called.
        /// // Allows CoreLib mods to manage their own custom playlists.
        /// </code>
        /// </example>
        [HarmonyPatch(typeof(MusicManager), nameof(MusicManager.SetNewMusicPlaylist), typeof(MusicRosterType))]
        [HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        public static bool SetRoster(MusicManager __instance, MusicRosterType m)
        {
            if (AudioModule.IsVanilla(m))
                return true;

            if (__instance.currentMusicRosterType != m)
            {
                __instance.currentMusicRosterType = MusicRosterType.DONT_PLAY_MUSIC;
                __instance.musicAudioSource.Stop();
                __instance.musicAudioSource.clip = null;
                __instance.SetValue("currentlyPlayingMusicIndex", -1);

                if (!__instance.GetValue<bool>("isPaused"))
                {
                    __instance.musicAudioSource.Pause();
                    __instance.SetValue("isPaused", true);
                }
            }

            var customRosterMusic = AudioModule.CustomRosterMusic;

            if (customRosterMusic.ContainsKey((int)m))
            {
                var list = customRosterMusic[(int)m];
                if (list != null && list.tracks.Count > 0)
                {
                    __instance.ResumeMusic();

                    if (__instance.GetValue<MusicManager.MusicRoster>("currentMusicRoster") == list)
                        return false;

                    __instance.SetValue("currentlyPlayingMusicIndex", -1);
                    __instance.SetValue("currentMusicRoster", list);
                    return false;
                }
            }

            AudioModule.Log.LogWarning(
                $"Failed to set music roster to {m}, because there is no such roster or it is empty!");
            return false;
        }

        #endregion
    }
}