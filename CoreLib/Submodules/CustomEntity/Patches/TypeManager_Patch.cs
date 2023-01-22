using System.Runtime.InteropServices;
using CoreLib.Util;
using HarmonyLib;
using Il2CppSystem;
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


                Type[] array = CustomEntityModule.customComponentsTypes.ToArray();
                GCHandle handle = GCHandle.Alloc(array);
                ModComponents.AddNewComponentTypes(array);
                handle.Free();
            }
        }
    }
}