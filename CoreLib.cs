using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;

namespace CoreLib {

    [BepInPlugin(GUID, NAME, VERSION)]
    public class CoreLib : BasePlugin {

        public const string GUID = "com.le4fless.corelib";
        public const string NAME = "CoreLib";
        public const string VERSION = "0.1.0";

        internal static CoreLib Instance { get; private set; }
        internal static ManualLogSource Logger { get; private set; }
        internal static Manager Manager { get; set; }

        public override void Load() {
            Instance = this;
            Logger = base.Log;

            var harmony = new Harmony("com.le4fless.corelib");
            harmony.PatchAll();

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} is loaded!");
        }
    }
}
