using Unity.Entities;

namespace CoreLib.Submodules.ModComponent
{
	public struct ModBufferFromEntity<T> where T : unmanaged
	{
		public unsafe ModBufferFromEntity(EntityManager entityManager, bool isReadOnly) : 
			this(ComponentModule.GetModTypeIndex<T>(), entityManager.m_EntityDataAccess, isReadOnly)
		{
		}

		internal unsafe ModBufferFromEntity(int typeIndex, EntityDataAccess* access, bool isReadOnly)
		{
			m_TypeIndex = typeIndex;
			m_Access = access;
			m_IsReadOnly = isReadOnly;
			m_Cache = default;
			m_GlobalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;
			m_InternalCapacity = ComponentModule.GetTypeInfo<T>().BufferCapacity;
		}

		public unsafe bool TryGetBuffer(Entity entity, out ModDynamicBuffer<T> bufferData)
		{
			EntityComponentStore* entityComponentStore = m_Access->EntityComponentStore;
			if (entityComponentStore->HasComponent(entity, m_TypeIndex, ref m_Cache))
			{
				BufferHeader* header;
				if (m_IsReadOnly)
					header = (BufferHeader*)entityComponentStore->GetComponentDataWithTypeRO(entity, m_TypeIndex, ref m_Cache);
				else
					header = (BufferHeader*)entityComponentStore->GetComponentDataWithTypeRW(entity, m_TypeIndex, m_GlobalSystemVersion, ref m_Cache);
				bufferData = new ModDynamicBuffer<T>(header, m_InternalCapacity);
				return true;
			}
			bufferData = default;
			return false;
		}

		public unsafe bool HasComponent(Entity entity)
		{
			return m_Access->EntityComponentStore->HasComponent(entity, m_TypeIndex);
		}

		public unsafe bool DidChange(Entity entity, uint version)
		{
			Chunk* chunk = m_Access->EntityComponentStore->GetChunk(entity);
			int indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(chunk->Archetype, m_TypeIndex);
			return indexInTypeArray != -1 && ChangeVersionUtility.DidChange(chunk->GetChangeVersion(indexInTypeArray), version);
		}

		public unsafe ModDynamicBuffer<T> this[Entity entity]
		{
			get
			{
				EntityComponentStore* entityComponentStore = m_Access->EntityComponentStore;
				if (m_IsReadOnly)
				{
					return new ModDynamicBuffer<T>((BufferHeader*)entityComponentStore->GetComponentDataWithTypeRO(entity, m_TypeIndex, ref m_Cache), m_InternalCapacity);
				}
				return new ModDynamicBuffer<T>((BufferHeader*)entityComponentStore->GetComponentDataWithTypeRW(entity, m_TypeIndex, m_GlobalSystemVersion, ref m_Cache), m_InternalCapacity);
			}
		}

		
		private readonly unsafe EntityDataAccess* m_Access;
		private readonly int m_TypeIndex;
		private readonly bool m_IsReadOnly;
		private readonly uint m_GlobalSystemVersion;
		private int m_InternalCapacity;
		private LookupCache m_Cache;
	}
}