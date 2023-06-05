using Unity.Entities;
#pragma warning disable CS0414

namespace CoreLib.Submodules.ModComponent
{
    public struct ModBufferTypeHandle<T> where T : struct
    {
        public uint GlobalSystemVersion => m_GlobalSystemVersion;

        public bool IsReadOnly => m_IsReadOnly;

        internal ModBufferTypeHandle(bool isReadOnly, uint globalSystemVersion)
        {
            m_Length = 1;
            m_TypeIndex = ComponentModule.GetModTypeIndex<T>();
            m_GlobalSystemVersion = globalSystemVersion;
            m_IsReadOnly = isReadOnly;
        }

        public unsafe void Update(ComponentSystemBase system)
        {
            Update(ref *system.m_StatePtr);
        }

        public unsafe void Update(ref SystemState state)
        {
            m_GlobalSystemVersion = state.m_EntityComponentStore->GlobalSystemVersion;
        }

        internal readonly int m_TypeIndex;
        internal uint m_GlobalSystemVersion;
        internal readonly bool m_IsReadOnly;
        private readonly int m_Length;
    }
}