using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

// Code decompiled from Unity engine code

namespace CoreLib.Submodules.ModComponent
{
    /// <summary>
    ///   <para>A NativeArray exposes a buffer of native memory to managed code, making it possible to share data between managed and native without marshalling costs.</para>
    ///  This version can work with any types.
    /// </summary>
    [DebuggerDisplay("Length = {Length}")]
    public struct ModNativeArray<T> : IDisposable, IEnumerable<T>, IEnumerable, IEquatable<ModNativeArray<T>>
        where T : unmanaged
    {
        internal unsafe void* m_Buffer;
        internal int m_Length;
        internal Allocator m_AllocatorLabel;

        public unsafe ModNativeArray(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(length, allocator, out this);
            if ((options & NativeArrayOptions.ClearMemory) != NativeArrayOptions.ClearMemory)
                return;
            UnsafeUtility.MemClear(m_Buffer, Length * (long)Unsafe.SizeOf<T>());
        }

        public ModNativeArray(T[] array, Allocator allocator)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            Allocate(array.Length, allocator, out this);
            Copy(array, this);
        }

        public ModNativeArray(ModNativeArray<T> array, Allocator allocator)
        {
            Allocate(array.Length, allocator, out this);
            Copy(array, 0, this, 0, array.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void* GetUnsafePtr()
        {
            return m_Buffer;
        }

        private static unsafe void Allocate(int length, Allocator allocator, out ModNativeArray<T> array)
        {
            long num = ModUnsafe.SizeOf<T>() * (long)length;
            array = new ModNativeArray<T>();
            array.m_Buffer = UnsafeUtility.Malloc(num, ModUnsafe.AlignOf<T>(), allocator);
            array.m_Length = length;
            array.m_AllocatorLabel = allocator;
        }

        public int Length => m_Length;

        public unsafe T this[int index]
        {
            get => ModUnsafe.ReadArrayElement<T>(m_Buffer, index);
            set => ModUnsafe.WriteArrayElement(m_Buffer, index, value);
        }
        
        public unsafe bool IsCreated => (IntPtr)m_Buffer != IntPtr.Zero;

        public unsafe void Dispose()
        {
            if ((IntPtr)m_Buffer == IntPtr.Zero)
                throw new ObjectDisposedException("The NativeArray is already disposed.");
            if (m_AllocatorLabel == Allocator.Invalid)
                throw new InvalidOperationException("The NativeArray can not be Disposed because it was not allocated with a valid allocator.");
            if (m_AllocatorLabel > Allocator.None)
            {
                UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
                m_AllocatorLabel = Allocator.Invalid;
            }

            m_Buffer = null;
        }

        public unsafe JobHandle Dispose(JobHandle inputDeps)
        {
            if (m_AllocatorLabel == Allocator.Invalid)
                throw new InvalidOperationException("The NativeArray can not be Disposed because it was not allocated with a valid allocator.");
            if ((IntPtr)m_Buffer == IntPtr.Zero)
                throw new InvalidOperationException("The NativeArray is already disposed.");
            if (m_AllocatorLabel > Allocator.None)
            {
                NativeArrayDisposeJob job = new NativeArrayDisposeJob
                {
                    Data = new NativeArrayDispose
                    {
                        m_Buffer = m_Buffer,
                        m_AllocatorLabel = m_AllocatorLabel,
                    }
                };
                JobHandle jobHandle = IJobExtensions.Schedule(job, inputDeps);

                m_Buffer = null;
                m_AllocatorLabel = Allocator.Invalid;
                return jobHandle;
            }

            m_Buffer = null;
            return inputDeps;
        }

        public void CopyFrom(T[] array) => Copy(array, this);

        public void CopyFrom(ModNativeArray<T> array) => Copy(array, this);

        public void CopyTo(T[] array) => Copy(this, array);

        public void CopyTo(ModNativeArray<T> array) => Copy(this, array);

        public T[] ToArray()
        {
            T[] dst = new T[Length];
            Copy(this, dst, Length);
            return dst;
        }

        public Enumerator GetEnumerator() => new Enumerator(ref this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(ref this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public unsafe bool Equals(ModNativeArray<T> other) => m_Buffer == other.m_Buffer && m_Length == other.m_Length;

        public override bool Equals(object obj) => obj != null && obj is ModNativeArray<T> other && Equals(other);

        public override unsafe int GetHashCode() => (int)m_Buffer * 397 ^ m_Length;

        public static bool operator ==(ModNativeArray<T> left, ModNativeArray<T> right) => left.Equals(right);

        public static bool operator !=(ModNativeArray<T> left, ModNativeArray<T> right) => !left.Equals(right);

        public static void Copy(ModNativeArray<T> src, ModNativeArray<T> dst)
        {
            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(T[] src, ModNativeArray<T> dst)
        {
            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(ModNativeArray<T> src, T[] dst)
        {
            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(ModNativeArray<T> src, ModNativeArray<T> dst, int length) => Copy(src, 0, dst, 0, length);

        public static void Copy(T[] src, ModNativeArray<T> dst, int length) => Copy(src, 0, dst, 0, length);

        public static void Copy(ModNativeArray<T> src, T[] dst, int length) => Copy(src, 0, dst, 0, length);

        public static unsafe void Copy(
            ModNativeArray<T> src,
            int srcIndex,
            ModNativeArray<T> dst,
            int dstIndex,
            int length)
        {
            UnsafeUtility.MemCpy((void*)((IntPtr)dst.m_Buffer + dstIndex * ModUnsafe.SizeOf<T>()), (void*)((IntPtr)src.m_Buffer + srcIndex * ModUnsafe.SizeOf<T>()),
                length * ModUnsafe.SizeOf<T>());
        }

        public static unsafe void Copy(
            T[] src,
            int srcIndex,
            ModNativeArray<T> dst,
            int dstIndex,
            int length)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));
            GCHandle gcHandle = GCHandle.Alloc(src, GCHandleType.Pinned);
            IntPtr num = gcHandle.AddrOfPinnedObject();
            UnsafeUtility.MemCpy((void*)((IntPtr)dst.m_Buffer + dstIndex * ModUnsafe.SizeOf<T>()), (void*)((IntPtr)(void*)num + srcIndex * ModUnsafe.SizeOf<T>()),
                length * ModUnsafe.SizeOf<T>());
            gcHandle.Free();
        }

        public static unsafe void Copy(
            ModNativeArray<T> src,
            int srcIndex,
            T[] dst,
            int dstIndex,
            int length)
        {
            if (dst == null)
                throw new ArgumentNullException(nameof(dst));
            GCHandle gcHandle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            UnsafeUtility.MemCpy((void*)((IntPtr)(void*)gcHandle.AddrOfPinnedObject() + dstIndex * ModUnsafe.SizeOf<T>()),
                (void*)((IntPtr)src.m_Buffer + srcIndex * ModUnsafe.SizeOf<T>()), length * ModUnsafe.SizeOf<T>());
            gcHandle.Free();
        }

        public unsafe U ReinterpretLoad<U>(int sourceIndex) where U : unmanaged
        {
            return ModUnsafe.ReadArrayElement<U>((void*)((IntPtr)m_Buffer + (ModUnsafe.SizeOf<T>() * sourceIndex)), 0);
        }

        public unsafe void ReinterpretStore<U>(int destIndex, U data) where U : unmanaged
        {
            ModUnsafe.WriteArrayElement((void*)((IntPtr)m_Buffer + (ModUnsafe.SizeOf<T>() * destIndex)), 0, data);
        }

        private unsafe ModNativeArray<U> InternalReinterpret<U>(int length) where U : unmanaged
        {
            ModNativeArray<U> modNativeArray = ConvertExistingDataToNativeArray<U>(m_Buffer, length, m_AllocatorLabel);
            return modNativeArray;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ModNativeArray<U> ConvertExistingDataToNativeArray<U>(
            void* dataPointer,
            int length,
            Allocator allocator)
            where U : unmanaged
        {
            return new ModNativeArray<U>
            {
                m_Buffer = dataPointer,
                m_Length = length,
                m_AllocatorLabel = allocator,
            };
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe NativeArray<U> ConvertExistingDataToil2CppNativeArray<U>(
            void* dataPointer,
            int length,
            Allocator allocator)
            where U : unmanaged
        {
            return new NativeArray<U>
            {
                m_Buffer = dataPointer,
                m_Length = length,
                m_AllocatorLabel = allocator,
            };
        }

        public unsafe NativeArray<T> AsNativeArray()
        {
            return new NativeArray<T>
            {
                m_Buffer = m_Buffer,
                m_Length = Length,
                m_AllocatorLabel = m_AllocatorLabel,
            };
        }

        public ModNativeArray<U> Reinterpret<U>() where U : unmanaged
        {
            return InternalReinterpret<U>(Length);
        }

        public ModNativeArray<U> Reinterpret<U>(int expectedTypeSize) where U : unmanaged
        {
            long tSize = ModUnsafe.SizeOf<T>();
            long uSize = ModUnsafe.SizeOf<U>();
            long byteLen = Length * tSize;
            long num = byteLen / uSize;
            return InternalReinterpret<U>((int)num);
        }

        public unsafe ModNativeArray<T> GetSubArray(int start, int length)
        {
            ModNativeArray<T> modNativeArray =
                ConvertExistingDataToNativeArray<T>((void*)((IntPtr)m_Buffer + (ModUnsafe.SizeOf<T>() * start)), length, Allocator.None);
            return modNativeArray;
        }

        public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
        {
            private ModNativeArray<T> m_Array;
            private int m_Index;

            public Enumerator(ref ModNativeArray<T> array)
            {
                m_Array = array;
                m_Index = -1;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                ++m_Index;
                return m_Index < m_Array.Length;
            }

            public void Reset() => m_Index = -1;

            public T Current => m_Array[m_Index];

            object IEnumerator.Current => Current;
        }
    }
}