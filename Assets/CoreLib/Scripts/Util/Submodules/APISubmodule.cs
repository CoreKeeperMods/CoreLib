using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

#pragma warning disable 649

namespace CoreLib
{
    public class APISubmoduleHandler
    {
        private readonly GameVersion _build;
        private readonly Logger logger;

        private readonly HashSet<string> loadedModules;

        private readonly Dictionary<Type, BaseSubmodule> allModules;

        internal APISubmoduleHandler(GameVersion build, Logger logger)
        {
            _build = build;
            this.logger = logger;
            loadedModules = new HashSet<string>();

            allModules = GetSubmodules();
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
            return RequestModuleLoad(moduleType, false);
        }
        private bool RequestModuleLoad(Type moduleType, bool ignoreDependencies)
        {
            if (moduleType == null)
                throw new ArgumentNullException(nameof(moduleType));
            
            if (!allModules.ContainsKey(moduleType))
                throw new InvalidOperationException($"Tried to load unknown submodule: '{moduleType.FullName}'!");

            if (IsLoaded(moduleType.Name)) return true;

            CoreLibMod.Log.LogInfo($"Enabling CoreLib Submodule: {moduleType.Name}");

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
                            logger.LogError($"{moduleType.Name} could not be initialized because one of it's dependencies failed to load.");
                        }
                    }
                }

                BaseSubmodule submodule = allModules[moduleType];

                if (!submodule.Build.Equals(GameVersion.zero) &&
                    !submodule.Build.CompatibleWith(_build))
                {
                    logger.LogWarning($"Submodule {moduleType.Name} was built for {submodule.Build}, but current build is {_build}.");
                }

                submodule.SetHooks();
                submodule.Load();

                submodule.Loaded = true;
                loadedModules.Add(moduleType.Name);
                
                submodule.PostLoad();
                return true;
            }
            catch (Exception e)
            {
                logger.LogError($"{moduleType.Name} could not be initialized and has been disabled:\n{e}");
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

        public Dictionary<Type, BaseSubmodule> GetSubmodules()
        {
            var moduleTypes = ReflectionUtil.GetTypesFromCallingAssembly().Where(type => type.IsAssignableTo(typeof(BaseSubmodule))).ToList();
            
            var moduleDict = new Dictionary<Type, BaseSubmodule>();
            foreach (Type moduleType in moduleTypes)
            {
                moduleDict.Add(moduleType, (BaseSubmodule)Activator.CreateInstance(moduleType));
            }
            
            return moduleDict;
        }
    }
}