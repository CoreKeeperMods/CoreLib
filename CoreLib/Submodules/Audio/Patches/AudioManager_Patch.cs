using CoreLib.Util;
using CoreLib.Util.Extensions;
using HarmonyLib;
using Iced.Intel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

namespace CoreLib.Submodules.Audio.Patches;

public static class AudioManager_Patch
{
    [HarmonyPatch(typeof(AudioManager), nameof(AudioManager.IsLegalSfxID))]
    [HarmonyPostfix]
    public static void IsSfxValid(SfxID id, ref bool __result)
    {
        __result = id >= 0 && (int)id < AudioModule.lastFreeSfxId;
    }
    
    [HarmonyPatch(typeof(AudioManager), nameof(AudioManager.AutoLoadAudioFiles))]
    [HarmonyPostfix]
    public static void AutoLoadAudioFiles(AudioManager __instance)
    {
        List<AudioField> customSoundEffect = AudioModule.rosterStore.customSoundEffects.Get();

       if (customSoundEffect.Count == 0) return;
       
       int oldSize = __instance.audioFieldMap.Count;
       int newSize = oldSize + customSoundEffect.Count;

       Il2CppReferenceArray<AudioField> newClips = new Il2CppReferenceArray<AudioField>(newSize);
       for (int i = 0; i < __instance.audioFieldMap.Count; i++)
       {
           newClips[i] = __instance.audioFieldMap[i];
       }

       for (int i = 0; i < customSoundEffect.Count; i++)
       {
           newClips[oldSize + i] = customSoundEffect._items[i];
       }

       __instance.audioFieldMap = newClips;
       CoreLibPlugin.Logger.LogInfo($"Loaded {customSoundEffect.Count} custom sound effects!");
    }

    [HarmonyPatch(typeof(AudioManager), nameof(AudioManager.Init))]
    [HarmonyPostfix]
    public static void Init(AudioManager __instance)
    {
        __instance.reuseMap = new Il2CppReferenceArray<PoolableAudioSource>(__instance.audioFieldMap.Count);
    }
    
    [NativeTranspilerPatch(typeof(AudioManager), nameof(AudioManager.PlayAudioClip))]
    public static System.Collections.Generic.List<Instruction> NativeTranspiler(System.Collections.Generic.List<Instruction> instructions)
    {
        for (int i = 0; i < instructions.Count; i++)
        {
            Instruction instr = instructions[i];
            if (instr.OpCode.Mnemonic == Mnemonic.Cmp && instr.Op0Kind == OpKind.Register && instr.Op1Kind == OpKind.Immediate32)
            {
                instr.SetImmediate(1, 500);
                instructions[i] = instr;
                break;
            }
        }

        return instructions;
    }
    
}