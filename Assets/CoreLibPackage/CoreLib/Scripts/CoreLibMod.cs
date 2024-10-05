using System;
using System.Runtime.CompilerServices;
using CoreLib.Data.Configuration;
using CoreLib.Util.Extensions;
using PugMod;
using QFSW.QC;
using Unity.Entities;
using UnityEngine;
using Logger = CoreLib.Util.Logger;
using Object = UnityEngine.Object;

[assembly:InternalsVisibleTo("CoreLib.Audio")]
[assembly:InternalsVisibleTo("CoreLib.Commands")]
[assembly:InternalsVisibleTo("CoreLib.Drops")]
[assembly:InternalsVisibleTo("CoreLib.Editor")]
[assembly:InternalsVisibleTo("CoreLib.Entity")]
[assembly:InternalsVisibleTo("CoreLib.Equipment")]
[assembly:InternalsVisibleTo("CoreLib.JsonLoader")]
[assembly:InternalsVisibleTo("CoreLib.Localization")]
[assembly:InternalsVisibleTo("CoreLib.ModderTools")]
[assembly:InternalsVisibleTo("CoreLib.Resources")]
[assembly:InternalsVisibleTo("CoreLib.RewiredExtension")]
[assembly:InternalsVisibleTo("CoreLib.Tilesets")]
[assembly:InternalsVisibleTo("CoreLib.UserInterface")]

namespace CoreLib
{
    public class CoreLibMod : IMod
    {
        public const string ID = "CoreLib";
        public const string NAME = "Core Lib";
        public const string CONFIG_FOLDER = "CoreLib/Config/";
        public const string VERSION = "3.2.1";

        internal static LoadedMod modInfo;
        
        internal static Logger Log = new Logger(NAME);
        internal static ConfigFile Config;
        public static readonly GameVersion buildFor = new GameVersion(1, 0, 0, "6489");
        
        internal static SubmoduleHandler submoduleHandler;

        public void EarlyInit()
        {
            modInfo = this.GetModInfo();
            if (modInfo == null)
            {
                Log.LogError("Failed to load CoreLib: mod metadata not found!");
                return;
            }

            Config = new ConfigFile($"{CONFIG_FOLDER}CoreLib.cfg", true, modInfo);
            API.Server.OnWorldCreated += WorldInitialize;

            var gameBuild = new GameVersion(Application.version);
            
            CheckIfUsedOnRightGameVersion(gameBuild);

            Log.LogInfo($"Loading CoreLib version {VERSION}!");
            
            submoduleHandler = new SubmoduleHandler(gameBuild, Log);
        }

        internal static void Patch(Type type)
        {
            API.ModLoader.ApplyHarmonyPatch(modInfo.ModId, type);
        }

        public void Init()
        {
        }
        
        /// <summary>
        /// Return true if the specified submodule is loaded.
        /// </summary>
        /// <param name="submodule">nameof the submodule</param>
        public static bool IsSubmoduleLoaded(string submodule)
        {
            return submoduleHandler.IsLoaded(submodule);
        }

        /// <summary>
        /// Load specified module
        /// </summary>
        /// <param name="moduleType">Type of needed module</param>
        /// <returns>Is loading successful?</returns>
        public static bool LoadModule(Type moduleType)
        {
            return submoduleHandler.RequestModuleLoad(moduleType);
        }

        /// <summary>
        /// Load specified modules
        /// </summary>
        /// <param name="moduleTypes">Types of needed modules</param>
        public static void LoadModules(params Type[] moduleTypes)
        {
            foreach (Type module in moduleTypes)
            {
                if (module == null) continue;
                LoadModule(module);
            }
        }
        
        internal static void CheckIfUsedOnRightGameVersion(GameVersion buildId)
        {
            Log.LogInfo($"Running under game version \"{buildId}\".");
            if (buildId == GameVersion.zero) return;
            
            if (buildFor.CompatibleWith(buildId))
                return;

            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            Log.LogWarning($"This version of CoreLib was built for game version \"{buildFor}\", but you are running \"{buildId}\".");
            Log.LogWarning("Should any problems arise, please check for a new version before reporting issues.");
        }

        internal static T GetModuleInstance<T>()
            where T : BaseSubmodule
        {
            return submoduleHandler.GetModuleInstance<T>();
        }

        public void Shutdown()
        {
        }

        public void ModObjectLoaded(Object obj)
        {
        }

        public bool CanBeUnloaded()
        {
            return false;
        }

        public void Update()
        {
        }

        public void WorldInitialize()
        {
        }
        
        private void OnObjectSpawned(Entity entity, EntityManager entitymanager, GameObject graphicalobject)
        {
        }
    }
}