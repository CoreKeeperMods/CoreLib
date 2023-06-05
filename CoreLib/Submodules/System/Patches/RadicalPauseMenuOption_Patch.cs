using HarmonyLib;
#pragma warning disable CS0618

namespace CoreLib.Submodules.ModSystem.Patches
{
    public class RadicalPauseMenuOption_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RadicalPauseMenuOption_ExitToTitle), nameof(RadicalPauseMenuOption_ExitToTitle.OnActivated))]
        public static void ExitToTitle()
        {
            CoreLibPlugin.Logger.LogInfo("Loading Server Systems!");
            foreach (IPseudoServerSystem serverSystem in SystemModule.serverSystems)
            {
                serverSystem.OnServerStopped();
            }

            CoreLibPlugin.Logger.LogInfo("Loading Client Systems!");
            foreach (IPseudoClientSystem serverSystem in SystemModule.clientSystems)
            {
                serverSystem.OnClientStopped();
            }
        }
    }
}