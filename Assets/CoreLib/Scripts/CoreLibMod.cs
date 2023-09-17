using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CoreLib.Submodules.TileSet;
using CoreLib.Util;
using CoreLib.Util.Extensions;
using HarmonyLib;
using PugMod;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;
using Logger = CoreLib.Util.Logger;
using Object = UnityEngine.Object;

namespace CoreLib
{
    public class CoreLibMod : IMod
    {
        public const string ID = "CoreLib";
        public const string NAME = "Core Lib";
        
        public static Logger Log = new Logger(NAME);
        public static readonly GameVersion buildFor = new GameVersion(0, 6, 0, 3, "3a54");
        
        public static Harmony harmony;
        public static AssetBundle assetBundle;
        
        internal static SubmoduleHandler submoduleHandler;

        public void EarlyInit()
        {
            var coreLibMod = this.GetModInfo();
            if (coreLibMod == null)
            {
                Log.LogError("Failed to load CoreLib: mod metadata not found!");
                return;
            }

            BurstRuntime.LoadAdditionalLibrary(Path.Combine(coreLibMod.Directory, "CoreLib_burst_generated.dll"));
            JobEarlyInitHelper.PerformJobEarlyInit(Assembly.GetExecutingAssembly());
            
            harmony = new Harmony(ID);
            API.Server.OnWorldCreated += WorldInitialize;
            
            assetBundle = coreLibMod.AssetBundles.FirstOrDefault();
            
            //CheckIfUsedOnRightGameVersion();
            
            submoduleHandler = new SubmoduleHandler(buildFor, Log);
            LoadModule(typeof(TileSetModule));
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
        
        internal static void CheckIfUsedOnRightGameVersion()
        {
            var buildId = new GameVersion(Application.version);
            Log.LogInfo($"Running under game version \"{buildId}\".");

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