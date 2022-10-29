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

                CoreLibPlugin.Logger.LogInfo("Adding components!");
                DOTSUtil.AddNewComponentTypes(Il2CppType.Of<CustomCD>(), Il2CppType.Of<CustomCDAuthoring>());
                
                int componentIndex = ECSExtensions.GetTypeIndex<CustomCD>();
                int authoringIndex = ECSExtensions.GetTypeIndex<CustomCDAuthoring>();
                int minecartIndex = ECSExtensions.GetTypeIndex<LevelCD>();
                int minecartAuthoringIndex = ECSExtensions.GetTypeIndex<LevelCDAuthoring>();
                int minecartIndexOrg = TypeManager.GetTypeIndex<LevelCD>();
                int minecartAuthoringIndexOrg = TypeManager.GetTypeIndex<LevelCDAuthoring>();
                
                CoreLibPlugin.Logger.LogInfo($"CustomCD index: {componentIndex}, authoring: {authoringIndex}");
                CoreLibPlugin.Logger.LogInfo($"LevelCD index: {minecartIndex}, authoring: {minecartAuthoringIndex}");
                CoreLibPlugin.Logger.LogInfo($"LevelCD ORG method index: {minecartIndexOrg}, authoring: {minecartAuthoringIndexOrg}");
            }
        }
    }
}