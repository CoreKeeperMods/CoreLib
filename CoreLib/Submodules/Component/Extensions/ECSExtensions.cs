using System;
using Unity.Burst;
using Unity.Entities;
using Type = Il2CppSystem.Type;
using Unsafe = System.Runtime.CompilerServices.Unsafe;

// Code taken from Unity engine source code, licensed under the Unity Companion License

namespace CoreLib.Submodules.ModComponent
{
    /// <summary>
    ///     Extensions to allow using ECS components, which do not have AOT compiled variants. Also includes working with
    ///     modded components
    /// </summary>
    public static class ECSExtensions
    {
        public const int ClearFlagsMask = 8388607;
        
        /// <summary>
        ///     Gets the run-time type information required to access an array of component data in a chunk.
        /// </summary>
        /// <param name="isReadOnly">
        ///     Whether the component data is only read, not written. Access components as
        ///     read-only whenever possible.
        /// </param>
        /// <typeparam name="T">A struct that implements <see cref="IComponentData" />.</typeparam>
        /// <returns>
        ///     An object representing the type information required to safely access component data stored in a
        ///     chunk.
        /// </returns>
        /// <remarks>
        ///     Pass an <see cref="ComponentTypeHandle{T}" /> instance to a job that has access to chunk data,
        ///     such as an <see cref="IJobChunk" /> job, to access that type of component inside the job.
        /// </remarks>
        public static unsafe ModComponentTypeHandle<T> GetModComponentTypeHandle<T>(this ComponentSystemBase system, bool isReadOnly = false) where T : struct
        {
            var state = system.CheckedState();
            return state->GetModComponentTypeHandle<T>(isReadOnly);
        }

        /// <summary>
        ///     Gets the run-time type information required to access an array of component data in a chunk.
        /// </summary>
        /// <param name="isReadOnly">
        ///     Whether the component data is only read, not written. Access components as
        ///     read-only whenever possible.
        /// </param>
        /// <typeparam name="T">A struct that implements <see cref="IComponentData" />.</typeparam>
        /// <returns>
        ///     An object representing the type information required to safely access component data stored in a
        ///     chunk.
        /// </returns>
        /// <remarks>
        ///     Pass an <see cref="ComponentTypeHandle{T}" /> instance to a job that has access to chunk data,
        ///     such as an <see cref="IJobChunk" /> job, to access that type of component inside the job.
        /// </remarks>
        public static ModComponentTypeHandle<T> GetModComponentTypeHandle<T>(this SystemState system, bool isReadOnly = false) where T : struct
        {
            system.AddReaderWriter(isReadOnly ? ComponentModule.ReadOnly<T>() : ComponentModule.ReadWrite<T>());
            return system.EntityManager.GetModComponentTypeHandle<T>(isReadOnly);
        }

        /// <summary>
        ///     Gets the dynamic type object required to access a chunk component of type T.
        /// </summary>
        /// <remarks>
        ///     To access a component stored in a chunk, you must have the type registry information for the component.
        ///     This function provides that information. Use the returned <see cref="ComponentTypeHandle{T}" />
        ///     object with the functions of an <see cref="ArchetypeChunk" /> object to get information about the components
        ///     in that chunk and to access the component values.
        /// </remarks>
        /// <param name="isReadOnly">
        ///     Specify whether the access to the component through this object is read only
        ///     or read and write. For managed components isReadonly will always be treated as false.
        /// </param>
        /// <typeparam name="T">The compile-time type of the component.</typeparam>
        /// <returns>The run-time type information of the component.</returns>
        public static ModComponentTypeHandle<T> GetModComponentTypeHandle<T>(this EntityManager entityManager, bool isReadOnly)
        {
            return new ModComponentTypeHandle<T>(isReadOnly, entityManager.GlobalSystemVersion);
        }

        /// <summary>
        ///     Provides a native array interface to components stored in this chunk.
        /// </summary>
        /// <remarks>The native array returned by this method references existing data, not a copy.</remarks>
        /// <param name="chunkComponentTypeHandle">
        ///     An object containing type and job safety information. Create this
        ///     object by calling <see cref="ComponentSystemBase.GetComponentTypeHandle{T}(bool)" />immediately
        ///     before scheduling a job. Pass the object to a job using a public field you define as part of the job struct.
        /// </param>
        /// <typeparam name="T">The data type of the component.</typeparam>
        /// <exception cref="ArgumentException">
        ///     If you call this function on a "tag" component type (which is an empty
        ///     component with no fields).
        /// </exception>
        /// <returns>A native array containing the components in the chunk.</returns>
        public static unsafe NativeArrayData GetNativeArray<T>(this ArchetypeChunk chunk, ModComponentTypeHandle<T> chunkComponentTypeHandle)
            where T : struct
        {
            var chunks = chunk.m_Chunk;
            var archetype = chunks->Archetype;

            int typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(archetype, chunkComponentTypeHandle.m_TypeIndex);
            if (typeIndexInArchetype == -1) return new NativeArrayData();

            byte* ptr = chunkComponentTypeHandle.IsReadOnly
                ? ChunkDataUtility.GetComponentDataRO(chunks, 0, typeIndexInArchetype)
                : ChunkDataUtility.GetComponentDataRW(chunks, 0, typeIndexInArchetype, chunkComponentTypeHandle.GlobalSystemVersion);

            int length = chunk.Count;
            int batchStartOffset = chunk.m_BatchStartEntityIndex * archetype->SizeOfs[typeIndexInArchetype];
            NativeArrayData result = new NativeArrayData
            {
                pointer = (IntPtr)ptr + batchStartOffset,
                length = length
            };

            return result;
        }

        /// <summary>
        ///     Gets the dynamic buffer of an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="isReadOnly">
        ///     Specify whether the access to the component through this object is read only
        ///     or read and write.
        /// </param>
        /// <typeparam name="T">The type of the buffer's elements.</typeparam>
        /// <returns>The DynamicBuffer object for accessing the buffer contents.</returns>
        /// <exception cref="ArgumentException">Thrown if T is an unsupported type.</exception>
        public static unsafe ModDynamicBuffer<T> GetModBuffer<T>(this EntityManager entityManager, Entity entity, bool isReadOnly = false) where T : unmanaged
        {
            int typeIndex = ComponentModule.GetModTypeIndex<T>();
            var access = entityManager.GetCheckedEntityDataAccess();

            if (!access->IsInExclusiveTransaction)
            {
                if (isReadOnly)
                    access->DependencyManager->CompleteWriteDependency(typeIndex);
                else
                    access->DependencyManager->CompleteReadAndWriteDependency(typeIndex);
            }

            BufferHeader* header;
            if (isReadOnly)
                header = (BufferHeader*)access->EntityComponentStore->GetComponentDataWithTypeRO(entity, typeIndex);
            else
                header = (BufferHeader*)access->EntityComponentStore->GetComponentDataWithTypeRW(entity, typeIndex,
                    access->EntityComponentStore->GlobalSystemVersion);

            int internalCapacity = GetTypeInfo(typeIndex).BufferCapacity;
            return new ModDynamicBuffer<T>(header, internalCapacity);
        }
        
        private static unsafe ref readonly TypeManager.TypeInfo GetTypeInfo(int typeIndex)
        {
            TypeManager.TypeInfo* val = TypeManager.GetTypeInfoPointer() + (typeIndex & ClearFlagsMask);
            return ref Unsafe.AsRef<TypeManager.TypeInfo>(val);
        }

        /// <summary>
        ///     Adds a dynamic buffer component to an entity.
        /// </summary>
        /// <remarks>
        ///     A buffer component stores the number of elements inside the chunk defined by the [InternalBufferCapacity]
        ///     attribute applied to the buffer element type declaration. Any additional elements are stored in a separate memory
        ///     block that is managed by the EntityManager.
        ///     Adding a component changes an entity's archetype and results in the entity being moved to a different
        ///     chunk.
        ///     (You can add a buffer component with the regular AddComponent methods, but unlike those methods, this
        ///     method conveniently also returns the new buffer.)
        ///     **Important:** This function creates a sync point, which means that the EntityManager waits for all
        ///     currently running Jobs to complete before adding the buffer and no additional Jobs can start before
        ///     the function is finished. A sync point can cause a drop in performance because the ECS framework may not
        ///     be able to make use of the processing power of all available cores.
        /// </remarks>
        /// <exception cref="ArgumentException">The <see cref="Entity" /> does not exist.</exception>
        /// <param name="entity">The entity.</param>
        /// <typeparam name="T">The type of buffer element. Must implement IBufferElementData.</typeparam>
        /// <returns>The buffer.</returns>
        /// <seealso cref="InternalBufferCapacityAttribute" />
        public static ModDynamicBuffer<T> AddBuffer<T>(this EntityManager entityManager, Entity entity) where T : unmanaged
        {
            AddModComponent<T>(entityManager, entity);
            return GetModBuffer<T>(entityManager, entity);
        }

        /// <summary>
        ///     Get Component Data of type.
        ///     This method will work on any type, including mod created ones
        /// </summary>
        /// <param name="entityManager">World EntityManager</param>
        /// <param name="entity">Target Entity</param>
        /// <typeparam name="T">Component Type</typeparam>
        public static unsafe T GetModComponentData<T>(this EntityManager entityManager, Entity entity)
        {
            int typeIndex = ComponentModule.GetModTypeIndex<T>();
            var dataAccess = entityManager.GetCheckedEntityDataAccess();

            if (!dataAccess->HasComponent(entity, ComponentType.FromTypeIndex(typeIndex)))
                throw new InvalidOperationException($"Tried to get component data for component {typeof(T).FullName}, which entity does not have!");

            if (!dataAccess->IsInExclusiveTransaction) (&dataAccess->m_DependencyManager)->CompleteWriteDependency(typeIndex);

            byte* ret = dataAccess->EntityComponentStore->GetComponentDataWithTypeRO(entity, typeIndex);

            return Unsafe.Read<T>(ret);
        }

        /// <summary>
        ///     Set Component Data of type.
        ///     This method will work on any type, including mod created ones
        /// </summary>
        /// <param name="entity">Target Entity</param>
        /// <param name="entityManager">World EntityManager</param>
        /// <param name="component">Component Data</param>
        /// <typeparam name="T">Component Type</typeparam>
        public static unsafe void SetModComponentData<T>(this EntityManager entityManager, Entity entity, T component)
        {
            int typeIndex = ComponentModule.GetModTypeIndex<T>();
            var dataAccess = entityManager.GetCheckedEntityDataAccess();
            var componentStore = dataAccess->EntityComponentStore;

            if (!dataAccess->HasComponent(entity, ComponentType.FromTypeIndex(typeIndex)))
                throw new InvalidOperationException($"Tried to set component data for component {typeof(T).FullName}, which entity does not have!");

            if (!dataAccess->IsInExclusiveTransaction) (&dataAccess->m_DependencyManager)->CompleteReadAndWriteDependency(typeIndex);

            byte* writePtr = componentStore->GetComponentDataWithTypeRW(entity, typeIndex, componentStore->m_GlobalSystemVersion);
            Unsafe.Copy(writePtr, ref component);
        }

        public static bool AddModComponentData<T>(this EntityManager entityManager, Entity entity, T component)
        {
            ComponentType componentType = ComponentType.FromTypeIndex(ComponentModule.GetModTypeIndex<T>());
            bool result = AddModComponent<T>(entityManager, entity);
            if (!componentType.IsZeroSized)
                SetModComponentData(entityManager, entity, component);

            return result;
        }

        public static bool AddModComponent<T>(this EntityManager entityManager, Entity entity)
        {
            ComponentType componentType = ComponentType.FromTypeIndex(ComponentModule.GetModTypeIndex<T>());
            return AddModComponent(entityManager, entity, componentType);
        }

        public static unsafe bool AddModComponent(EntityManager entityManager, Entity entity, ComponentType componentType)
        {
            var dataAccess = entityManager.GetCheckedEntityDataAccess();
            var componentStore = dataAccess->EntityComponentStore;

            if (dataAccess->HasComponent(entity, componentType))
                return false;

            if (!componentStore->Exists(entity))
                throw new InvalidOperationException("The entity does not exist");

            EntityComponentStore.ArchetypeChanges changes = dataAccess->BeginStructuralChanges();

            bool result = StructuralChange.AddComponentEntity(componentStore, &entity, componentType.TypeIndex);

            dataAccess->EndStructuralChanges(ref changes);

            return result;
        }

        public static unsafe bool HasModComponent<T>(this EntityManager entityManager, Entity entity)
        {
            ComponentType componentType = ComponentType.FromTypeIndex(ComponentModule.GetModTypeIndex<T>());
            var dataAccess = entityManager.GetCheckedEntityDataAccess();

            return dataAccess->HasComponent(entity, componentType);
        }

        public static bool RemoveModComponent<T>(this EntityManager entityManager, Entity entity)
        {
            ComponentType componentType = ComponentType.FromTypeIndex(ComponentModule.GetModTypeIndex<T>());
            return entityManager.RemoveComponent(entity, componentType);
        }

        public static bool RemoveModComponent(this EntityManager entityManager, Entity entity, ComponentType componentType)
        {
            return entityManager.RemoveComponent(entity, componentType);
        }

        /// <summary>
        ///     List all <see cref="Il2CppSystem.Type" /> that are on the entity
        /// </summary>
        /// <param name="entityManager">World EntityManager</param>
        /// <param name="entity">Target Entity</param>
        public static Type[] GetModComponentTypes(this EntityManager entityManager, Entity entity)
        {
            var typesArray = entityManager.GetComponentTypes(entity);
            var types = new Type[typesArray.Length];

            for (int i = 0; i < typesArray.Length; i++) types[i] = TypeManager.GetType(typesArray[i].TypeIndex);

            return types;
        }

        internal static unsafe ref T GetData<T>(this SharedStatic<T> sharedStatic)
            where T : unmanaged
        {
            return ref Unsafe.AsRef<T>(sharedStatic._buffer);
        }
    }
}