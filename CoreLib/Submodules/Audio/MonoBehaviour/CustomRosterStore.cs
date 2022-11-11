using System;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppSystem.Collections.Generic;
using UnityEngine;
using MusicList = Il2CppSystem.Collections.Generic.List<MusicManager.MusicTrack>;

namespace CoreLib.Submodules.Audio;

public class CustomRosterStore : MonoBehaviour
{
    public Il2CppReferenceField<Dictionary<int, MusicManager.MusicRoster>> customRosterMusic;
    public Il2CppReferenceField<Dictionary<int, MusicManager.MusicRoster>> vanillaRosterAddTracksInfos;
    public Il2CppReferenceField<List<AudioField>> customSoundEffects;

    public CustomRosterStore(IntPtr ptr) : base(ptr) { }

    private GCHandle customMusicHandle;
    private GCHandle vanillaMusicHandle;
    private GCHandle customSoundEffectsHandle;

    private void Awake()
    {
        Dictionary<int, MusicManager.MusicRoster> list = new Dictionary<int, MusicManager.MusicRoster>();
        customMusicHandle = GCHandle.Alloc(list, GCHandleType.Normal);
        customRosterMusic.Set(list);

        list = new Dictionary<int, MusicManager.MusicRoster>();
        vanillaMusicHandle = GCHandle.Alloc(list, GCHandleType.Normal);
        vanillaRosterAddTracksInfos.Set(list);

        List<AudioField> sfxList = new List<AudioField>();
        customSoundEffectsHandle = GCHandle.Alloc(sfxList, GCHandleType.Normal);
        customSoundEffects.Set(sfxList);

        CoreLibPlugin.Logger.LogDebug("Custom Music Roster Store is initialized");
    }

    private void OnDestroy()
    {
        customMusicHandle.Free();
        vanillaMusicHandle.Free();
        customSoundEffectsHandle.Free();
    }
}