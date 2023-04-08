using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace CoreLib.Submodules.ModComponent
{
    public static class ModUnsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AlignOf<U>() where U : unmanaged => Unsafe.SizeOf<UnsafeUtility.AlignOfHelper<U>>() - Unsafe.SizeOf<U>();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe U ReadArrayElement<U>(void* source, int index) where U : unmanaged => *(U*)((IntPtr)source + (index * sizeof(U)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteArrayElement<U>(void* destination, int index, U value) where U : unmanaged =>
            *(U*)((IntPtr)destination + index * sizeof(U)) = value;

        public static unsafe ref T ArrayElementAsRef<T>(void* ptr, int index) where T : unmanaged => ref Unsafe.AsRef<T>((void*)((IntPtr) ptr +  index *  sizeof (T)));

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>() where T : unmanaged
        {
            return Unsafe.SizeOf<T>();
        }
    }
}