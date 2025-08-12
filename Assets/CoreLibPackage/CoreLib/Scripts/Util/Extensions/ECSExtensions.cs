using Unity.Entities;

namespace CoreLib.Util.Extensions
{
    /// Provides extension methods for working with Unity's Entity Component System (ECS).
    public static class ECSExtensions
    {
        /// Retrieves the component data of the specified type <typeparamref name="T"/> from the given entity.
        /// If the entity does not already have a component of type <typeparamref name="T"/>, one is added with the default value.
        /// <param name="entityManager">
        /// The <see cref="Unity.Entities.EntityManager"/> that manages the given entity.
        /// </param>
        /// <param name="entity">
        /// The entity from which the component data will be retrieved or to which the component will be added.
        /// </param>
        /// <typeparam name="T">
        /// The type of the component data. Must be unmanaged and implement <see cref="Unity.Entities.IComponentData"/>.
        /// </typeparam>
        /// <returns>
        /// The component data of type <typeparamref name="T"/> associated with the specified entity. If the component
        /// is added, the returned data will be initialized to its default value.
        /// </returns>
        public static T GetOrAddComponentData<T>(this EntityManager entityManager, Entity entity) where T : unmanaged, IComponentData
        {
            if (entityManager.HasComponent<T>(entity))
            {
                return entityManager.GetComponentData<T>(entity);
            }

            entityManager.AddComponent<T>(entity);
            return default;
        }

        /// Retrieves the dynamic buffer of the specified type <typeparamref name="T"/> from the given entity.
        /// If the entity does not already have a buffer of type <typeparamref name="T"/>, one is added.
        /// <param name="entityManager">
        /// The <see cref="Unity.Entities.EntityManager"/> that manages the given entity.
        /// </param>
        /// <param name="entity">
        /// The entity from which the buffer will be retrieved or to which the buffer will be added.
        /// </param>
        /// <typeparam name="T">
        /// The type of the elements in the dynamic buffer. Must be unmanaged and implement
        /// <see cref="Unity.Entities.IBufferElementData"/>.
        /// </typeparam>
        /// <returns>
        /// The dynamic buffer of type <typeparamref name="T"/> associated with the specified entity.
        /// </returns>
        public static DynamicBuffer<T> GetOrAddBuffer<T>(this EntityManager entityManager, Entity entity) where T : unmanaged, IBufferElementData
        {
            if (entityManager.HasComponent<T>(entity))
            {
                return entityManager.GetBuffer<T>(entity);
            }

            return entityManager.AddBuffer<T>(entity);
        }

        /// Removes the specified element from the dynamic buffer. This method searches
        /// for the first occurrence of the element that matches the provided
        /// <paramref name="bufferElementData"/> and removes it.
        /// <param name="buffer">
        /// The dynamic buffer from which the element will be removed.
        /// </param>
        /// <param name="bufferElementData">
        /// The element to be removed from the buffer.
        /// </param>
        /// <typeparam name="T">
        /// The type of elements in the dynamic buffer. Must be unmanaged and implement
        /// <see cref="Unity.Entities.IBufferElementData"/>.
        /// </typeparam>
        public static void Remove<T>(this DynamicBuffer<T> buffer, T bufferElementData)
            where T : unmanaged, IBufferElementData
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Equals(bufferElementData))
                {
                    buffer.RemoveAt(i);
                    break;
                }
            }
        }
    }
}