using System;
using System.Reflection;
using CoreLib.Submodules.Audio.Patches;
using CoreLib.Submodules.ModResources;
using CoreLib.Util;
using UnityEngine;
using MusicList = Il2CppSystem.Collections.Generic.List<MusicManager.MusicTrack>;


namespace CoreLib.Submodules.Audio;

[CoreLibSubmodule(Dependencies = new[] { typeof(ResourcesModule) })]
public static class AudioModule
{
    #region Public Interface

    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
    public static bool Loaded
    {
        get => _loaded;
        internal set => _loaded = value;
    }

    public static bool IsVanilla(MusicManager.MusicRosterType rosterType)
    {
        return (int)rosterType <= maxVanillaRosterId;
    }

    /// <summary>
    /// Define new music roster.
    /// </summary>
    /// <returns>Unique ID of new music roster</returns>
    public static MusicManager.MusicRosterType AddCustomRoster()
    {
        ThrowIfNotLoaded();
        int id = lastFreeMusicRosterId;
        lastFreeMusicRosterId++;
        return (MusicManager.MusicRosterType)id;
    }

    /// <summary>
    /// Add new music track to music roster
    /// </summary>
    /// <param name="rosterType">Target roster ID</param>
    /// <param name="musicPath">path to music clip in asset bundle</param>
    /// <param name="introPath">path to intro clip in asset bundle</param>
    public static void AddMusicToRoster(MusicManager.MusicRosterType rosterType, string musicPath, string introPath = "")
    {
        ThrowIfNotLoaded();
        MusicManager.MusicRoster roster = GetRosterTracks(rosterType);
        MusicManager.MusicTrack track = new MusicManager.MusicTrack();

        track.track = ResourcesModule.LoadAsset<AudioClip>(musicPath);
        if (!introPath.Equals(""))
        {
            track.optionalIntro = ResourcesModule.LoadAsset<AudioClip>(introPath);
        }

        roster.tracks.Add(track);
    }

    /// <summary>
    /// Add custom sound effect
    /// </summary>
    /// <param name="sfxClipPath">Path to AudioClip in mod asset bundle</param>
    public static SfxID AddSoundEffect(string sfxClipPath)
    {
        ThrowIfNotLoaded();
        AudioField effect = new AudioField();
        AudioClip effectClip = ResourcesModule.LoadAsset<AudioClip>(sfxClipPath);

        if (effectClip != null)
        {
            effect.audioPlayables.Add(effectClip);
        }

        return AddSoundEffect(effect);
    }

    /// <summary>
    /// Add custom sound effect with multiple AudioClips
    /// </summary>
    /// <param name="sfxClipsPaths">Paths to AudioClips in mod asset bundle</param>
    /// <param name="playOrder">AudioClip play order</param>
    public static SfxID AddSoundEffect(string[] sfxClipsPaths, AudioField.AudioClipPlayOrder playOrder)
    {
        ThrowIfNotLoaded();
        AudioField effect = new AudioField();
        effect.audioClipPlayOrder = playOrder;
        foreach (string sfxClipPath in sfxClipsPaths)
        {
            AudioClip effectClip = ResourcesModule.LoadAsset<AudioClip>(sfxClipPath);

            if (effectClip != null)
            {
                effect.audioPlayables.Add(effectClip);
            }
        }

        return AddSoundEffect(effect);
    }

    #endregion

    #region Private Implementation

    private static bool _loaded;


    [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
    internal static void SetHooks()
    {
        CoreLibPlugin.harmony.PatchAll(typeof(MusicManager_Patch));
        CoreLibPlugin.harmony.PatchAll(typeof(AudioManager_Patch));

        CoreLibPlugin.Logger.LogInfo("Patching the method!");
        NativeTranspiler.PatchAll(typeof(AudioManager_Patch));
    }

    [CoreLibSubmoduleInit(Stage = InitStage.Load)]
    internal static void Load()
    {
        rosterStore = CoreLibPlugin.Instance.AddComponent<CustomRosterStore>();
        lastFreeSfxId = (int)Enum.Parse<SfxID>(nameof(SfxID.__max__));
        CoreLibPlugin.Logger.LogInfo($"Max Sfx ID: {lastFreeSfxId}");
    }

    internal static void ThrowIfNotLoaded()
    {
        if (!Loaded)
        {
            Type submoduleType = MethodBase.GetCurrentMethod().DeclaringType;
            string message = $"{submoduleType.Name} is not loaded. Please use [{nameof(CoreLibSubmoduleDependency)}(nameof({submoduleType.Name})]";
            throw new InvalidOperationException(message);
        }
    }

    private const int maxVanillaRosterId = 49;
    private static int lastFreeMusicRosterId = maxVanillaRosterId + 1;
    internal static int lastFreeSfxId;

    internal static CustomRosterStore rosterStore;

    internal static MusicManager.MusicRoster GetRosterTracks(MusicManager.MusicRosterType rosterType)
    {
        int rosterId = (int)rosterType;
        if (IsVanilla(rosterType))
        {
            var vanillaRosterAddTracksInfos = rosterStore.vanillaRosterAddTracksInfos.Get();
            if (vanillaRosterAddTracksInfos.ContainsKey(rosterId))
            {
                return vanillaRosterAddTracksInfos[rosterId];
            }

            MusicManager.MusicRoster roster = new MusicManager.MusicRoster();
            vanillaRosterAddTracksInfos.Add(rosterId, roster);
            return roster;
        }
        else
        {
            var customRosterMusic = rosterStore.customRosterMusic.Get();
            if (customRosterMusic.ContainsKey(rosterId))
            {
                return customRosterMusic[rosterId];
            }

            MusicManager.MusicRoster roster = new MusicManager.MusicRoster();
            customRosterMusic.Add(rosterId, roster);
            return roster;
        }
    }

    private static SfxID AddSoundEffect(AudioField effect)
    {
        if (effect != null && effect.audioPlayables.Count > 0)
        {
            var list = rosterStore.customSoundEffects.Get();
            list.Add(effect);

            int sfxId = lastFreeSfxId;
            effect.audioFieldName = $"sfx_{sfxId}";
            lastFreeSfxId++;
            return (SfxID)sfxId;
        }

        return SfxID.__illegal__;
    }

    internal static MusicManager.MusicRoster GetVanillaRoster(MusicManager manager, MusicManager.MusicRosterType rosterType)
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