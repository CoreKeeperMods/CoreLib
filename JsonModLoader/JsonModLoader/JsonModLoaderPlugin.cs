using System.IO;
using System.Reflection;
using System.Text.Json;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using CoreLib;
using CoreLib.Submodules.JsonLoader;

namespace JsonModLoader
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(CoreLibPlugin.GUID)]
    [CoreLibSubmoduleDependency(nameof(JsonLoaderModule))]
    public class JsonModLoaderPlugin : BasePlugin
    {
        public static ManualLogSource logger;

        public override void Load()
        {
            // Plugin startup logic
            logger = Log;

            Assembly assembly = Assembly.GetExecutingAssembly();
            
            string currentDir = Path.GetDirectoryName(assembly.Location);
            string pluginsDir = Path.GetRelativePath(currentDir, "../");

            LoadJsonMods(pluginsDir, assembly);
            
            logger.LogInfo($"Finished loading all JSON mods");
        }

        private static void LoadJsonMods(string pluginsDir, Assembly assembly)
        {
            foreach (string directory in Directory.EnumerateDirectories(pluginsDir))
            {
                string myAssemblyPath = Path.Combine(directory, $"{assembly.GetName().Name}.dll");
                logger.LogInfo($"Checking dir: {directory}, filename: {myAssemblyPath}");
                if (File.Exists(myAssemblyPath))
                {
                    logger.LogInfo("My Assembly found!");
                    LoadDirectory(directory);
                }
            }
        }

        internal static void LoadDirectory(string directory)
        {
            string text = File.ReadAllText(Path.Combine(directory, "manifest.json"));
            Manifest manifest = JsonSerializer.Deserialize<Manifest>(text);

            if (string.IsNullOrEmpty(manifest.author) ||
                string.IsNullOrEmpty(manifest.name) ||
                string.IsNullOrEmpty(manifest.version_number))
            {
                logger.LogError($"Failed to load mod folder {directory}, because it's manifest file is not valid!");
                return;
            }

            string fullModName = $"{manifest.author}-{manifest.name}";

            logger.LogDebug($"Loading JSON mod {fullModName} version {manifest.version_number}!");
            JsonLoaderModule.LoadFolder(fullModName, directory);
        }
    }
}