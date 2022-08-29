using System;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using UnityEngine;
using Il2CppSystem.Collections.Generic;

#if IL2CPP

namespace CoreLib.UnityComponents
{
    public class TestComponent : MonoBehaviour
    {
        public Il2CppReferenceField<Dictionary<int, List<MusicManager.MusicTrack>>> customRosterMusic;
        public Il2CppReferenceField<Dictionary<int, List<MusicManager.MusicTrack>>> vanillaRosterAddTracksInfos;
        public Il2CppReferenceField<List<AudioField>> customSoundEffects;

        public TestComponent(IntPtr ptr) : base(ptr) { }

        private GCHandle customMusicHandle;
        private GCHandle vanillaMusicHandle;
        private GCHandle customSoundEffectsHandle;

        private void Awake()
        {
            Dictionary<int, List<MusicManager.MusicTrack>> list = new Dictionary<int, List<MusicManager.MusicTrack>>();
            customMusicHandle = GCHandle.Alloc(list, GCHandleType.Normal);
            customRosterMusic.Set(list);

            list = new Dictionary<int, List<MusicManager.MusicTrack>>();
            vanillaMusicHandle = GCHandle.Alloc(list, GCHandleType.Normal);
            vanillaRosterAddTracksInfos.Set(list);

            List<AudioField> sfxList = new List<AudioField>();
            customSoundEffectsHandle = GCHandle.Alloc(sfxList, GCHandleType.Normal);
            customSoundEffects.Set(sfxList);
        }

        public int OnDestroy()
        {
            customMusicHandle.Free();
            vanillaMusicHandle.Free();
            customSoundEffectsHandle.Free();
            return 0;
        }
    }
}
#endif