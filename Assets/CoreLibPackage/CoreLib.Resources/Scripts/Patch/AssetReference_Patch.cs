using HarmonyLib;
using UnityEngine.AddressableAssets;

namespace CoreLib.ModResources
{
    /// <summary>
    /// A Harmony patch class that extends the functionality of the AssetReference class by intercepting the
    /// RuntimeKeyIsValid method. This patch provides custom validation logic for runtime keys with a specific
    /// protocol format, enabling custom resource management for modded assets.
    /// </summary>
    /// <remarks>
    /// The patch checks if the runtime key of an AssetReference starts with the protocol defined in
    /// ModResourceLocator and forces a valid result for such cases. This integration ensures compatibility
    /// with mod resources handled through custom locators and providers.
    /// </remarks>
    public class AssetReference_Patch
    {
        /// <summary>
        /// Postfix method that modifies the result of the RuntimeKeyIsValid method in the AssetReference class.
        /// Ensures runtime keys starting with the custom protocol defined in ModResourceLocator are treated as valid.
        /// </summary>
        /// <param name="__instance">The AssetReference instance being validated.</param>
        /// <param name="__result">Reference to the boolean result of the RuntimeKeyIsValid method. Modified if the key matches the custom protocol.</param>
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