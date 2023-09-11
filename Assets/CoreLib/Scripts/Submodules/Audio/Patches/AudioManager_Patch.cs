using System;
using System.Collections.Generic;
using HarmonyLib;

namespace CoreLib.Submodules.Audio.Patches
{
    public static class AudioManager_Patch
    {
        [HarmonyPatch(typeof(AudioManager), nameof(AudioManager.IsLegalSfxID))]
        [HarmonyPostfix]
        public static void IsSfxValid(SfxID id, ref bool __result)
        {
            __result = id >= 0 && (int)id < AudioModule.lastFreeSfxId;
        }
    
        [HarmonyPatch(typeof(AudioManager), "AutoLoadAudioFiles")]
        [HarmonyPostfix]
        public static void AutoLoadAudioFiles(AudioManager __instance)
        {
            List<AudioField> customSoundEffect = AudioModule.customSoundEffects;

            if (customSoundEffect.Count == 0) return;

            var fieldMap = __instance.GetField<AudioField[]>("audioFieldMap");
            int oldSize = fieldMap.Length;
            int newSize = oldSize + customSoundEffect.Count;

            Array.Resize(ref fieldMap,  newSize);

            for (int i = 0; i < customSoundEffect.Count; i++)
            {
                fieldMap[oldSize + i] = customSoundEffect[i];
            }
            
            __instance.SetField("audioFieldMap", fieldMap);

            CoreLibMod.Log.LogInfo($"Loaded {customSoundEffect.Count} custom sound effects!");
        }

        [HarmonyPatch(typeof(AudioManager), nameof(AudioManager.Init))]
        [HarmonyPostfix]
        public static void Init(AudioManager __instance)
        {
            var fieldMap = __instance.GetField<AudioField[]>("audioFieldMap");
            __instance.reuseMap = new PoolableAudioSource[fieldMap.Length];
        }
    }
}