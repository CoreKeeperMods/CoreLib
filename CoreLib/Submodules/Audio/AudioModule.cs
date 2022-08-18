﻿using System;
using System.Collections.Generic;
using System.Reflection;
using CoreLib.Submodules.Audio.Patches;
using CoreLib.Submodules.CustomEntity.Patches;
using CoreLib.Submodules.ModResources;
using UnityEngine;
using MusicList = Il2CppSystem.Collections.Generic.List<MusicManager.MusicTrack>;
using Object = UnityEngine.Object;


namespace CoreLib.Submodules.Audio;

[CoreLibSubmodule(Dependencies = new []{typeof(ResourcesModule)})]
public static class AudioModule
{
    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
    public static bool Loaded
    {
        get => _loaded;
        internal set => _loaded = value;
    }

    private static bool _loaded;


    [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
    internal static void SetHooks()
    {
        CoreLibPlugin.harmony.PatchAll(typeof(MusicManager_Patch));
        rosterStore = CoreLibPlugin.Instance.AddComponent<CustomRosterStore>();
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

    internal static CustomRosterStore rosterStore;

    public static bool IsVanilla(MusicManager.MusicRosterType rosterType)
    {
        return (int)rosterType <= maxVanillaRosterId;
    }
    
    internal static MusicList GetRosterTracks(MusicManager.MusicRosterType rosterType)
    {
        int rosterId = (int)rosterType;
        if (IsVanilla(rosterType))
        {
            var vanillaDict = rosterStore.vanillaRosterAddTracksInfos.Get();
            
            if (vanillaDict.ContainsKey(rosterId))
            {
                return vanillaDict[rosterId];
            }

            MusicList list = new MusicList();
            vanillaDict.Add(rosterId, list);
            return list;
        }
        else
        {
            var customDict = rosterStore.customRosterMusic.Get();
            if (customDict.ContainsKey(rosterId))
            {
                return customDict[rosterId];
            }

            MusicList list = new MusicList();
            customDict.Add(rosterId, list);
            return list;
        }
    }
    
    /// <summary>
    /// Define new music roster.
    /// </summary>
    /// <returns>Unique ID of new music roster</returns>
    public static MusicManager.MusicRosterType AddMusicRoster()
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
    public static void AddRosterMusic(MusicManager.MusicRosterType rosterType, string musicPath, string introPath = "")
    {
        ThrowIfNotLoaded();
        MusicList list = GetRosterTracks(rosterType);
        
        MusicManager.MusicTrack track = new MusicManager.MusicTrack();
        
        track.track = ResourcesModule.LoadAsset<AudioClip>(musicPath);
        if (!introPath.Equals(""))
        {
            track.optionalIntro = ResourcesModule.LoadAsset<AudioClip>(introPath);
        }
        
        list.Add(track);
    }

    internal static MusicList GetVanillaRoster(MusicManager manager, MusicManager.MusicRosterType rosterType)
    {
        switch (rosterType)
        {
            case MusicManager.MusicRosterType.DEFAULT:
                return manager.defaultMusic;
            case MusicManager.MusicRosterType.TITLE:
                return manager.titleMusic;
            case MusicManager.MusicRosterType.INTRO:
                return manager.introMusic;
            case MusicManager.MusicRosterType.MAIN:
                return manager.mainMusic;
            case MusicManager.MusicRosterType.BOSS:
                return manager.bossMusic;
            case MusicManager.MusicRosterType.DONT_PLAY_MUSIC:
                return null;
            case MusicManager.MusicRosterType.CREDITS:
                return manager.creditsMusic;
            case MusicManager.MusicRosterType.MYSTERY:
                return manager.mysteryMusic;
            case MusicManager.MusicRosterType.EDITOR:
                return manager.editorMusic;
            case MusicManager.MusicRosterType.HOME_BASE:
                return manager.homeBaseMusic;
            case MusicManager.MusicRosterType.STONE_BIOME:
                return manager.stoneBiomeMusic;
            case MusicManager.MusicRosterType.LARVA_BIOME:
                return manager.larvaBiomeMusic;
            case MusicManager.MusicRosterType.NATURE_BIOME:
                return manager.natureBiomeMusic;
            case MusicManager.MusicRosterType.SLIME_BIOME:
                return manager.slimeBiomeMusic;
            case MusicManager.MusicRosterType.SEA_BIOME:
                return manager.seaBiomeMusic;
            case MusicManager.MusicRosterType.MOLD_DUNGEON:
                return manager.moldDungeonMusic;
            case MusicManager.MusicRosterType.CITY_DUNGEON:
                return manager.cityDungeonMusic;
            default:
                return null;
        }
    }
    
    
}