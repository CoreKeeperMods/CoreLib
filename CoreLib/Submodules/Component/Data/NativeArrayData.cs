using System;
using Unity.Collections;

namespace CoreLib.Submodules.ModComponent
{
    public struct NativeArrayData
    {
        public IntPtr pointer;
        public int length;
        public Allocator allocatorLabel;
            
        public static unsafe NativeArrayData ToNativeArray<T>(NativeArray<T> array) where T : unmanaged
        {
            return new NativeArrayData()
            {
                pointer = (IntPtr)array.m_Buffer,
                length = array.m_Length,
                allocatorLabel = array.m_AllocatorLabel
            };
        }
    }
}