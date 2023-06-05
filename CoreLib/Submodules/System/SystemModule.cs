using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using CoreLib.Submodules.ModComponent;
using CoreLib.Submodules.ModSystem.Jobs;
using CoreLib.Submodules.ModSystem.Patches;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
// ReSharper disable SuspiciousTypeConversion.Global

namespace CoreLib.Submodules.ModSystem
{
    [CoreLibSubmodule(Dependencies = new []{typeof(ComponentModule)})]
    public static class SystemModule
    {
        #region PublicInterface

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded
        {
            get => _loaded;
            internal set => _loaded = value;
        }

        public const int LOWEST_PRIORITY = 700;
        public const int LOWER_PRIORITY = 600;
        public const int NORMAL_PRIORITY = 500;
        public const int HIGHER_PRIORITY = 400;
        public const int HIGHEST_PRIORITY = 300;

        public static StateID GetModStateId(string stateId)
        {
            ThrowIfNotLoaded();
            int index = stateIdBind.HasIndex(stateId) ? 
                stateIdBind.GetIndex(stateId) : 
                stateIdBind.GetNextId(stateId);
            return (StateID) index;
        }
        
        public static void RegisterSystem<T>() 
            where T : MonoBehaviour
        {
            ThrowIfNotLoaded();
            Type systemType = typeof(T);

            if (!systemType.IsAssignableTo(typeof(IPseudoClientSystem)) &&
                !systemType.IsAssignableTo(typeof(IPseudoServerSystem)))
            {
                CoreLibPlugin.Logger.LogError($"Failed to register '{systemType.FullName}' as Pseudo System. Pseudo System must implement '{nameof(IPseudoClientSystem)}' or '{nameof(IPseudoServerSystem)}' ");
                return;
            }

            T instance = CoreLibPlugin.Instance.AddComponent<T>();

            bool success = false;

            if (instance is IPseudoClientSystem clientSystem)
            {
                success |= clientSystems.TryAdd(clientSystem);
            }

            if (instance is IPseudoServerSystem serverSystem)
            {
                success |= serverSystems.TryAdd(serverSystem);
            }

            if (instance is IStateRequester stateRequester)
            {
                RegisterStateRequester_Internal(stateRequester);
            }

            if (success)
            {
                CoreLibPlugin.Logger.LogInfo($"Registered '{systemType.FullName}' as a Pseudo System!");
            }
        }

        public static void RegisterStateRequester<T>()
            where T : IStateRequester
        {
            ThrowIfNotLoaded();
            Type systemType = typeof(T);
            
            if (systemType.IsAssignableTo(typeof(MonoBehaviour)))
            {
                CoreLibPlugin.Logger.LogWarning($"Failed to register '{systemType.FullName}' State Requester. MonoBehaviour based State Requesters must be registered using '{nameof(RegisterSystem)}'");
                return;
            }

            RegisterStateRequester_Internal(Activator.CreateInstance<T>());
        }

        #endregion

        #region PrivateImplementation

        public static List<IPseudoClientSystem> clientSystems = new List<IPseudoClientSystem>();
        public static List<IPseudoServerSystem> serverSystems = new List<IPseudoServerSystem>();

        public static List<IStateRequester> stateRequesters = new List<IStateRequester>();

        public static IdBindConfigFile stateIdBind; 

        public const int modStateIdRangeStart = 33000;
        public const int modStateIdRangeEnd = ushort.MaxValue;


        private static bool TryAdd<T>(this List<T> list, T item)
        {
            if (list.Any(system => system.GetType() == item.GetType()))
            {
                CoreLibPlugin.Logger.LogWarning($"Tried to register system '{item.GetType().FullName}' twice!");
                return false;
            }
            
            list.Add(item);
            return true;
        }

        private static bool _loaded;

        internal static void ThrowIfNotLoaded()
        {
            if (!Loaded)
            {
                Type submoduleType = MethodBase.GetCurrentMethod().DeclaringType;
                string message = $"{submoduleType.Name} is not loaded. Please use [{nameof(CoreLibSubmoduleDependency)}(nameof({submoduleType.Name})]";
                throw new InvalidOperationException(message);
            }
        }
        
        [CoreLibSubmoduleInit(Stage = InitStage.Load)]
        internal static void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<JobExtensions.JobDelegate>();
            
            BepInPlugin metadata = MetadataHelper.GetMetadata(typeof(CoreLibPlugin));
            stateIdBind = new IdBindConfigFile($"{Paths.ConfigPath}/CoreLib/CoreLib.ModStateID.cfg", metadata, modStateIdRangeStart, modStateIdRangeEnd);
        }
        
        [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks()
        {
            CoreLibPlugin.harmony.PatchAll(typeof(SceneHandler_Patch));
            CoreLibPlugin.harmony.PatchAll(typeof(RadicalPauseMenuOption_Patch));
            CoreLibPlugin.harmony.PatchAll(typeof(StateRequestSystem_Patch));
        }
        
        private static void RegisterStateRequester_Internal(IStateRequester stateRequester)
        {
            bool success = stateRequesters.TryAdd(stateRequester);
            
            if (success)
            {
                CoreLibPlugin.Logger.LogInfo($"Registered '{stateRequester.GetType().FullName}' as a State Requester!");
            }
        }

        #endregion
    }
}