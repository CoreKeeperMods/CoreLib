using System;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppSystem.Collections.Generic;
using UnityEngine;
using MusicList = Il2CppSystem.Collections.Generic.List<MusicManager.MusicTrack>;
using Object = Il2CppSystem.Object;

namespace CoreLib.Submodules.Audio;

public class CustomRosterStore : MonoBehaviour
{
    public Il2CppReferenceField<Dictionary<int, MusicList>> customRosterMusic;
    public Il2CppReferenceField<Dictionary<int, MusicList>> vanillaRosterAddTracksInfos;
    public Il2CppReferenceField<List<AudioField>> customSoundEffects;

    public CustomRosterStore(IntPtr ptr) : base(ptr) { }

    private GCHandle customMusicHandle;
    private GCHandle vanillaMusicHandle;
    private GCHandle customSoundEffectsHandle;

    private void Awake()
    {
        Dictionary<int, MusicList> list = new Dictionary<int, MusicList>();
        customMusicHandle = GCHandle.Alloc(list, GCHandleType.Normal);
        customRosterMusic.Set(list);
        
        list = new Dictionary<int, MusicList>();
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