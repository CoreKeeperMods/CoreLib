using HarmonyLib;
using Unity.Entities;

namespace CoreLib.Submodules.ModSystem.Patches
{
    public static class SceneHandler_Patch
    {
        [HarmonyPatch(typeof(SceneHandler), nameof(SceneHandler.SetSceneHandlerReady))]
        [HarmonyPostfix]
        public static void OnSceneStart()
        {
            World serverWorld = Manager.ecs.ServerWorld;
            if (serverWorld != null)
            {
                CoreLibPlugin.Logger.LogInfo("Loading Server Systems!");
                foreach (IPseudoServerSystem serverSystem in SystemModule.serverSystems)
                {
                    serverSystem.OnServerStarted(serverWorld);
                }
            }

            World clientWorld = Manager.ecs.ClientWorld;
            if (clientWorld != null)
            {
                CoreLibPlugin.Logger.LogInfo("Loading Client Systems!");
                foreach (IPseudoClientSystem serverSystem in SystemModule.clientSystems)
                {
                    serverSystem.OnClientStarted(serverWorld);
                }
            }
        }
    }
}