using System;
using System.Collections.Generic;
using CoreLib.Util.Extensions;
using HarmonyLib;

namespace CoreLib.Audio.Patches
{
    public static class AudioManager_Patch
    {
        [HarmonyPatch(typeof(AudioManager), nameof(AudioManager.IsLegalSfxID))]
        [HarmonyPostfix]
        public static void IsSfxValid(SfxID id, ref bool __result)
        {
            __result = id >= 0 && (int)id < AudioModule.lastFreeSfxId;
        }
        
        public static void AutoLoadAudioFiles(AudioManager __instance)
        {
            List<AudioField> customSoundEffect = AudioModule.customSoundEffects;

            if (customSoundEffect.Count == 0) return;

            var fieldMap = __instance.GetValue<AudioField[]>("audioFieldMap");
            int oldSize = fieldMap.Length;
            int newSize = oldSize + customSoundEffect.Count;

            Array.Resize(ref fieldMap,  newSize);

            for (int i = 0; i < customSoundEffect.Count; i++)
            {
                fieldMap[oldSize + i] = customSoundEffect[i];
            }
            
            __instance.SetValue("audioFieldMap", fieldMap);
            __instance.reuseMap = new PoolableAudioSource[fieldMap.Length];

            CoreLibMod.Log.LogInfo($"Loaded {customSoundEffect.Count} custom sound effects!");
        }

        [HarmonyPatch(typeof(AudioManager), "Initialized", MethodType.Setter)]
        [HarmonyPostfix]
        public static void OnSetInitialized(AudioManager __instance)
        {
            AutoLoadAudioFiles(__instance);
        }
    }
}