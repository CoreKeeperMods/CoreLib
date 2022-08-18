using System;
using Il2CppSystem.Collections.Generic;
using UnhollowerRuntimeLib;
using UnityEngine;
using MusicList = Il2CppSystem.Collections.Generic.List<MusicManager.MusicTrack>;

namespace CoreLib.Submodules.Audio;

public class CustomRosterStore : MonoBehaviour
{
    public Il2CppReferenceField<Dictionary<int, MusicList>> customRosterMusic;
    public Il2CppReferenceField<Dictionary<int, MusicList>> vanillaRosterAddTracksInfos;

    public CustomRosterStore(IntPtr ptr) : base(ptr) { }

    private void Awake()
    {
        customRosterMusic.Set(new Dictionary<int, MusicList>());
        vanillaRosterAddTracksInfos.Set(new Dictionary<int, MusicList>());
    }
}