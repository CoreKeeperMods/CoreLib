using HarmonyLib;
using MusicList = Il2CppSystem.Collections.Generic.List<MusicManager.MusicTrack>;

namespace CoreLib.Submodules.Audio.Patches;

public static class MusicManager_Patch
{
    [HarmonyPatch(typeof(MusicManager), nameof(MusicManager.Init))]
    [HarmonyPostfix]
    public static void Init(MusicManager __instance)
    {
        var vanillaDict = AudioModule.rosterStore.vanillaRosterAddTracksInfos.Get();
        foreach (var pair in vanillaDict)
        {
            MusicList roster = AudioModule.GetVanillaRoster(__instance, (MusicManager.MusicRosterType)pair.Key);
            if (roster == null)
            {
                CoreLibPlugin.Logger.LogWarning($"Failed to get roster list for type {((MusicManager.MusicRosterType)pair.Key).ToString()}");
                continue;
            }
            
            foreach (MusicManager.MusicTrack track in pair.Value)
            {
                roster.Add(track);
            }
        }
    }

    [HarmonyPatch(typeof(MusicManager), nameof(MusicManager.SetNewMusicPlaylist), typeof(MusicManager.MusicRosterType))]
    [HarmonyPrefix]
    public static bool SetRoster(MusicManager __instance, MusicManager.MusicRosterType m)
    {
        if (AudioModule.IsVanilla(m)) return true;
        if (__instance.currentMusicRosterType != m)
        {
            __instance.currentMusicRosterType = MusicManager.MusicRosterType.DONT_PLAY_MUSIC;
            __instance.musicAudioSource.Stop();
            __instance.musicAudioSource.clip = null;
            __instance.currentlyPlayingMusicIndex = -1;
            if (!__instance.isPaused)
            {
                __instance.musicAudioSource.Pause();
                __instance.isPaused = true;
            }
        }
        
        var customDict = AudioModule.rosterStore.vanillaRosterAddTracksInfos.Get();
        if (customDict.ContainsKey((int)m))
        {
            MusicList list = customDict[(int)m];
            if (list != null && list.Count > 0)
            {
                __instance.ResumeMusic();
                if (__instance.currentMusicRoster != list)
                {
                    __instance.currentlyPlayingMusicIndex = -1;
                    __instance.currentMusicRoster = list;
                }
                return false;
            }
        }

        CoreLibPlugin.Logger.LogWarning($"Failed to set music roster to {m.ToString()}, because there is no such roster or it is empty!");
        return false;
    }
}