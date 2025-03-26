using HarmonyLib;
using UnityEngine.AddressableAssets;

namespace CoreLib.ModResources
{
    public class AssetReference_Patch
    {
        [HarmonyPatch(typeof(AssetReference), nameof(AssetReference.RuntimeKeyIsValid))]
        [HarmonyPostfix]
        public static void OnRuntimeKeyIsValid(AssetReference __instance, ref bool __result)
        {
            var text = __instance.RuntimeKey.ToString();

            if (text.StartsWith(ModResourceLocator.PROTOCOL))
            {
                __result = true;
            }
        }
    }
}