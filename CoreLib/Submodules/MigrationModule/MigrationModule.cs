using System;
using System.Diagnostics;
using System.Reflection;
using CoreLib.Submodules.ModComponent;
using CoreLib.Submodules.ModEntity;
using CoreLib.Submodules.ModSystem;
using CoreLib.Submodules.ModSystem.Jobs;
using Il2CppInterop.Runtime.Injection;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

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
            ClassInjector.RegisterTypeInIl2Cpp<IDMigrationJob>();
            SystemModule.OnServerWorldStarted += ScheduleMigration;
        }

        internal static void ScheduleMigration(World world)
        {
            var objectsQuery = world.EntityManager.CreateEntityQuery(new EntityQueryDesc()
            {
                All = new[] { ComponentModule.ReadOnly<ObjectDataCD>() },
                Options = EntityQueryOptions.IncludeDisabled
            });
            
            var inventoriesQuery = world.EntityManager.CreateEntityQuery(new EntityQueryDesc()
            {
                All = new[] { ComponentModule.ReadOnly<InventoryCD>() },
                Options = EntityQueryOptions.IncludeDisabled
            });

            IDMigrationJob migrationJob = new IDMigrationJob()
            {
                objectsEntities = objectsQuery.ToEntityArrayAsync(Allocator.TempJob, out JobHandle handle1),
                inventoryEntities = inventoriesQuery.ToEntityArrayAsync(Allocator.TempJob, out JobHandle handle2),
                objectDataFromEntity = new ModComponentDataFromEntity<ObjectDataCD>(world.EntityManager),
                containedObjectsFromEntity = new ModBufferFromEntity<ContainedObjectsBuffer>(world.EntityManager, false)
            };
            
            JobHandle jobsHandle = JobHandle.CombineDependencies(handle1, handle2);
            JobHandle myJobHandle = migrationJob.ModSchedule(jobsHandle);
            myJobHandle.Complete();
        }

        #endregion
    }
}