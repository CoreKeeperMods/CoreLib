using CoreLib.Util.Extensions;
using HarmonyLib;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Audio.Patches
{
    /// A static class responsible for patching the <c>MusicManager</c> class to extend or modify its functionality
    /// within the game. This class uses the Harmony library for applying patches to methods.
    public static class MusicManagerPatch
    {
        /// Initializes the MusicManager and adds additional tracks to the vanilla music rosters based on predefined configurations.
        /// <param name="__instance">
        /// The instance of the MusicManager being initialized. This is the target object where additional tracks
        /// are added to the appropriate music rosters.
        /// </param>
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
                    BaseSubmodule.Log.LogWarning($"Failed to get roster list for type {((MusicRosterType)pair.Key).ToString()}");
                    continue;
                }

                foreach (var track in pair.Value.tracks)
                {
                    roster.tracks.Add(track);
                }
            }
        }
        
        /// Attempts to set a new music roster for the MusicManager instance.
        /// If the provided music roster type is not vanilla and corresponds to a valid custom music roster,
        /// it pauses the current music, clears the current playlist, and sets the provided roster as the current one.
        /// Logs a warning if the roster cannot be applied.
        /// <param name="__instance">The instance of the MusicManager to modify.</param>
        /// <param name="m">The new music roster type to set.</param>
        [HarmonyPatch(typeof(MusicManager), nameof(MusicManager.SetNewMusicPlaylist), typeof(MusicRosterType))]
        [HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        public static bool SetRoster(MusicManager __instance, MusicRosterType m)
        {
            if (AudioModule.IsVanilla(m)) return true;
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
                    if (__instance.GetValue<MusicManager.MusicRoster>("currentMusicRoster") == list) return false;
                    __instance.SetValue("currentlyPlayingMusicIndex", -1);
                    __instance.SetValue("currentMusicRoster", list);
                    return false;
                }
            }

            BaseSubmodule.Log.LogWarning($"Failed to set music roster to {m}, because there is no such roster or it is empty!");
            return false;
        }
    }
}