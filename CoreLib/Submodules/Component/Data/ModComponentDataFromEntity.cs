using System.Runtime.CompilerServices;
using Unity.Entities;

namespace CoreLib.Submodules.ModComponent
{
    /// <summary>
    /// A [NativeContainer] that provides access to all instances of components of type T, indexed by <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IComponentData"/> to access.</typeparam>
    /// <remarks>
    /// ComponentDataFromEntity is a native container that provides array-like access to components of a specific
    /// type. You can use ComponentDataFromEntity to look up data associated with one entity while iterating over a
    /// different set of entities. For example, Unity.Transforms stores the <see cref="Entity"/> object of parent entities
    /// in a Parent component and looks up the parent's LocalToWorld matrix using
    /// ComponentDataFromEntity&lt;LocalToWorld&gt; when calculating the world positions of child entities.
    ///
    /// To get a ComponentDataFromEntity, call <see cref="ComponentSystemBase.GetComponentDataFromEntity"/>.
    ///
    /// Pass a ComponentDataFromEntity container to a job by defining a public field of the appropriate type
    /// in your IJob implementation. You can safely read from ComponentDataFromEntity in any job, but by
    /// default, you cannot write to components in the container in parallel jobs (including
    /// <see cref="IJobEntity"/>, Entities.Foreach and <see cref="IJobEntityBatch"/>). If you know that two instances of a parallel
    /// job can never write to the same index in the container, you can disable the restriction on parallel writing
    /// by adding [NativeDisableParallelForRestrictionAttribute] to the ComponentDataFromEntity field definition in the job struct.
    ///
    /// If you would like to access an entity's components outside of a job, consider using the <see cref="EntityManager"/> methods
    /// <see cref="EntityManager.GetComponentData"/> and <see cref="EntityManager.SetComponentData"/>
    /// instead, to avoid the overhead of creating a ComponentDataFromEntity object.
    ///
    /// [NativeContainer]: https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerAttribute
    /// [NativeDisableParallelForRestrictionAttribute]: https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeDisableParallelForRestrictionAttribute.html
    /// </remarks>
    public unsafe struct ModComponentDataFromEntity<T> where T : unmanaged
    {
        readonly EntityDataAccess*       m_Access;
        readonly int                     m_TypeIndex;
        readonly uint                    m_GlobalSystemVersion;
        LookupCache                      m_Cache;
        
        public ModComponentDataFromEntity(EntityManager entityManager) : 
            this(ComponentModule.GetModTypeIndex<T>(), entityManager.m_EntityDataAccess)
        {
        }

        internal ModComponentDataFromEntity(int typeIndex, EntityDataAccess* access)
        {
            m_TypeIndex = typeIndex;
            m_Access = access;
            m_Cache = default;
            m_GlobalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;
        }

        /// <summary>
        /// Reports whether the specified <see cref="Entity"/> instance still refers to a valid entity and that it has a
        /// component of type T.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>True if the entity has a component of type T, and false if it does not. Also returns false if
        /// the Entity instance refers to an entity that has been destroyed.</returns>
        /// <remarks>To report if the provided entity has a component of type T, this function confirms
        /// whether the <see cref="EntityArchetype"/> of the provided entity includes components of type T.
        /// </remarks>
        public bool HasComponent(Entity entity)
        {
            var ecs = m_Access->EntityComponentStore;
            return ecs->HasComponent(entity, m_TypeIndex);
        }

        /// <summary>
        /// Retrieves the component associated with the specified <see cref="Entity"/>, if it exists. Then reports if the instance still refers to a valid entity and that it has a
        /// component of type T.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// /// <param name="componentData">The component of type T for the given entity, if it exists.</param>
        /// <returns>True if the entity has a component of type T, and false if it does not.</returns>
        /// <remarks>To report if the provided entity has a component of type T, this function confirms
        /// whether the <see cref="EntityArchetype"/> of the provided entity includes components of type T.
        /// </remarks>
        public bool TryGetComponent(Entity entity, out T componentData)
        {
            var ecs = m_Access->EntityComponentStore;

            var hasComponent = ecs->HasComponent(entity, m_TypeIndex, ref m_Cache);
            if (hasComponent)
            {
                void* ptr = ecs->GetComponentDataWithTypeRO(entity, m_TypeIndex, ref m_Cache);
                componentData = Unsafe.Read<T>(ptr);
            }
            else
            {
                componentData = default;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Reports whether any of IComponentData components of the type T, in the chunk containing the
        /// specified <see cref="Entity"/>, could have changed.
        /// </summary>
        /// <remarks>
        /// Note that for efficiency, the change version applies to whole chunks not individual entities. The change
        /// version is incremented even when another job or system that has declared write access to a component does
        /// not actually change the component value.</remarks>
        /// <param name="entity">The entity.</param>
        /// <param name="version">The version to compare. In a system, this parameter should be set to the
        /// current <see cref="Unity.Entities.ComponentSystemBase.LastSystemVersion"/> at the time the job is run or
        /// scheduled.</param>
        /// <returns>True, if the version number stored in the chunk for this component is more recent than the version
        /// passed to the <paramref name="version"/> parameter.</returns>
        public bool DidChange(Entity entity, uint version)
        {
            var ecs = m_Access->EntityComponentStore;
            var chunk = ecs->GetChunk(entity);

            var typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(chunk->Archetype, m_TypeIndex);
            if (typeIndexInArchetype == -1) return false;
            var chunkVersion = chunk->GetChangeVersion(typeIndexInArchetype);

            return ChangeVersionUtility.DidChange(chunkVersion, version);
        }

        /// <summary>
        /// Gets the <see cref="IComponentData"/> instance of type T for the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>An <see cref="IComponentData"/> type.</returns>
        /// <remarks>You cannot use ComponentDataFromEntity to get zero-sized <see cref="IComponentData"/>.
        /// Use <see cref="HasComponent"/> to check whether an entity has the zero-sized component instead.
        ///
        /// Normally, you cannot write to components accessed using a ComponentDataFromEntity instance
        /// in a parallel Job. This restriction is in place because multiple threads could write to the same component,
        /// leading to a race condition and nondeterministic results. However, when you are certain that your algorithm
        /// cannot write to the same component from different threads, you can manually disable this safety check
        /// by putting the [NativeDisableParallelForRestrictions] attribute on the ComponentDataFromEntity field in the Job.
        ///
        /// </remarks>
        public T this[Entity entity]
        {
            get
            {
                var ecs = m_Access->EntityComponentStore;

                void* ptr = ecs->GetComponentDataWithTypeRO(entity, m_TypeIndex, ref m_Cache);
                return Unsafe.Read<T>(ptr);
            }
            set
            {
                var ecs = m_Access->EntityComponentStore;

                void* ptr = ecs->GetComponentDataWithTypeRW(entity, m_TypeIndex, m_GlobalSystemVersion, ref m_Cache);
                Unsafe.Copy(ptr, ref value);
            }
        }

        internal void SetComponentEnabled(Entity entity, bool value)
        {
            m_Access->SetComponentEnabled(entity, m_TypeIndex, value);
        }
    }
}