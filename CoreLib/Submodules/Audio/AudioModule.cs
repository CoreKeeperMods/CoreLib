using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using CoreLib.Submodules.Audio.Patches;
using CoreLib.Submodules.ModResources;
using Iced.Intel;
using Il2CppInterop.Common;
using UnityEngine;
using Decoder = Iced.Intel.Decoder;
using MusicList = Il2CppSystem.Collections.Generic.List<MusicManager.MusicTrack>;


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
        CoreLibPlugin.harmony.PatchAll(typeof(AudioManager_Patch));
        
        CoreLibPlugin.Logger.LogInfo("Patching the method!");
        PatchMethod();
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

    public static bool IsVanilla(MusicManager.MusicRosterType rosterType)
    {
        return (int)rosterType <= maxVanillaRosterId;
    }
    
    internal static MusicList GetRosterTracks(MusicManager.MusicRosterType rosterType)
    {
        int rosterId = (int)rosterType;
        if (IsVanilla(rosterType))
        {
            var vanillaRosterAddTracksInfos = rosterStore.vanillaRosterAddTracksInfos.Get();
            if (vanillaRosterAddTracksInfos.ContainsKey(rosterId))
            {
                return vanillaRosterAddTracksInfos[rosterId];
            }

            MusicList list = new MusicList();
            vanillaRosterAddTracksInfos.Add(rosterId, list);
            return list;
        }
        else
        {
            var customRosterMusic = rosterStore.customRosterMusic.Get();
            if (customRosterMusic.ContainsKey(rosterId))
            {
                return customRosterMusic[rosterId];
            }

            MusicList list = new MusicList();
            customRosterMusic.Add(rosterId, list);
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

    public static SfxID AddSoundEffect(string sfxClipPath)
    {
        AudioField effect = new AudioField();
        AudioClip effectClip = ResourcesModule.LoadAsset<AudioClip>(sfxClipPath);

        if (effectClip != null)
        {
            effect.audioPlayables.Add(effectClip);
        }

        return AddSoundEffect(effect);
    }
    
    public static SfxID AddSoundEffect(string[] sfxClipsPaths, AudioField.AudioClipPlayOrder playOrder)
    {
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
    
    private static SfxID AddSoundEffect(AudioField effect)
    {
        if (effect != null && effect.audioPlayables.Count > 0)
        {
            var list = rosterStore.customSoundEffects.Get();
            list.Add(effect);
            
            int sfxId = lastFreeSfxId;
            effect.audioFieldName = $"sfx_{sfxId}";
            lastFreeSfxId++;
            return (SfxID) sfxId;
        }

        return SfxID.__illegal__;
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

    sealed class CodeWriterImpl : CodeWriter {
        readonly List<byte> allBytes = new List<byte>();
        public override void WriteByte(byte value) => allBytes.Add(value);
        public byte[] ToArray() => allBytes.ToArray();
    }
    
    static void Disassemble(byte[] data, ulong ip) {
        var formatter = new NasmFormatter();
        var output = new StringOutput();
        var codeReader = new ByteArrayCodeReader(data);
        var decoder = Decoder.Create(IntPtr.Size * 8, codeReader);
        decoder.IP = ip;
        while (codeReader.CanReadByte) {
            decoder.Decode(out var instr);
            formatter.Format(instr, output);
            CoreLibPlugin.Logger.LogDebug($"{instr.IP:X16} {output.ToStringAndReset()}");
        }
    }
    
    static void PrintInstructions(List<Instruction> instructions) {

        var formatter = new NasmFormatter();
        var output = new StringOutput();
        foreach (Instruction instr in instructions)
        {
            formatter.Format(instr, output);
            CoreLibPlugin.Logger.LogDebug($"{instr.IP:X16} {output.ToStringAndReset()}");
        }
    }

    public static List<Instruction> Transpiler(List<Instruction> instructions)
    {
        for (int i = 0; i < instructions.Count; i++)
        {
            Instruction instr = instructions[i];
            if (instr.OpCode.Mnemonic == Mnemonic.Cmp && instr.Op0Kind == OpKind.Register && instr.Op1Kind == OpKind.Immediate32)
            {
                CoreLibPlugin.Logger.LogDebug($"Found it, value: {instr.Immediate32}");
                instr.SetImmediate(1, 500);
                instructions[i] = instr;
                break;
            }
        }

        return instructions;
    }


    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    private static extern bool VirtualProtect(IntPtr lpAddress, IntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

    public static bool VirtualProtect(IntPtr lpAdress, IntPtr dwSize, ProtectMode flNewProtect, out ProtectMode lpflOldProtect)
    {
        bool result = VirtualProtect(lpAdress, dwSize, (uint)flNewProtect, out uint oldProtect);
        lpflOldProtect = (ProtectMode)oldProtect;
        return result;
    }
    
    internal static unsafe void PatchMethod()
    {
        MethodInfo methodInfo = typeof(AudioManager).GetMethod(nameof(AudioManager.PlayAudioClip));
        var fieldValue = Il2CppInteropUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(methodInfo)?.GetValue(null);
        if (fieldValue == null) return;
        
        IntPtr codeStart = *(IntPtr*)(IntPtr)fieldValue;

        var stream = new UnmanagedMemoryStream((byte*)codeStart, 2000, 2000, FileAccess.ReadWrite);
        var codeReader = new StreamCodeReader(stream);
        var decoder = Decoder.Create(IntPtr.Size * 8, codeReader);
        decoder.IP = (ulong)codeStart;

        List<Instruction> instructions = new List<Instruction>(2000);

        while (true)
        {
            decoder.Decode(out Instruction instr);
            if (decoder.LastError == DecoderError.NoMoreBytes)
            {
                CoreLibPlugin.Logger.LogWarning("Failed to patch method!");
                break;
            }
            instructions.Add(instr);
            
            
            if (instr.FlowControl == FlowControl.Return)
                break;
        }

        IntPtr codeSize = (IntPtr)instructions.Count;
        //PrintInstructions(instructions);

        CoreLibPlugin.Logger.LogDebug("Editing code!");
        instructions = Transpiler(instructions);

        var codeWriter = new CodeWriterImpl();
        
        var block = new InstructionBlock(codeWriter, instructions, (ulong)codeStart);
        // This method can also encode more than one block but that's rarely needed, see above comment.
        bool success = BlockEncoder.TryEncode(decoder.Bitness, block, out var errorMessage, out _);
        if (!success) {
            CoreLibPlugin.Logger.LogError($"Error patching method: {errorMessage}");
            return;
        }
        var newCode = codeWriter.ToArray();
        
        CoreLibPlugin.Logger.LogDebug("Writing code!");

        if (VirtualProtect(codeStart, codeSize, ProtectMode.PAGE_EXECUTE_READWRITE, out ProtectMode oldProtect))
        {
            for (int i = 0; i < newCode.Length; i++)
            {
                IntPtr ptr = codeStart + i;
                Marshal.WriteByte(ptr, newCode[i]);
            }

            VirtualProtect(codeStart, codeSize, oldProtect, out ProtectMode _);
            CoreLibPlugin.Logger.LogDebug("Done, result:");
        }
        else
        {
            CoreLibPlugin.Logger.LogWarning("Failed to change protection!");
        }
        
        //PrintCode(stream, codeStart);
    }

    private static void PrintCode(UnmanagedMemoryStream stream, IntPtr codeStart)
    {
        var codeReader = new StreamCodeReader(stream);
        var decoder = Decoder.Create(IntPtr.Size * 8, codeReader);
        decoder.IP = (ulong)codeStart;
        
        var formatter = new NasmFormatter();
        var output = new StringOutput();
        StringBuilder builder = new StringBuilder();
        stream.Position = 0;
        decoder.IP = (ulong)codeStart;
        while (true)
        {
            decoder.Decode(out Instruction instr);
            if (decoder.LastError == DecoderError.NoMoreBytes)
            {
                break;
            }

            formatter.Format(instr, output);

            builder.Clear();
            builder.Append(instr.IP.ToString("X16"));
            builder.Append(' ');
            builder.Append(output.ToStringAndReset());
            CoreLibPlugin.Logger.LogDebug(builder.ToString());

            if (instr.FlowControl == FlowControl.Return)
                break;
        }
    }
}