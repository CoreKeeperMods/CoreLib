using HarmonyLib;
using Unity.Entities;

namespace CoreLib.Audio.Patches
{
    public class EffectEventExtensions_Patch
    {
        [HarmonyPatch(typeof(EffectEventExtensions), nameof(EffectEventExtensions.PlayEffect))]
        [HarmonyPostfix]
        public static void OnPlayEffect(EffectEventCD effectEvent, Entity callerEntity, World world)
        {
            if (!AudioModule.customEffects.ContainsKey(effectEvent.effectID)) return;
            
            var effect = AudioModule.customEffects[effectEvent.effectID];
            effect.PlayEffect(effectEvent, callerEntity, world);
        }
    }
}