using Unity.Entities;

namespace CoreLib.Submodules.ModComponent
{
    public struct ModComponentTypeHandle<T>
    {
        internal readonly int m_TypeIndex;
        internal readonly uint m_GlobalSystemVersion;
        internal readonly bool m_IsReadOnly;
        internal readonly bool m_IsZeroSized;

        public uint GlobalSystemVersion => m_GlobalSystemVersion;
        public bool IsReadOnly => m_IsReadOnly;

#pragma warning disable 0414
        private readonly int m_Length;
#pragma warning restore 0414
        
        internal unsafe ModComponentTypeHandle(bool isReadOnly, uint globalSystemVersion)
        {
            m_Length = 1;
            m_TypeIndex = ComponentModule.GetModTypeIndex<T>();
            m_IsZeroSized = TypeManager.GetTypeInfoPointer()[m_TypeIndex & 0x00FFFFFF].IsZeroSized;
            m_GlobalSystemVersion = globalSystemVersion;
            m_IsReadOnly = isReadOnly;
        }
    }
}