using System;
using System.Diagnostics;
using System.Reflection;
using CoreLib.Submodules.ModEntity;
using CoreLib.Submodules.ModSystem;
using Il2CppInterop.Runtime.Injection;

namespace CoreLib.Submodules.MigrationModule
{
    /// <summary>
    /// Module is used to perform save migration. Does not provide any public API
    /// </summary>
    [CoreLibSubmodule(Dependencies = new []{typeof(EntityModule), typeof(SystemModule)})]
    public static class MigrationModule
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

        #endregion

        #region PrivateImplementation

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


        [CoreLibSubmoduleInit(Stage = InitStage.PostLoad)]
        internal static void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<IDMigrationSystem>();
            SystemModule.RegisterSystem<IDMigrationSystem>();
        }

        #endregion
    }
}