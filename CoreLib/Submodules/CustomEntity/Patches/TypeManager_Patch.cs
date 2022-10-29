using CoreLib.Components;
using CoreLib.Util.Extensions;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Unity.Entities;

namespace CoreLib.Submodules.CustomEntity.Patches
{
    public class TypeManager_Patch
    {
        private static bool done;
        
        [HarmonyPatch(typeof(TypeManager), nameof(TypeManager.Initialize))]
        [HarmonyPostfix]
        public static void ManagerInit()
        {
            if (!done)
            {
                done = true;

                CoreLibPlugin.Logger.LogInfo($"Adding {CustomEntityModule.customComponentsTypes.Count} custom components!");
                ECSExtensions.AddNewComponentTypes(CustomEntityModule.customComponentsTypes.ToArray());
            }
        }
    }
}