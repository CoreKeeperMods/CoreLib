using System;
using CoreLib.Data.Configuration;
using CoreLib.Util.Extensions;
using PugMod;
using Unity.Entities;
using UnityEngine;
using Logger = CoreLib.Util.Logger;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace CoreLib
{
    /// <summary>
    /// Represents the Core Library Mod. This class implements the IMod interface
    /// to facilitate mod initialization, configuration handling, version management, and submodule support.
    /// </summary>
    public class CoreLibMod : IMod
    {
        public const string ID = "CoreLib";
        public const string Name = "Core Lib";
        public const string ConfigFolder = "CoreLib/Config/";
        public const string Version = "4.0.0";
        
        internal static LoadedMod ModInfo;
        
        internal static Logger Log = new(Name);
        internal static ConfigFile Config;
        public static readonly GameVersion BuildFor = new(1, 1, 2, 0, "7da5");
        
        internal static SubmoduleHandler SubmoduleHandler;


        /// <summary>
        /// Performs early initialization of the CoreLib module by setting up the mod metadata, configuration files,
        /// and submodule handler. Validates compatibility with the game's version and subscribes to necessary events
        /// for further initialization during the game lifecycle.
        /// </summary>
        public void EarlyInit()
        {
            ModInfo = this.GetModInfo();
            if (ModInfo == null)
            {
                Log.LogError("Failed to load CoreLib: mod metadata not found!");
                return;
            }

            
            //API.ConfigFilesystem.CreateDirectory(ConfigFolder);
            //API.ConfigFilesystem.
            //API.Config.Register("CoreLib", "General Test", "Testing Description and such", "Test", "blah");
            //Config = new ConfigFile($"{ConfigFolder}CoreLib.cfg", true, ModInfo);
            API.Server.OnWorldCreated += WorldInitialize;

            var gameBuild = new GameVersion(Application.version);

            CheckIfUsedOnRightGameVersion(gameBuild);

            Log.LogInfo($"Loading CoreLib version {Version}!");

            SubmoduleHandler = new SubmoduleHandler(gameBuild, Log);
        }

        /// <summary>
        /// Applies a Harmony patch to the specified type within the mod's context to modify or extend its functionality at runtime.
        /// </summary>
        /// <param name="type">The type representing the target class or method to be patched.</param>
        internal static void Patch(Type type)
        {
            API.ModLoader.ApplyHarmonyPatch(ModInfo.ModId, type);
        }

        /// <summary>
        /// Performs initialization processes for the CoreLib module during the game's lifecycle. This includes setting up
        /// the mod's internal state, establishing necessary dependencies, and preparing for further operations within the
        /// module structure.
        /// </summary>
        public void Init()
        {
        }

        /// <summary>
        /// Determines whether a specific submodule is loaded.
        /// </summary>
        /// <param name="submodule">The name of the submodule to check.</param>
        /// <returns>True if the specified submodule is loaded; otherwise, false.</returns>
        public static bool IsSubmoduleLoaded(string submodule)
        {
            return SubmoduleHandler.IsLoaded(submodule);
        }

        /// <summary>
        /// Loads the specified module.
        /// </summary>
        /// <param name="moduleType">The type of the module to load.</param>
        /// <returns>True if the module was successfully loaded; otherwise, false.</returns>
        public static bool LoadModule(Type moduleType)
        {
            return SubmoduleHandler.RequestModuleLoad(moduleType);
        }

        /// <summary>
        /// Loads the specified modules by iterating through the provided types and initializing each one.
        /// Ensures that only non-null module types are processed and delegates the actual module loading
        /// to the <see cref="LoadModule"/> method.
        /// </summary>
        /// <param name="moduleTypes">An array of <see cref="Type"/> objects representing the modules to be loaded.</param>
        public static void LoadModules(params Type[] moduleTypes)
        {
            foreach (Type module in moduleTypes)
            {
                if (module == null) continue;
                LoadModule(module);
            }
        }

        /// <summary>
        /// Ensures the mod is being used with a version of the game compatible with the mod's intended version.
        /// Logs a warning if the game version does not match the expected version.
        /// </summary>
        /// <param name="buildId">The currently running game version to be validated against the mod's compatible version.</param>
        internal static void CheckIfUsedOnRightGameVersion(GameVersion buildId)
        {
            Log.LogInfo($"Running under game version \"{buildId}\".");
            if (buildId == GameVersion.Zero) return;

            if (BuildFor.CompatibleWith(buildId))
                return;

            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            Log.LogWarning(
                $"This version of CoreLib was built for game version \"{BuildFor}\", but you are running \"{buildId}\".");
            Log.LogWarning("Should any problems arise, please check for a new version before reporting issues.");
        }

        /// <summary>
        /// Retrieves an instance of the specified submodule type. The submodule must inherit from <see cref="BaseSubmodule"/>.
        /// This method relies on the internal <see cref="CoreLib.SubmoduleHandler"/> to provide the appropriate module instance.
        /// </summary>
        /// <typeparam name="T">The type of the submodule to retrieve, which must extend <see cref="BaseSubmodule"/>.</typeparam>
        /// <returns>The instance of the requested submodule type.</returns>
        internal static T GetModuleInstance<T>()
            where T : BaseSubmodule
        {
            return SubmoduleHandler.GetModuleInstance<T>();
        }

        /// <summary>
        /// Performs tasks necessary for shutting down the CoreLib module. This typically includes releasing resources,
        /// saving configurations, unsubscribing from events, and ensuring the mod can be cleanly unloaded without leaving
        /// residual effects on the application. This method is crucial for maintaining stability when disabling or reloading the mod.
        /// </summary>
        public void Shutdown()
        {
        }

        /// <summary>
        /// Handles actions or processes triggered when a mod-related object is successfully loaded.
        /// This method is invoked to facilitate any necessary setup, integration, or validation tasks
        /// associated with the loaded object within the mod lifecycle.
        /// </summary>
        /// <param name="obj">The loaded object related to the mod. Typically used to perform
        /// specific actions based on the type or state of the object.</param>
        public void ModObjectLoaded(Object obj)
        {
        }

        /// <summary>
        /// Determines whether the mod can be unloaded.
        /// </summary>
        /// <returns>True if the mod can be unloaded; otherwise, false.</returns>
        public bool CanBeUnloaded()
        {
            return false;
        }

        /// <summary>
        /// Handles the periodic update logic for the CoreLib mod. This method is invoked during
        /// the update cycle to perform necessary tasks such as refreshing states, checking conditions,
        /// or processing mod-related runtime operations.
        /// </summary>
        public void Update()
        {
        }

        /// <summary>
        /// Executes initialization procedures specific to the game world creation process, enabling necessary setup
        /// for the game's runtime environment, event handling, and mod functionality integration.
        /// </summary>
        public void WorldInitialize()
        {
        }

        /// <summary>
        /// Handles the event of an object being spawned. Responsible for processing the spawned entity,
        /// associating it with Unity's ECS (Entity Component System), and potentially linking it with a
        /// corresponding graphical representation in the scene.
        /// </summary>
        /// <param name="entity">The Entity that was spawned within the ECS.</param>
        /// <param name="entitymanager">The EntityManager responsible for managing the spawned entity.</param>
        /// <param name="graphicalobject">The GameObject that represents the visual aspect of the spawned entity.</param>
        private void OnObjectSpawned(Unity.Entities.Entity entity, EntityManager entitymanager, GameObject graphicalobject)
        {
        }
    }
}