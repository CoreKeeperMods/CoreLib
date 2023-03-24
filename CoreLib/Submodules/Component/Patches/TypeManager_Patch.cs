using System.Runtime.InteropServices;
using CoreLib.Components;
using HarmonyLib;
using Il2CppSystem;
using Unity.Entities;

namespace CoreLib.Submodules.ModComponent.Patches
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

                CoreLibPlugin.Logger.LogInfo($"Adding {ComponentModule.customComponentsTypes.Count} custom components!");


                Type[] array = ComponentModule.customComponentsTypes.ToArray();
                GCHandle handle = GCHandle.Alloc(array);
                ComponentModule.AddNewComponentTypes(array);
                handle.Free();
            }
        }
    }
}