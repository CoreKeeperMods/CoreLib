using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoreLib.Submodules.ModSystem.Patches;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace CoreLib.Submodules.ModSystem
{
    [CoreLibSubmodule]
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

            if (success)
            {
                CoreLibPlugin.Logger.LogInfo($"Registered '{systemType.FullName}' as a Pseudo System!");
            }
        }

        #endregion

        #region PrivateImplementation

        public static List<IPseudoClientSystem> clientSystems = new List<IPseudoClientSystem>();
        public static List<IPseudoServerSystem> serverSystems = new List<IPseudoServerSystem>();


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


        [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks()
        {
            CoreLibPlugin.harmony.PatchAll(typeof(SceneHandler_Patch));
            CoreLibPlugin.harmony.PatchAll(typeof(RadicalPauseMenuOption_Patch));
        }

        #endregion
    }
}