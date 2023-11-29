using Unity.Entities;

namespace CoreLib.Util.Extensions
{
    public static class ECSExtensions
    {
        public static T GetOrAddComponentData<T>(this EntityManager entityManager, Entity entity) where T : unmanaged, IComponentData
        {
            if (entityManager.HasComponent<T>(entity))
            {
                return entityManager.GetComponentData<T>(entity);
            }

            entityManager.AddComponent<T>(entity);
            return default;
        }
        
        public static DynamicBuffer<T> GetOrAddBuffer<T>(this EntityManager entityManager, Entity entity) where T : unmanaged, IBufferElementData
        {
            if (entityManager.HasComponent<T>(entity))
            {
                return entityManager.GetBuffer<T>(entity);
            }

            return entityManager.AddBuffer<T>(entity);
        }
        
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