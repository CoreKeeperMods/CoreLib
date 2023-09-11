using HarmonyLib;
namespace CoreLib.Submodules.Audio.Patches
{
    public static class MusicManager_Patch
    {
        [HarmonyPatch(typeof(MusicManager), nameof(MusicManager.Init))]
        [HarmonyPostfix]
        public static void Init(MusicManager __instance)
        {
            foreach (var pair in AudioModule.vanillaRosterAddTracksInfos)
            {
                MusicManager.MusicRoster roster = AudioModule.GetVanillaRoster(__instance, (MusicRosterType)pair.Key);
                if (roster == null)
                {
                    CoreLibMod.Log.LogWarning($"Failed to get roster list for type {((MusicRosterType)pair.Key).ToString()}");
                    continue;
                }

                foreach (MusicManager.MusicTrack track in pair.Value.tracks)
                {
                    roster.tracks.Add(track);
                }
            }
        }

        [HarmonyPatch(typeof(MusicManager), nameof(MusicManager.SetNewMusicPlaylist), typeof(MusicRosterType))]
        [HarmonyPrefix]
        public static bool SetRoster(MusicManager __instance, MusicRosterType m)
        {
            if (AudioModule.IsVanilla(m)) return true;
            if (__instance.currentMusicRosterType != m)
            {
                __instance.currentMusicRosterType = MusicRosterType.DONT_PLAY_MUSIC;
                __instance.musicAudioSource.Stop();
                __instance.musicAudioSource.clip = null;
                __instance.SetField("currentlyPlayingMusicIndex", -1);
                if (!__instance.GetField<bool>("isPaused"))
                {
                    __instance.musicAudioSource.Pause();
                    __instance.SetField("isPaused", true);
                }
            }

            var customRosterMusic = AudioModule.customRosterMusic;

            if (customRosterMusic.ContainsKey((int)m))
            {
                MusicManager.MusicRoster list = customRosterMusic[(int)m];
                if (list != null && list.tracks.Count > 0)
                {
                    __instance.ResumeMusic();
                    if (__instance.GetField<MusicManager.MusicRoster>("currentMusicRoster") != list)
                    {
                        __instance.SetField("currentlyPlayingMusicIndex", -1);
                        __instance.SetField("currentMusicRoster", list);
                    }

                    return false;
                }
            }

            CoreLibMod.Log.LogWarning($"Failed to set music roster to {m}, because there is no such roster or it is empty!");
            return false;
        }
    }
}