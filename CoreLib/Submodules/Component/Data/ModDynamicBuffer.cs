using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace CoreLib.Submodules.ModComponent
{
    /// <summary>
    /// An array-like data structure that can be used as a component.
    /// </summary>
    /// <example>
    /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.class"/>
    /// </example>
    /// <typeparam name="T">The data type stored in the buffer. Must be a value type.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    public unsafe struct ModDynamicBuffer<T> : IList<T>
        where T : unmanaged
    {
        BufferHeader* m_Buffer;

        // Stores original internal capacity of the buffer header, so heap excess can be removed entirely when trimming.
        private int m_InternalCapacity;
        
        internal ModDynamicBuffer(BufferHeader* header, int internalCapacity)
        {
            m_Buffer = header;
            m_InternalCapacity = internalCapacity;
        }

        /// <summary>
        /// The number of elements the buffer holds.
        /// </summary>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.length"/>
        /// </example>
        public int Length
        {
            get => m_Buffer->Length;
            set => ResizeUninitialized(value);
        }

        /// <summary>
        /// The number of elements the buffer can hold.
        /// </summary>
        /// <remarks>
        /// <paramref name="Capacity"/> can not be set lower than <see cref="Length"/> - this will raise an exception.
        /// If <paramref name="Capacity"/> grows greater than the internal capacity of the DynamicBuffer, memory external to the DynamicBuffer will be allocated.
        /// If <paramref name="Capacity"/> shrinks to the internal capacity of the DynamicBuffer or smaller, memory external to the DynamicBuffer will be freed.
        /// No effort is made to avoid costly reallocations when <paramref name="Capacity"/> changes slightly;
        /// if <paramref name="Capacity"/> is incremented by 1, an array 1 element bigger is allocated.
        /// </remarks>
        public int Capacity
        {
            get => m_Buffer->Capacity;
            set => BufferHeader.SetCapacity(m_Buffer, value, ModUnsafe.SizeOf<T>(), ModUnsafe.AlignOf<T>(), BufferHeader.TrashMode.RetainOldData, false, 0, m_InternalCapacity);
        }

        /// <summary>
        /// Reports whether container is empty.
        /// </summary>
        /// <value>True if this container empty.</value>
        public bool IsEmpty => !IsCreated || Length == 0;

        /// <summary>
        /// Whether the memory for this dynamic buffer has been allocated.
        /// </summary>
        public bool IsCreated => m_Buffer != null;

        /// <summary>
        /// Array-like indexing operator.
        /// </summary>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.indexoperator"/>
        /// </example>
        /// <param name="index">The zero-based index.</param>
        public T this[int index]
        {
            get => ModUnsafe.ReadArrayElement<T>(BufferHeader.GetElementPointer(m_Buffer), index);
            set => ModUnsafe.WriteArrayElement<T>(BufferHeader.GetElementPointer(m_Buffer), index, value);
        }

        /// <summary>
        /// Return a reference to the element at index.
        /// </summary>
        /// <param name="index">The zero-based index.</param>
        /// <returns></returns>
        public ref T ElementAt(int index)
        {
            return ref ModUnsafe.ArrayElementAsRef<T>(BufferHeader.GetElementPointer(m_Buffer), index);
        }

        /// <summary>
        /// Increases the buffer capacity and length.
        /// </summary>
        /// <remarks>If <paramref name="length"/> is less than the current
        /// length of the buffer, the length of the buffer is reduced while the
        /// capacity remains unchanged.</remarks>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.resizeuninitialized"/>
        /// </example>
        /// <param name="length">The new length of the buffer.</param>
        public void ResizeUninitialized(int length)
        {
            EnsureCapacity(length);
            m_Buffer->Length = length;
        }

        /// <summary>
        /// Ensures that the buffer has at least the specified capacity.
        /// </summary>
        /// <remarks>If <paramref name="length"/> is greater than the current <see cref="Capacity"/>
        /// of this buffer and greater than the capacity reserved with
        /// <see cref="InternalBufferCapacityAttribute"/>, this function allocates a new memory block
        /// and copies the current buffer to it. The number of elements in the buffer remains
        /// unchanged.</remarks>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.reserve"/>
        /// </example>
        /// <param name="length">The buffer capacity is ensured to be at least this big.</param>
        public void EnsureCapacity(int length)
        {
            BufferHeader.EnsureCapacity(m_Buffer, length, ModUnsafe.SizeOf<T>(), ModUnsafe.AlignOf<T>(), BufferHeader.TrashMode.RetainOldData, false, 0);
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        /// <summary>
        /// Sets the buffer length to zero.
        /// </summary>
        /// <remarks>The capacity of the buffer remains unchanged. Buffer memory
        /// is not overwritten.</remarks>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.clear"/>
        /// </example>
        public void Clear()
        {
            m_Buffer->Length = 0;
        }

        public bool Contains(T item)
        {
            for (int i = 0; i < Length; i++)
            {
                if (this[i].Equals(item))
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
                RemoveAt(index);
            return index >= 0;
        }

        public int Count => Length;
        public bool IsReadOnly => false;

        /// <summary>
        /// Removes any excess capacity in the buffer.
        /// </summary>
        /// <remarks>Sets the buffer capacity to the current length.
        /// If the buffer memory size changes, the current contents
        /// of the buffer are copied to a new block of memory and the
        /// old memory is freed. If the buffer now fits in the space in the
        /// chunk reserved with <see cref="InternalBufferCapacityAttribute"/>,
        /// then the buffer contents are moved to the chunk.</remarks>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.trimexcess"/>
        /// </example>
        public void TrimExcess()
        {
            byte* oldPtr = m_Buffer->Pointer;
            int length = m_Buffer->Length;

            if (length == Capacity || oldPtr == null)
                return;

            int elemSize = ModUnsafe.SizeOf<T>();
            int elemAlign = ModUnsafe.AlignOf<T>();

            bool isInternal;
            byte* newPtr;

            // If the size fits in the internal buffer, prefer to move the elements back there.
            if (length <= m_InternalCapacity)
            {
                newPtr = (byte*)(m_Buffer + 1);
                isInternal = true;
            }
            else
            {
                newPtr = (byte*)Memory.Unmanaged.Allocate((long)elemSize * length, elemAlign, Allocator.Persistent);
                isInternal = false;
            }

            UnsafeUtility.MemCpy(newPtr, oldPtr, (long)elemSize * length);

            m_Buffer->Capacity = Math.Max(length, m_InternalCapacity);
            m_Buffer->Pointer = isInternal ? null : newPtr;

            Memory.Unmanaged.Free(oldPtr, Allocator.Persistent);
        }

        /// <summary>
        /// Adds an element to the end of the buffer, resizing as necessary.
        /// </summary>
        /// <remarks>The buffer is resized if it has no additional capacity.</remarks>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.add"/>
        /// </example>
        /// <param name="elem">The element to add to the buffer.</param>
        /// <returns>The index of the added element, which is equal to the new length of the buffer minus one.</returns>
        public int Add(T elem)
        {
            int length = Length;
            ResizeUninitialized(length + 1);
            this[length] = elem;
            return length;
        }

        public int IndexOf(T item)
        {
            for (int i = 0; i < Length; i++)
            {
                if (this[i].Equals(item))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Inserts an element at the specified index, resizing as necessary.
        /// </summary>
        /// <remarks>The buffer is resized if it has no additional capacity.</remarks>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.insert"/>
        /// </example>
        /// <param name="index">The position at which to insert the new element.</param>
        /// <param name="elem">The element to add to the buffer.</param>
        public void Insert(int index, T elem)
        {
            int length = Length;
            ResizeUninitialized(length + 1);
            int elemSize = ModUnsafe.SizeOf<T>();
            byte* basePtr = BufferHeader.GetElementPointer(m_Buffer);
            UnsafeUtility.MemMove(basePtr + (index + 1) * elemSize, basePtr + index * elemSize, (long)elemSize * (length - index));
            this[index] = elem;
        }

        /// <summary>
        /// Adds all the elements from <paramref name="newElems"/> to the end
        /// of the buffer, resizing as necessary.
        /// </summary>
        /// <remarks>The buffer is resized if it has no additional capacity.</remarks>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.addrange"/>
        /// </example>
        /// <param name="newElems">The native array of elements to insert.</param>
        public void AddRange(ModNativeArray<T> newElems)
        {
            int elemSize = ModUnsafe.SizeOf<T>();
            int oldLength = Length;
            ResizeUninitialized(oldLength + newElems.Length);

            byte* basePtr = BufferHeader.GetElementPointer(m_Buffer);
            UnsafeUtility.MemCpy(basePtr + (long)oldLength * elemSize, newElems.GetUnsafePtr(), (long)elemSize * newElems.Length);
        }

        /// <summary>
        /// Removes the specified number of elements, starting with the element at the specified index.
        /// </summary>
        /// <remarks>The buffer capacity remains unchanged.</remarks>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.removerange"/>
        /// </example>
        /// <param name="index">The first element to remove.</param>
        /// <param name="count">How many elements tot remove.</param>
        public void RemoveRange(int index, int count)
        {
            if (count == 0)
                return;

            int elemSize = ModUnsafe.SizeOf<T>();
            byte* basePtr = BufferHeader.GetElementPointer(m_Buffer);

            UnsafeUtility.MemMove(basePtr + index * elemSize, basePtr + (index + count) * elemSize, (long)elemSize * (Length - count - index));

            m_Buffer->Length -= count;
        }

        /// <summary>
        /// Removes the specified number of elements, starting with the element at the specified index. It replaces the
        /// elements that were removed with a range of elements from the back of the buffer. This is more efficient
        /// than moving all elements following the removed elements, but does change the order of elements in the buffer.
        /// </summary>
        /// <remarks>The buffer capacity remains unchanged.</remarks>
        /// <param name="index">The first element to remove.</param>
        /// <param name="count">How many elements tot remove.</param>
        public void RemoveRangeSwapBack(int index, int count)
        {
            if (count == 0)
                return;

            ref var l = ref m_Buffer->Length;
            byte* basePtr = BufferHeader.GetElementPointer(m_Buffer);
            int elemSize = ModUnsafe.SizeOf<T>();
            int copyFrom = math.max(l - count, index + count);
            void* dst = basePtr + index * elemSize;
            void* src = basePtr + copyFrom * elemSize;
            UnsafeUtility.MemMove(dst, src, (l - copyFrom) * elemSize);
            l -= count;
        }

        /// <summary>
        /// Removes the element at the specified index.
        /// </summary>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.removeat"/>
        /// </example>
        /// <param name="index">The index of the element to remove.</param>
        public void RemoveAt(int index)
        {
            RemoveRange(index, 1);
        }

        /// <summary>
        /// Removes the element at the specified index and swaps the last element into its place. This is more efficient
        /// than moving all elements following the removed element, but does change the order of elements in the buffer.
        /// </summary>
        /// <param name="index">The index of the element to remove.</param>
        public void RemoveAtSwapBack(int index)
        {
            ref var l = ref m_Buffer->Length;
            l -= 1;
            int newLength = l;
            if (index != newLength)
            {
                byte* basePtr = BufferHeader.GetElementPointer(m_Buffer);
                ModUnsafe.WriteArrayElement(basePtr, index, ModUnsafe.ReadArrayElement<T>(basePtr, newLength));
            }
        }

        /// <summary>
        /// Gets an <see langword="unsafe"/> read/write pointer to the contents of the buffer.
        /// </summary>
        /// <remarks>This function can only be called in unsafe code contexts.</remarks>
        /// <returns>A typed, unsafe pointer to the first element in the buffer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetUnsafePtr()
        {
            return BufferHeader.GetElementPointer(m_Buffer);
        }

        /// <summary>
        /// Gets an <see langword="unsafe"/> read-only pointer to the contents of the buffer.
        /// </summary>
        /// <remarks>This function can only be called in unsafe code contexts.</remarks>
        /// <returns>A typed, unsafe pointer to the first element in the buffer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetUnsafeReadOnlyPtr()
        {
            return BufferHeader.GetElementPointer(m_Buffer);
        }

        /// <summary>
        /// Returns a dynamic buffer of a different type, pointing to the same buffer memory.
        /// </summary>
        /// <remarks>No memory modification occurs. The reinterpreted type must be the same size
        /// in memory as the original type.</remarks>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.reinterpret"/>
        /// </example>
        /// <typeparam name="U">The reinterpreted type.</typeparam>
        /// <returns>A dynamic buffer of the reinterpreted type.</returns>
        /// <exception cref="InvalidOperationException">If the reinterpreted type is a different
        /// size than the original.</exception>
        public ModDynamicBuffer<U> Reinterpret<U>() where U : unmanaged
        {
            return new ModDynamicBuffer<U>(m_Buffer, m_InternalCapacity);
        }

        /// <summary>
        /// Return a native array that aliases the original buffer contents.
        /// </summary>
        /// <remarks>You can only access the native array as long as the
        /// the buffer memory has not been reallocated. Several dynamic buffer operations,
        /// such as <see cref="Add"/> and <see cref="TrimExcess"/> can result in
        /// buffer reallocation.</remarks>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.asnativearray"/>
        /// </example>
        public ModNativeArray<T> AsNativeArray()
        {
            return ModNativeArray<T>.ConvertExistingDataToNativeArray<T>(BufferHeader.GetElementPointer(m_Buffer), Length, Allocator.None);
        }
        
        public NativeArray<T> AsIl2CppNativeArray()
        {
            return ModNativeArray<T>.ConvertExistingDataToil2CppNativeArray<T>(BufferHeader.GetElementPointer(m_Buffer), Length, Allocator.None);
        }
        

        /// <summary>
        /// Provides an enumerator for iterating over the buffer elements.
        /// </summary>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.getenumerator"/>
        /// </example>
        /// <returns>The enumerator.</returns>
        public ModNativeArray<T>.Enumerator GetEnumerator()
        {
            var array = AsNativeArray();
            return new ModNativeArray<T>.Enumerator(ref array);
        }

        IEnumerator IEnumerable.GetEnumerator() => new BufferEnumerator(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new BufferEnumerator(this);

        /// <summary>
        /// Copies the buffer into a new native array.
        /// </summary>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.tonativearray"/>
        /// </example>
        /// <param name="allocator">The type of memory allocation to use when creating the
        /// native array.</param>
        /// <returns>A native array containing copies of the buffer elements.</returns>
        public ModNativeArray<T> ToNativeArray(Allocator allocator)
        {
            return new ModNativeArray<T>(AsNativeArray(), allocator);
        }
        
        public NativeArray<T> ToIl2CppNativeArray(Allocator allocator)
        {
            return new NativeArray<T>(AsIl2CppNativeArray(), allocator);
        }

        /// <summary>
        /// Copies all the elements from another dynamic buffer.
        /// </summary>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.copyfrom.dynamicbuffer"/>
        /// </example>
        /// <param name="v">The dynamic buffer containing the elements to copy.</param>
        public void CopyFrom(ModDynamicBuffer<T> v)
        {
            ResizeUninitialized(v.Length);

            UnsafeUtility.MemCpy(BufferHeader.GetElementPointer(m_Buffer),
                BufferHeader.GetElementPointer(v.m_Buffer), Length * ModUnsafe.SizeOf<T>());
        }

        /// <summary>
        /// Copies all the elements from an array.
        /// </summary>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.copyfrom.array"/>
        /// </example>
        /// <param name="v">A C# array containing the elements to copy.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void CopyFrom(T[] v)
        {
            if (v == null)
                throw new ArgumentNullException(nameof(v));
            
            ResizeUninitialized(v.Length);

            GCHandle gcHandle = GCHandle.Alloc(v, GCHandleType.Pinned);
            IntPtr num = gcHandle.AddrOfPinnedObject();

            UnsafeUtility.MemCpy(BufferHeader.GetElementPointer(m_Buffer), (void*)num, Length * ModUnsafe.SizeOf<T>());
            gcHandle.Free();
        }
        
        public struct BufferEnumerator : IEnumerator<T>
        {
            private readonly ModDynamicBuffer<T> _buffer;
            private int _index;
            private T _current;

            internal BufferEnumerator(ModDynamicBuffer<T> buffer)
            {
                _buffer = buffer;
                _index = 0;
                _current = default;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                if ((uint)_index < (uint)_buffer.Length)
                {
                    _current = _buffer[_index];
                    _index++;
                    return true;
                }

                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                _index = _buffer.Length + 1;
                _current = default;
                return false;
            }

            public T Current => _current;

            object IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _buffer.Length + 1)
                    {
                        throw new InvalidOperationException("Can't access Current, because state is invalid");
                    }

                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                _index = 0;
                _current = default;
            }
        }
    }
}