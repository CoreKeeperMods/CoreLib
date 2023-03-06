using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using ArgumentException = System.ArgumentException;
using IntPtr = System.IntPtr;
using InvalidOperationException = System.InvalidOperationException;
using Unsafe = System.Runtime.CompilerServices.Unsafe;

// Code taken from Unity engine source code, licensed under the Unity Companion License

namespace CoreLib.Submodules.ModComponent
{
    /// <summary>
    /// Extensions to allow using ECS components, which do not have AOT compiled variants. Also includes working with modded components
    /// </summary>
    public static class ECSExtensions
    {
        /// <summary>
        /// Gets the run-time type information required to access an array of component data in a chunk.
        /// </summary>
        /// <param name="isReadOnly">Whether the component data is only read, not written. Access components as
        /// read-only whenever possible.</param>
        /// <typeparam name="T">A struct that implements <see cref="IComponentData"/>.</typeparam>
        /// <returns>An object representing the type information required to safely access component data stored in a
        /// chunk.</returns>
        /// <remarks>Pass an <see cref="ComponentTypeHandle{T}"/> instance to a job that has access to chunk data,
        /// such as an <see cref="IJobChunk"/> job, to access that type of component inside the job.</remarks>
        public static unsafe ModComponentTypeHandle<T> GetModComponentTypeHandle<T>(this ComponentSystemBase system, bool isReadOnly = false) where T : struct
        {
            SystemState* state = system.CheckedState();
            return state->GetModComponentTypeHandle<T>(isReadOnly);
        }

        /// <summary>
        /// Gets the run-time type information required to access an array of component data in a chunk.
        /// </summary>
        /// <param name="isReadOnly">Whether the component data is only read, not written. Access components as
        /// read-only whenever possible.</param>
        /// <typeparam name="T">A struct that implements <see cref="IComponentData"/>.</typeparam>
        /// <returns>An object representing the type information required to safely access component data stored in a
        /// chunk.</returns>
        /// <remarks>Pass an <see cref="ComponentTypeHandle{T}"/> instance to a job that has access to chunk data,
        /// such as an <see cref="IJobChunk"/> job, to access that type of component inside the job.</remarks>
        public static unsafe ModComponentTypeHandle<T> GetModComponentTypeHandle<T>(this SystemState system, bool isReadOnly = false) where T : struct
        {
            system.AddReaderWriter(isReadOnly ? ComponentModule.ReadOnly<T>() : ComponentModule.ReadWrite<T>());
            return system.EntityManager.GetModComponentTypeHandle<T>(isReadOnly);
        }

        /// <summary>
        /// Gets the dynamic type object required to access a chunk component of type T.
        /// </summary>
        /// <remarks>
        /// To access a component stored in a chunk, you must have the type registry information for the component.
        /// This function provides that information. Use the returned <see cref="ComponentTypeHandle{T}"/>
        /// object with the functions of an <see cref="ArchetypeChunk"/> object to get information about the components
        /// in that chunk and to access the component values.
        /// </remarks>
        /// <param name="isReadOnly">Specify whether the access to the component through this object is read only
        /// or read and write. For managed components isReadonly will always be treated as false.</param>
        /// <typeparam name="T">The compile-time type of the component.</typeparam>
        /// <returns>The run-time type information of the component.</returns>
        public static unsafe ModComponentTypeHandle<T> GetModComponentTypeHandle<T>(this EntityManager entityManager, bool isReadOnly)
        {
            return new ModComponentTypeHandle<T>(isReadOnly, entityManager.GlobalSystemVersion);
        }

        /// <summary>
        /// Provides a native array interface to components stored in this chunk.
        /// </summary>
        /// <remarks>The native array returned by this method references existing data, not a copy.</remarks>
        /// <param name="chunkComponentTypeHandle">An object containing type and job safety information. Create this
        /// object by calling <see cref="ComponentSystemBase.GetComponentTypeHandle{T}(bool)"/>immediately
        /// before scheduling a job. Pass the object to a job using a public field you define as part of the job struct.</param>
        /// <typeparam name="T">The data type of the component.</typeparam>
        /// <exception cref="ArgumentException">If you call this function on a "tag" component type (which is an empty
        /// component with no fields).</exception>
        /// <returns>A native array containing the components in the chunk.</returns>
        public static unsafe NativeArrayData GetNativeArray<T>(this ArchetypeChunk chunk, ModComponentTypeHandle<T> chunkComponentTypeHandle)
            where T : struct
        {
            Chunk* chunks = chunk.m_Chunk;
            Archetype* archetype = chunks->Archetype;

            var typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(archetype, chunkComponentTypeHandle.m_TypeIndex);
            if (typeIndexInArchetype == -1)
            {
                return new NativeArrayData();
            }

            byte* ptr = (chunkComponentTypeHandle.IsReadOnly)
                ? ChunkDataUtility.GetComponentDataRO(chunks, 0, typeIndexInArchetype)
                : ChunkDataUtility.GetComponentDataRW(chunks, 0, typeIndexInArchetype, chunkComponentTypeHandle.GlobalSystemVersion);

            var length = chunk.Count;
            var batchStartOffset = chunk.m_BatchStartEntityIndex * archetype->SizeOfs[typeIndexInArchetype];
            var result = new NativeArrayData
            {
                pointer = (IntPtr)ptr + batchStartOffset,
                length = length
            };

            return result;
        }

        /// <summary>
        /// Get Component Data of type.
        /// This method will work on any type, including mod created ones
        /// </summary>
        /// <param name="entityManager">World EntityManager</param>
        /// <param name="entity">Target Entity</param>
        /// <typeparam name="T">Component Type</typeparam>
        public static unsafe T GetModComponentData<T>(this EntityManager entityManager, Entity entity)
        {
            int typeIndex = ComponentModule.GetModTypeIndex<T>();
            var dataAccess = entityManager.GetCheckedEntityDataAccess();

            if (!dataAccess->HasComponent(entity, ComponentType.FromTypeIndex(typeIndex)))
            {
                throw new InvalidOperationException($"Tried to get component data for component {typeof(T).FullName}, which entity does not have!");
            }

            if (!dataAccess->IsInExclusiveTransaction)
            {
                (&dataAccess->m_DependencyManager)->CompleteWriteDependency(typeIndex);
            }

            byte* ret = dataAccess->EntityComponentStore->GetComponentDataWithTypeRO(entity, typeIndex);

            return Unsafe.Read<T>(ret);
        }

        /// <summary>
        /// Set Component Data of type.
        /// This method will work on any type, including mod created ones
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
            {
                throw new InvalidOperationException($"Tried to set component data for component {typeof(T).FullName}, which entity does not have!");
            }

            if (!dataAccess->IsInExclusiveTransaction)
            {
                (&dataAccess->m_DependencyManager)->CompleteReadAndWriteDependency(typeIndex);
            }

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

        public static unsafe bool AddModComponent<T>(this EntityManager entityManager, Entity entity)
        {
            ComponentType componentType = ComponentType.FromTypeIndex(ComponentModule.GetModTypeIndex<T>());
            var dataAccess = entityManager.GetCheckedEntityDataAccess();
            var componentStore = dataAccess->EntityComponentStore;

            if (dataAccess->HasComponent(entity, componentType))
                return false;

            if (!componentStore->Exists(entity))
                throw new InvalidOperationException("The entity does not exist");

            var changes = dataAccess->BeginStructuralChanges();

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
        
        /// <summary>
        /// List all <see cref="Il2CppSystem.Type"/> that are on the entity
        /// </summary>
        /// <param name="entityManager">World EntityManager</param>
        /// <param name="entity">Target Entity</param>
        public static Il2CppSystem.Type[] GetModComponentTypes(this EntityManager entityManager, Entity entity)
        {
            NativeArray<ComponentType> typesArray = entityManager.GetComponentTypes(entity);
            Il2CppSystem.Type[] types = new Il2CppSystem.Type[typesArray.Length];

            for (var i = 0; i < typesArray.Length; i++)
            {
                types[i] = TypeManager.GetType(typesArray[i].TypeIndex);
            }

            return types;
        }

        internal static unsafe ref T GetData<T>(this SharedStatic<T> sharedStatic)
            where T : unmanaged
        {
            return ref Unsafe.AsRef<T>(sharedStatic._buffer);
        }
    }
}