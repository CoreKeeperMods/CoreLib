// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: CoreLibMod.cs
// Author: Minepatcher, Limoka
// Created: 2023-09-16
// Updated: 2025-11-21
// Description: Core entry point for the Core Library mod. Handles initialization,
//              configuration, version verification, and dynamic submodule loading.
// ========================================================

using System;
using System.Linq;
using CoreLib.Data.Configuration;
using CoreLib.Util.Extension;
using PugMod;
using UnityEngine;
using Logger = CoreLib.Util.Logger;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace CoreLib
{
    /// Main entry point for the Core Library mod.
    /// Implements <see cref="IMod"/> to integrate with the Core Keeper mod loader
    /// and provide initialization, version control, and submodule management.
    /// <seealso cref="IMod"/>
    /// <seealso cref="BaseSubmodule"/>
    /// <seealso cref="SubmoduleHandler"/>
    public class CoreLibMod : IMod
    {
        #region Fields
        
        /// The unique identifier string used to distinguish this mod from others.
        public const string ID = "CoreLib";

        /// The human-readable name of this mod, used in logs and UI.
        public const string Name = "Core Library";

        /// The relative path used for storing and retrieving mod configuration files.
        public const string ConfigFolder = "CoreLib/";

        /// The current Core Library mod version.
        public const string Version = "4.0.0";

        /// Specifies the game version this mod was built for.
        /// Used to verify compatibility during initialization.
        /// <seealso cref="GameVersion"/>
        public static readonly GameVersion BuildFor = new(1, 1, 2, 0, "7da5");

        /// Metadata information about this mod, provided by the mod loader.
        internal static LoadedMod ModInfo;

        /// Centralized logging utility for CoreLib operations.
        /// <seealso cref="Logger"/>
        internal static readonly Logger Log = new(Name);
        
        internal static ConfigFile Config;

        /// Manages submodules and their lifecycle during mod initialization.
        /// <seealso cref="SubmoduleHandler"/>
        internal static SubmoduleHandler SubmoduleHandler { get; set; }

        #endregion

        #region IMod Implementation
        public void EarlyInit()
        {
            try
            {
                ModInfo = this.GetModInfo() ?? throw new InvalidOperationException($"Mod metadata for {Name} not found!");

                Config = new ConfigFile($"{ConfigFolder}CoreLib.cfg", true, ModInfo);
                
                var gameBuild = new GameVersion(Application.version);
                Log.LogInfo($"Loading {Name} version {Version}");
                Log.LogInfo($"Built For Game Version: {BuildFor}\nRunning Game Version: {gameBuild}");
                SubmoduleHandler = new SubmoduleHandler(gameBuild, Log);
            }
            catch (Exception e)
            {
                Log.LogError($"{Name} initialization failed: {e.Message}\n{e.StackTrace}");
            }
        }

        public void Init()
        {
            Log.LogInfo("Doing Late load");
            SubmoduleHandler.CallLateLoad();
        } 
        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { }

        #endregion

        #region Public Methods

        /// Requests loading of one or more CoreLib submodules.
        /// <param name="moduleTypes">
        /// An array of <see cref="Type"/> objects representing submodules to load.
        /// </param>
        public static void LoadSubmodule(params Type[] moduleTypes) =>
            moduleTypes?.Where(t => t != null).ToList()
                .ForEach(type => SubmoduleHandler.RequestModuleLoad(type));

        #endregion

        #region Internal Methods

        /// Retrieves an instance of a loaded CoreLib submodule of type <typeparamref name="T"/>.
        /// <typeparam name="T">The submodule type to retrieve.</typeparam>
        /// <returns>An instance of the requested submodule type, or <c>null</c> if not loaded.</returns>
        internal static T GetModuleInstance<T>() where T : BaseSubmodule => SubmoduleHandler.GetModuleInstance<T>();

        /// Applies all Harmony patches for the specified type on behalf of this mod.
        /// <param name="type">The type containing Harmony patch attributes.</param>
        internal static void Patch(Type type) => API.ModLoader.ApplyHarmonyPatch(ModInfo.ModId, type);

        #endregion
    }
}