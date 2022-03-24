
using HarmonyLib;

namespace CoreLib.Patches {

    [HarmonyPatch(typeof(Manager))]
    internal class ManagerPatch {

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        static void AwakePostfix(Manager __instance) {
            CoreLib.Manager = __instance;
            CoreLib.Logger.LogInfo("Patched Manager");
        }
    }
}
