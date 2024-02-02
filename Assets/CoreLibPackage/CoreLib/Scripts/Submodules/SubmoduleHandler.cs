using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Util;
using CoreLib.Util.Extensions;
using HarmonyLib;
using PugMod;

#pragma warning disable 649

namespace CoreLib
{
    public class SubmoduleHandler
    {
        private readonly GameVersion currentBuild;
        private readonly Logger logger;

        private readonly HashSet<string> loadedModules;
        private readonly Dictionary<Type, BaseSubmodule> allModules;
        private int lastSubmoduleCount;

        internal SubmoduleHandler(GameVersion build, Logger logger)
        {
            currentBuild = build;
            this.logger = logger;
            loadedModules = new HashSet<string>();

            allModules = new Dictionary<Type, BaseSubmodule>();
            UpdateSubmoduleList();
        }

        internal T GetModuleInstance<T>()
            where T : BaseSubmodule
        {
            if (allModules.TryGetValue(typeof(T), out BaseSubmodule submodule))
            {
                return (T)submodule;
            }

            return null;
        }

        /// <summary>
        /// Return true if the specified submodule is loaded.
        /// </summary>
        /// <param name="submodule">nameof the submodule</param>
        public bool IsLoaded(string submodule) => loadedModules.Contains(submodule);

        /// <summary>
        /// Load submodule
        /// </summary>
        /// <param name="moduleType">Module type</param>
        /// <returns>Is loading successful?</returns>
        public bool RequestModuleLoad(Type moduleType)
        {
            UpdateSubmoduleList();
            return RequestModuleLoad(moduleType, false);
        }

        private bool RequestModuleLoad(Type moduleType, bool ignoreDependencies)
        {
            if (moduleType == null)
                throw new ArgumentNullException(nameof(moduleType));

            if (!allModules.ContainsKey(moduleType))
                throw new InvalidOperationException($"Tried to load unknown submodule: '{moduleType.FullName}'!");

            var name = moduleType.GetNameChecked();

            if (IsLoaded(name)) return true;
            var version = allModules[moduleType].Version;

            CoreLibMod.Log.LogInfo($"Enabling CoreLib Submodule: {name}, version {version}");

            try
            {
                if (!ignoreDependencies)
                {
                    var dependencies = GetModuleDependencies(moduleType);
                    foreach (Type dependency in dependencies)
                    {
                        if (dependency == moduleType) continue;
                        if (!RequestModuleLoad(dependency, true))
                        {
                            logger.LogError($"{name} could not be initialized because one of it's dependencies failed to load.");
                        }
                    }
                }

                BaseSubmodule submodule = allModules[moduleType];

                if (!submodule.Build.Equals(GameVersion.zero) &&
                    !submodule.Build.CompatibleWith(currentBuild))
                {
                    logger.LogWarning($"Submodule {name} was built for {submodule.Build}, but current build is {currentBuild}.");
                }

                submodule.SetHooks();
                submodule.Load();

                submodule.Loaded = true;
                loadedModules.Add(name);

                submodule.PostLoad();
                return true;
            }
            catch (Exception e)
            {
                logger.LogError($"{name} could not be initialized and has been disabled:\n{e}");
            }

            return false;
        }

        private IEnumerable<Type> GetModuleDependencies(Type moduleType)
        {
            IEnumerable<Type> modulesToAdd = moduleType.GetDependants(type =>
                {
                    BaseSubmodule submodule = allModules[type];
                    return submodule.Dependencies.AddRangeToArray(submodule.GetOptionalDependencies());
                },
                (start, end) =>
                {
                    CoreLibMod.Log.LogWarning(
                        $"Found Submodule circular dependency! Submodule {start.FullName} depends on {end.FullName}, which depends on {start.FullName}! Submodule {start.FullName} and all of its dependencies will not be loaded.");
                });
            return modulesToAdd;
        }

        private void UpdateSubmoduleList()
        {
            var coreLibModules = API.ModLoader.LoadedMods.Where(IsCoreLibModuleMod);
            var moduleTypes = coreLibModules
                .SelectMany(mod => API.Reflection.GetTypes(mod.ModId).Where(IsSubmodule))
                .ToList();

            if (moduleTypes.Count > lastSubmoduleCount)
            {
                foreach (Type moduleType in moduleTypes)
                {
                    if (allModules.ContainsKey(moduleType)) continue;

                    allModules.Add(moduleType, (BaseSubmodule)Activator.CreateInstance(moduleType));
                }

                lastSubmoduleCount = moduleTypes.Count;
            }
        }

        private bool IsCoreLibModuleMod(LoadedMod mod)
        {
            return mod.Metadata.name.Contains("CoreLib");
        }

        private static bool IsSubmodule(Type type)
        {
            return type.IsAssignableTo(typeof(BaseSubmodule)) &&
                   type != typeof(BaseSubmodule);
        }
    }
}