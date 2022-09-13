using System;
using System.Reflection;
using Il2CppInterop.Runtime.Injection;

namespace CoreLib.Submodules.Utils
{
    [CoreLibSubmodule]
    public class UtilsModule
    {
        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded
        {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;
        

        [CoreLibSubmoduleInit(Stage = InitStage.Load)]
        internal static void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<ThreadingHelper>();
            ThreadingHelper.Initialize();
        }

        internal static void ThrowIfNotLoaded()
        {
            if (!Loaded)
            {
                Type submoduleType = MethodBase.GetCurrentMethod().DeclaringType;
                string message = $"{submoduleType.Name} is not loaded. Please use [{nameof(CoreLibSubmoduleDependency)}(nameof({submoduleType.Name})]";
                throw new InvalidOperationException(message);
            }
        }
    }
}