using System;
using System.Reflection;
using CoreLib.Submodules.TileSet;
using CoreLib.Util;
using HarmonyLib;
using PugMod;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

[assembly: AssemblyVersion("1.0.0.0")]

namespace CoreLib
{
    public class CoreLibMod : IMod
    {
        public static Logger Log = new Logger("Core Lib");
        public static readonly GameVersion buildFor = new GameVersion(0, 6, 0, 3, "3a54");
        
        public static Harmony harmony;
        public static VirtualAssetBundle assetBundle = new VirtualAssetBundle();
        
        internal static APISubmoduleHandler submoduleHandler;

        public void EarlyInit()
        {
            BurstRuntime.LoadAdditionalLibrary($"{Application.streamingAssetsPath}/Mods/CoreLib/CoreLib_burst_generated.dll");
            JobEarlyInitHelper.PerformJobEarlyInit(Assembly.GetExecutingAssembly());
            
            harmony = new Harmony("CoreLib");
            API.Server.OnWorldCreated += WorldInitialize;
            
            //CheckIfUsedOnRightGameVersion();
            
            submoduleHandler = new APISubmoduleHandler(buildFor, Log);
        }
        
        public void Init()
        {
            TileSetModule.TrySave();
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

        public void Shutdown()
        {
        }

        public void ModObjectLoaded(Object obj)
        {
            assetBundle.Register(obj);
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

        public void MessageReceivedOnClient(int messageType, Entity entity, int value0, int value1)
        {
        }

        public void MessageReceivedOnServer(int messageType, Entity entity, int value0, int value1)
        {
        }
    }
}