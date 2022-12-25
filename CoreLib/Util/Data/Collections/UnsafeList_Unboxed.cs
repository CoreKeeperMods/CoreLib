using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using CoreLib.Util.Extensions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace CoreLib.Util.Data
{
    /// <summary>
    /// An unmanaged, resizable list.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafeList_Unboxed<T> : IEnumerable<T> where T : unmanaged
    {
        // <WARNING>
        // 'Header' of this struct must binary match 'UntypedUnsafeList' struct
        // Fields must match UntypedUnsafeList structure, please don't reorder and don't insert anything in between first 4 fields

        /// <summary>
        /// The internal buffer of this list.
        /// </summary>
        public T* Ptr;

        /// <summary>
        /// The number of elements.
        /// </summary>
        public int m_length;

        /// <summary>
        /// The number of elements that can fit in the internal buffer.
        /// </summary>
        public int m_capacity;

        /// <summary>
        /// The allocator used to create the internal buffer.
        /// </summary>
        public AllocatorManager.AllocatorHandle Allocator;

        [Obsolete("Use Length property (UnityUpgradable) -> Length", true)]
        public int length;

        [Obsolete("Use Capacity property (UnityUpgradable) -> Capacity", true)]
        public int capacity;

        /// <summary>
        /// The number of elements.
        /// </summary>
        /// <value>The number of elements.</value>
        public int Length
        {
            get { return CollectionHelper.AssumePositive(m_length); }

            set
            {
                if (value > Capacity)
                {
                    Resize(value);
                }
                else
                {
                    m_length = value;
                }
            }
        }

        /// <summary>
        /// The number of elements that can fit in the internal buffer.
        /// </summary>
        /// <value>The number of elements that can fit in the internal buffer.</value>
        public int Capacity
        {
            get { return CollectionHelper.AssumePositive(m_capacity); }

            set { SetCapacity(value); }
        }

        /// <summary>
        /// The element at an index.
        /// </summary>
        /// <param name="index">An index.</param>
        /// <value>The element at the index.</value>
        public T this[int index]
        {
            get { return Ptr[CollectionHelper.AssumePositive(index)]; }

            set { Ptr[CollectionHelper.AssumePositive(index)] = value; }
        }

        /// <summary>
        /// Returns a reference to the element at a given index.
        /// </summary>
        /// <param name="index">The index to access. Must be in the range of [0..Length).</param>
        /// <returns>A reference to the element at the index.</returns>
        public ref T ElementAt(int index)
        {
            return ref Ptr[CollectionHelper.AssumePositive(index)];
        }

        /// <summary>
        /// Initializes and returns an instance of UnsafeList.
        /// </summary>
        /// <param name="ptr">An existing byte array to set as the internal buffer.</param>
        /// <param name="length">The length.</param>
        public UnsafeList_Unboxed(T* ptr, int length) : this()
        {
            Ptr = ptr;
            this.m_length = length;
            m_capacity = 0;
            Allocator = AllocatorManager.None;
        }

        /// <summary>
        /// Initializes and returns an instance of UnsafeList.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        public UnsafeList_Unboxed(int initialCapacity, AllocatorManager.AllocatorHandle allocator,
            NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) : this()
        {
            Ptr = null;
            m_length = 0;
            m_capacity = 0;
            Allocator = allocator;

            if (initialCapacity != 0)
            {
                SetCapacity(initialCapacity);
            }

            if (options == NativeArrayOptions.ClearMemory && Ptr != null)
            {
                var sizeOf = sizeof(T);
                UnsafeUtility.MemClear(Ptr, Capacity * sizeOf);
            }
        }

        internal void Initialize(int initialCapacity, ref AllocatorManager.AllocatorHandle allocator,
            NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            Ptr = null;
            m_length = 0;
            m_capacity = 0;
            Allocator = AllocatorManager.None;
            Initialize(initialCapacity, ref allocator, options);
        }

        internal static UnsafeList_Unboxed<T> New(int initialCapacity, ref AllocatorManager.AllocatorHandle allocator,
            NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            UnsafeList_Unboxed<T> instance = default;
            instance.Initialize(initialCapacity, ref allocator, options);
            return instance;
        }

        internal static UnsafeList_Unboxed<T>* Create(int initialCapacity, ref AllocatorManager.AllocatorHandle allocator,
            NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            UnsafeList_Unboxed<T>* listData = allocator.Allocate(default(UnsafeList_Unboxed<T>), 1);
            UnsafeUtility.MemClear(listData, sizeof(UnsafeList_Unboxed<T>));

            listData->Allocator = allocator.Handle;

            if (initialCapacity != 0)
            {
                listData->SetCapacity(ref allocator, initialCapacity);
            }

            if (options == NativeArrayOptions.ClearMemory
                && listData->Ptr != null)
            {
                var sizeOf = sizeof(T);
                UnsafeUtility.MemClear(listData->Ptr, listData->Capacity * sizeOf);
            }

            return listData;
        }

        internal static void Destroy(UnsafeList_Unboxed<T>* listData, ref AllocatorManager.AllocatorHandle allocator)
        {
            listData->Dispose(ref allocator);
            allocator.Free(listData, sizeof(UnsafeList_Unboxed<T>), UnsafeUtility.AlignOf<UnsafeList_Unboxed<T>>(), 1);
        }

        /// <summary>
        /// Returns a new list.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        /// <returns>A pointer to the new list.</returns>
        public static UnsafeList_Unboxed<T>* Create(int initialCapacity, AllocatorManager.AllocatorHandle allocator,
            NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            UnsafeList_Unboxed<T>* listData = AllocatorManager.Allocate<UnsafeList_Unboxed<T>>(allocator);
            *listData = new UnsafeList_Unboxed<T>(initialCapacity, allocator, options);

            return listData;
        }

        /// <summary>
        /// Destroys the list.
        /// </summary>
        /// <param name="listData">The list to destroy.</param>
        public static void Destroy(UnsafeList_Unboxed<T>* listData)
        {
            var allocator = listData->Allocator;
            listData->Dispose();
            allocator.Free(listData, 1);
        }

        /// <summary>
        /// Whether the list is empty.
        /// </summary>
        /// <value>True if the list is empty or the list has not been constructed.</value>
        public bool IsEmpty => !IsCreated || m_length == 0;

        /// <summary>
        /// Whether this list has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this list has been allocated (and not yet deallocated).</value>
        public bool IsCreated => Ptr != null;

        internal void Dispose(ref AllocatorManager.AllocatorHandle allocator)
        {
            allocator.Free(Ptr, m_length);
            Ptr = null;
            m_length = 0;
            m_capacity = 0;
        }

        /// <summary>
        /// Releases all resources (memory).
        /// </summary>
        public void Dispose()
        {
            if (CollectionHelper.ShouldDeallocate(Allocator))
            {
                AllocatorManager.Free(Allocator, Ptr);
                Allocator = AllocatorManager.Invalid;
            }

            Ptr = null;
            m_length = 0;
            m_capacity = 0;
        }

        /// <summary>
        /// Sets the length to 0.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            m_length = 0;
        }

        /// <summary>
        /// Sets the length, expanding the capacity if necessary.
        /// </summary>
        /// <param name="length">The new length.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        public void Resize(int length, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            var oldLength = m_length;

            if (length > Capacity)
            {
                SetCapacity(length);
            }

            m_length = length;

            if (options == NativeArrayOptions.ClearMemory && oldLength < length)
            {
                var num = length - oldLength;
                byte* ptr = (byte*)Ptr;
                var sizeOf = sizeof(T);
                UnsafeUtility.MemClear(ptr + oldLength * sizeOf, num * sizeOf);
            }
        }

        void Realloc(ref AllocatorManager.AllocatorHandle allocator, int newCapacity)
        {
            T* newPointer = null;

            var alignOf = UnsafeUtility.AlignOf<T>();
            var sizeOf = sizeof(T);

            if (newCapacity > 0)
            {
                newPointer = (T*)allocator.Allocate(sizeOf, alignOf, newCapacity);

                if (m_capacity > 0)
                {
                    var itemsToCopy = math.min(newCapacity, Capacity);
                    var bytesToCopy = itemsToCopy * sizeOf;
                    UnsafeUtility.MemCpy(newPointer, Ptr, bytesToCopy);
                }
            }

            allocator.Free(Ptr, Capacity);

            Ptr = newPointer;
            m_capacity = newCapacity;
            m_length = math.min(m_length, newCapacity);
        }

        void Realloc(int capacity)
        {
            Realloc(ref Allocator, capacity);
        }

        void SetCapacity(ref AllocatorManager.AllocatorHandle allocator, int capacity)
        {
            var sizeOf = sizeof(T);
            var newCapacity = math.max(capacity, 64 / sizeOf);
            newCapacity = math.ceilpow2(newCapacity);

            if (newCapacity == Capacity)
            {
                return;
            }

            Realloc(ref allocator, newCapacity);
        }

        /// <summary>
        /// Sets the capacity.
        /// </summary>
        /// <param name="capacity">The new capacity.</param>
        public void SetCapacity(int capacity)
        {
            SetCapacity(ref Allocator, capacity);
        }

        /// <summary>
        /// Sets the capacity to match the length.
        /// </summary>
        public void TrimExcess()
        {
            if (Capacity != m_length)
            {
                Realloc(m_length);
            }
        }

        /// <summary>
        /// Adds an element to the end of this list.
        /// </summary>
        /// <remarks>
        /// Increments the length by 1. Never increases the capacity.
        /// </remarks>
        /// <param name="value">The value to add to the end of the list.</param>
        /// <exception cref="Exception">Thrown if incrementing the length would exceed the capacity.</exception>
        public void AddNoResize(T value)
        {
            UnsafeUtility.WriteArrayElement(Ptr, m_length, value);
            m_length += 1;
        }

        /// <summary>
        /// Copies elements from a buffer to the end of this list.
        /// </summary>
        /// <remarks>
        /// Increments the length by `count`. Never increases the capacity.
        /// </remarks>
        /// <param name="ptr">The buffer to copy from.</param>
        /// <param name="count">The number of elements to copy from the buffer.</param>
        /// <exception cref="Exception">Thrown if the increased length would exceed the capacity.</exception>
        public void AddRangeNoResize(void* ptr, int count)
        {
            var sizeOf = sizeof(T);
            void* dst = (byte*)Ptr + m_length * sizeOf;
            UnsafeUtility.MemCpy(dst, ptr, count * sizeOf);
            m_length += count;
        }

        /// <summary>
        /// Copies the elements of another list to the end of this list.
        /// </summary>
        /// <param name="list">The other list to copy from.</param>
        /// <remarks>
        /// Increments the length by the length of the other list. Never increases the capacity.
        /// </remarks>
        /// <exception cref="Exception">Thrown if the increased length would exceed the capacity.</exception>
        public void AddRangeNoResize(UnsafeList<T> list)
        {
            AddRangeNoResize(list.Ptr, CollectionHelper.AssumePositive(list.m_length));
        }

        /// <summary>
        /// Adds an element to the end of the list.
        /// </summary>
        /// <param name="value">The value to add to the end of this list.</param>
        /// <remarks>
        /// Increments the length by 1. Increases the capacity if necessary.
        /// </remarks>
        public void Add(in T value)
        {
            var idx = m_length;

            if (m_length + 1 > Capacity)
            {
                Resize(idx + 1);
            }
            else
            {
                m_length += 1;
            }

            UnsafeUtility.WriteArrayElement(Ptr, idx, value);
        }

        /// <summary>
        /// Copies the elements of a buffer to the end of this list.
        /// </summary>
        /// <param name="ptr">The buffer to copy from.</param>
        /// <param name="count">The number of elements to copy from the buffer.</param>
        /// <remarks>
        /// Increments the length by `count`. Increases the capacity if necessary.
        /// </remarks>
        public void AddRange(void* ptr, int count)
        {
            var idx = m_length;

            if (m_length + count > Capacity)
            {
                Resize(m_length + count);
            }
            else
            {
                m_length += count;
            }

            var sizeOf = sizeof(T);
            void* dst = (byte*)Ptr + idx * sizeOf;
            UnsafeUtility.MemCpy(dst, ptr, count * sizeOf);
        }

        /// <summary>
        /// Copies the elements of another list to the end of the list.
        /// </summary>
        /// <param name="list">The list to copy from.</param>
        /// <remarks>
        /// The length is increased by the length of the other list. Increases the capacity if necessary.
        /// </remarks>
        public void AddRange(UnsafeList<T> list)
        {
            AddRange(list.Ptr, list.Length);
        }

        /// <summary>
        /// Shifts elements toward the end of this list, increasing its length.
        /// </summary>
        /// <remarks>
        /// Right-shifts elements in the list so as to create 'free' slots at the beginning or in the middle.
        ///
        /// The length is increased by `end - begin`. If necessary, the capacity will be increased accordingly.
        ///
        /// If `end` equals `begin`, the method does nothing.
        ///
        /// The element at index `begin` will be copied to index `end`, the element at index `begin + 1` will be copied to `end + 1`, and so forth.
        ///
        /// The indexes `begin` up to `end` are not cleared: they will contain whatever values they held prior.
        /// </remarks>
        /// <param name="begin">The index of the first element that will be shifted up.</param>
        /// <param name="end">The index where the first shifted element will end up.</param>
        /// <exception cref="ArgumentException">Thrown if `end &lt; begin`.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `begin` or `end` are out of bounds.</exception>
        public void InsertRangeWithBeginEnd(int begin, int end)
        {
            int items = end - begin;
            if (items < 1)
            {
                return;
            }

            var oldLength = m_length;

            if (m_length + items > Capacity)
            {
                Resize(m_length + items);
            }
            else
            {
                m_length += items;
            }

            var itemsToCopy = oldLength - begin;

            if (itemsToCopy < 1)
            {
                return;
            }

            var sizeOf = sizeof(T);
            var bytesToCopy = itemsToCopy * sizeOf;
            unsafe
            {
                byte* ptr = (byte*)Ptr;
                byte* dest = ptr + end * sizeOf;
                byte* src = ptr + begin * sizeOf;
                UnsafeUtility.MemMove(dest, src, bytesToCopy);
            }
        }

        /// <summary>
        /// Copies the last element of this list to the specified index. Decrements the length by 1.
        /// </summary>
        /// <remarks>Useful as a cheap way to remove an element from this list when you don't care about preserving order.</remarks>
        /// <param name="index">The index to overwrite with the last element.</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if `index` is out of bounds.</exception>
        public void RemoveAtSwapBack(int index)
        {
            RemoveRangeSwapBack(index, 1);
        }

        /// <summary>
        /// Copies the last *N* elements of this list to a range in this list. Decrements the length by *N*.
        /// </summary>
        /// <remarks>
        /// Copies the last `count` elements to the indexes `index` up to `index + count`.
        ///
        /// Useful as a cheap way to remove elements from a list when you don't care about preserving order.
        /// </remarks>
        /// <param name="index">The index of the first element to overwrite.</param>
        /// <param name="count">The number of elements to copy and remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `index` is out of bounds, `count` is negative,
        /// or `index + count` exceeds the length.</exception>
        public void RemoveRangeSwapBack(int index, int count)
        {
            if (count > 0)
            {
                int copyFrom = math.max(m_length - count, index + count);
                var sizeOf = sizeof(T);
                void* dst = (byte*)Ptr + index * sizeOf;
                void* src = (byte*)Ptr + copyFrom * sizeOf;
                UnsafeUtility.MemCpy(dst, src, (m_length - copyFrom) * sizeOf);
                m_length -= count;
            }
        }

        /// <summary>
        /// Copies the last *N* elements of this list to a range in this list. Decrements the length by *N*.
        /// </summary>
        /// <remarks>
        /// Copies the last `end - begin` elements to the indexes `begin` up to `end`.
        ///
        /// Useful as a cheap way to remove elements from a list when you don't care about preserving order.
        ///
        /// Does nothing if `end - begin` is less than 1.
        /// </remarks>
        /// <param name="begin">The index of the first element to overwrite.</param>
        /// <param name="end">The index one greater than the last element to overwrite.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `begin` or `end` are out of bounds.</exception>
        [Obsolete("RemoveRangeSwapBackWithBeginEnd(begin, end) is deprecated, use RemoveRangeSwapBack(index, count) instead. (RemovedAfter 2021-06-02)", false)]
        public void RemoveRangeSwapBackWithBeginEnd(int begin, int end)
        {
            int itemsToRemove = end - begin;
            if (itemsToRemove > 0)
            {
                int copyFrom = math.max(m_length - itemsToRemove, end);
                var sizeOf = sizeof(T);
                void* dst = (byte*)Ptr + begin * sizeOf;
                void* src = (byte*)Ptr + copyFrom * sizeOf;
                UnsafeUtility.MemCpy(dst, src, (m_length - copyFrom) * sizeOf);
                m_length -= itemsToRemove;
            }
        }

        /// <summary>
        /// Removes the element at an index, shifting everything above it down by one. Decrements the length by 1.
        /// </summary>
        /// <param name="index">The index of the element to remove.</param>
        /// <remarks>
        /// If you don't care about preserving the order of the elements, <see cref="RemoveAtSwapBack(int)"/> is a more efficient way to remove elements.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `index` is out of bounds.</exception>
        public void RemoveAt(int index)
        {
            RemoveRange(index, 1);
        }

        /// <summary>
        /// Removes *N* elements in a range, shifting everything above the range down by *N*. Decrements the length by *N*.
        /// </summary>
        /// <param name="index">The index of the first element to remove.</param>
        /// <param name="count">The number of elements to remove.</param>
        /// <remarks>
        /// If you don't care about preserving the order of the elements, <see cref="RemoveRangeSwapBackWithBeginEnd"/>
        /// is a more efficient way to remove elements.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `index` is out of bounds, `count` is negative,
        /// or `index + count` exceeds the length.</exception>
        public void RemoveRange(int index, int count)
        {
            if (count > 0)
            {
                int copyFrom = math.min(index + count, m_length);
                var sizeOf = sizeof(T);
                void* dst = (byte*)Ptr + index * sizeOf;
                void* src = (byte*)Ptr + copyFrom * sizeOf;
                UnsafeUtility.MemCpy(dst, src, (m_length - copyFrom) * sizeOf);
                m_length -= count;
            }
        }

        /// <summary>
        /// Removes *N* elements in a range, shifting everything above it down by *N*. Decrements the length by *N*.
        /// </summary>
        /// <param name="begin">The index of the first element to remove.</param>
        /// <param name="end">The index one greater than the last element to remove.</param>
        /// <remarks>
        /// If you don't care about preserving the order of the elements, <see cref="RemoveRangeSwapBackWithBeginEnd"/> is a more efficient way to remove elements.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if `end &lt; begin`.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `begin` or `end` are out of bounds.</exception>
        [Obsolete("RemoveRangeWithBeginEnd(begin, end) is deprecated, use RemoveRange(index, count) instead. (RemovedAfter 2021-06-02)", false)]
        public void RemoveRangeWithBeginEnd(int begin, int end)
        {
            int itemsToRemove = end - begin;
            if (itemsToRemove > 0)
            {
                int copyFrom = math.min(begin + itemsToRemove, m_length);
                var sizeOf = sizeof(T);
                void* dst = (byte*)Ptr + begin * sizeOf;
                void* src = (byte*)Ptr + copyFrom * sizeOf;
                UnsafeUtility.MemCpy(dst, src, (m_length - copyFrom) * sizeOf);
                m_length -= itemsToRemove;
            }
        }

        /// <summary>
        /// Returns a parallel reader of this list.
        /// </summary>
        /// <returns>A parallel reader of this list.</returns>
        public ParallelReader AsParallelReader()
        {
            return new ParallelReader(Ptr, Length);
        }

        /// <summary>
        /// A parallel reader for an UnsafeList&lt;T&gt;.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelReader"/> to create a parallel reader for a list.
        /// </remarks>
        public unsafe struct ParallelReader
        {
            /// <summary>
            /// The internal buffer of the list.
            /// </summary>
            public readonly T* Ptr;

            /// <summary>
            /// The number of elements.
            /// </summary>
            public readonly int Length;

            internal ParallelReader(T* ptr, int length)
            {
                Ptr = ptr;
                Length = length;
            }
        }

        /// <summary>
        /// Returns a parallel writer of this list.
        /// </summary>
        /// <returns>A parallel writer of this list.</returns>
        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter((UnsafeList_Unboxed<T>*)UnsafeUtility.AddressOf(ref this));
        }

        /// <summary>
        /// A parallel writer for an UnsafeList&lt;T&gt;.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a list.
        /// </remarks>
        public unsafe struct ParallelWriter
        {
            /// <summary>
            /// The data of the list.
            /// </summary>
            public readonly void* Ptr => ListData->Ptr;

            /// <summary>
            /// The UnsafeList to write to.
            /// </summary>
            public UnsafeList_Unboxed<T>* ListData;

            internal unsafe ParallelWriter(UnsafeList_Unboxed<T>* listData)
            {
                ListData = listData;
            }

            /// <summary>
            /// Adds an element to the end of the list.
            /// </summary>
            /// <param name="value">The value to add to the end of the list.</param>
            /// <remarks>
            /// Increments the length by 1. Never increases the capacity.
            /// </remarks>
            /// <exception cref="Exception">Thrown if incrementing the length would exceed the capacity.</exception>
            public void AddNoResize(T value)
            {
                var idx = Interlocked.Increment(ref ListData->m_length) - 1;
                UnsafeUtility.WriteArrayElement(ListData->Ptr, idx, value);
            }

            /// <summary>
            /// Copies elements from a buffer to the end of the list.
            /// </summary>
            /// <param name="ptr">The buffer to copy from.</param>
            /// <param name="count">The number of elements to copy from the buffer.</param>
            /// <remarks>
            /// Increments the length by `count`. Never increases the capacity.
            /// </remarks>
            /// <exception cref="Exception">Thrown if the increased length would exceed the capacity.</exception>
            public void AddRangeNoResize(void* ptr, int count)
            {
                var idx = Interlocked.Add(ref ListData->m_length, count) - count;
                void* dst = (byte*)ListData->Ptr + idx * sizeof(T);
                UnsafeUtility.MemCpy(dst, ptr, count * sizeof(T));
            }

            /// <summary>
            /// Copies the elements of another list to the end of this list.
            /// </summary>
            /// <param name="list">The other list to copy from.</param>
            /// <remarks>
            /// Increments the length by the length of the other list. Never increases the capacity.
            /// </remarks>
            /// <exception cref="Exception">Thrown if the increased length would exceed the capacity.</exception>
            public void AddRangeNoResize(UnsafeList<T> list)
            {
                AddRangeNoResize(list.Ptr, list.Length);
            }
        }

        /// <summary>
        /// Overwrites the elements of this list with the elements of an equal-length array.
        /// </summary>
        /// <param name="array">An array to copy into this list.</param>
        public void CopyFrom(UnsafeList<T> array)
        {
            Resize(array.Length);
            UnsafeUtility.MemCpy(Ptr, array.Ptr, UnsafeUtility.SizeOf<T>() * Length);
        }


        /// <summary>
        /// Returns an enumerator over the elements of the list.
        /// </summary>
        /// <returns>An enumerator over the elements of the list.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator { m_Ptr = Ptr, m_Length = Length, m_Index = -1 };
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new Exception("Not Implemented");
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            throw new Exception("Not Implemented");
        }

        /// <summary>
        /// An enumerator over the elements of a list.
        /// </summary>
        /// <remarks>
        /// In an enumerator's initial state, <see cref="Current"/> is invalid.
        /// The first <see cref="MoveNext"/> call advances the enumerator to the first element of the list.
        /// </remarks>
        public struct Enumerator : IEnumerator<T>
        {
            internal T* m_Ptr;
            internal int m_Length;
            internal int m_Index;

            /// <summary>
            /// Does nothing.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next element of the list.
            /// </summary>
            /// <remarks>
            /// The first `MoveNext` call advances the enumerator to the first element of the list. Before this call, `Current` is not valid to read.
            /// </remarks>
            /// <returns>True if `Current` is valid to read after the call.</returns>
            public bool MoveNext() => ++m_Index < m_Length;

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset() => m_Index = -1;

            /// <summary>
            /// The current element.
            /// </summary>
            /// <value>The current element.</value>
            public T Current => m_Ptr[m_Index];

            object IEnumerator.Current => Current;
        }
    }

    /// <summary>
    /// Provides extension methods for UnsafeList.
    /// </summary>
    public unsafe static class UnsafeListExtensions
    {
        /// <summary>
        /// Finds the index of the first occurrence of a particular value in this list.
        /// </summary>
        /// <typeparam name="T">The type of elements in this list.</typeparam>
        /// <typeparam name="U">The type of value to locate.</typeparam>
        /// <param name="listUnboxed">This list.</param>
        /// <param name="value">A value to locate.</param>
        /// <returns>The zero-based index of the first occurrence of the value if it is found. Returns -1 if no occurrence is found.</returns>
        public static int IndexOf<T, U>(this UnsafeList_Unboxed<T> listUnboxed, U value) where T : unmanaged, IEquatable<U>
        {
            return NativeArrayExtensions.IndexOf<T, U>(listUnboxed.Ptr, listUnboxed.Length, value);
        }

        /// <summary>
        /// Returns true if a particular value is present in this list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <typeparam name="U">The type of value to locate.</typeparam>
        /// <param name="listUnboxed">This list.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True if the value is present in this list.</returns>
        public static bool Contains<T, U>(this UnsafeList_Unboxed<T> listUnboxed, U value) where T : unmanaged, IEquatable<U>
        {
            return listUnboxed.IndexOf(value) != -1;
        }

        /// <summary>
        /// Finds the index of the first occurrence of a particular value in the list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <typeparam name="U">The type of value to locate.</typeparam>
        /// <param name="list">This reader of the list.</param>
        /// <param name="value">A value to locate.</param>
        /// <returns>The zero-based index of the first occurrence of the value if it is found. Returns -1 if no occurrence is found.</returns>
        public static int IndexOf<T, U>(this UnsafeList_Unboxed<T>.ParallelReader list, U value) where T : unmanaged, IEquatable<U>
        {
            return NativeArrayExtensions.IndexOf<T, U>(list.Ptr, list.Length, value);
        }

        /// <summary>
        /// Returns true if a particular value is present in the list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <typeparam name="U">The type of value to locate.</typeparam>
        /// <param name="list">This reader of the list.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True if the value is present in the list.</returns>
        public static bool Contains<T, U>(this UnsafeList_Unboxed<T>.ParallelReader list, U value) where T : unmanaged, IEquatable<U>
        {
            return list.IndexOf(value) != -1;
        }


        /// <summary>
        /// Returns true if this array and another have equal length and content.
        /// </summary>
        /// <typeparam name="T">The type of the source array's elements.</typeparam>
        /// <param name="array">The array to compare for equality.</param>
        /// <param name="other">The other array to compare for equality.</param>
        /// <returns>True if the arrays have equal length and content.</returns>
        public static bool ArraysEqual<T>(this UnsafeList_Unboxed<T> array, UnsafeList_Unboxed<T> other) where T : unmanaged, IEquatable<T>
        {
            if (array.Length != other.Length)
                return false;

            for (int i = 0; i != array.Length; i++)
            {
                if (!array[i].Equals(other[i]))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// An unmanaged, resizable list of pointers.
    /// </summary>
    /// <typeparam name="T">The type of pointer element.</typeparam>
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}, IsEmpty = {IsEmpty}")]
    [DebuggerTypeProxy(typeof(UnsafePtrListTDebugView<>))]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafePtrList<T>
        : IEnumerable<IntPtr> // Used by collection initializers.
        where T : unmanaged
    {
        /// <summary>
        /// The internal buffer of this list.
        /// </summary>
        public readonly T** Ptr;

        /// <summary>
        /// The number of elements.
        /// </summary>
        public readonly int m_length;

        /// <summary>
        /// The number of elements that can fit in the internal buffer.
        /// </summary>
        public readonly int m_capacity;

        /// <summary>
        /// The allocator used to create the internal buffer.
        /// </summary>
        public readonly AllocatorManager.AllocatorHandle Allocator;

        [Obsolete("Use Length property (UnityUpgradable) -> Length", true)]
        public int length;

        [Obsolete("Use Capacity property (UnityUpgradable) -> Capacity", true)]
        public int capacity;

        /// <summary>
        /// The number of elements.
        /// </summary>
        /// <value>The number of elements.</value>
        public int Length
        {
            get { return this.ListData().Length; }

            set { this.ListData().Length = value; }
        }

        /// <summary>
        /// The number of elements that can fit in the internal buffer.
        /// </summary>
        /// <value>The number of elements that can fit in the internal buffer.</value>
        public int Capacity
        {
            get { return this.ListData().Capacity; }

            set { this.ListData().Capacity = value; }
        }

        /// <summary>
        /// The element at an index.
        /// </summary>
        /// <param name="index">An index.</param>
        /// <value>The element at the index.</value>
        public T* this[int index]
        {
            get { return Ptr[CollectionHelper.AssumePositive(index)]; }

            set { Ptr[CollectionHelper.AssumePositive(index)] = value; }
        }

        /// <summary>
        /// Returns a reference to the element at a given index.
        /// </summary>
        /// <param name="index">The index to access. Must be in the range of [0..Length).</param>
        /// <returns>A reference to the element at the index.</returns>
        public ref T* ElementAt(int index)
        {
            return ref Ptr[CollectionHelper.AssumePositive(index)];
        }

        /// <summary>
        /// Initializes and returns an instance of UnsafePtrList.
        /// </summary>
        /// <param name="ptr">An existing pointer array to set as the internal buffer.</param>
        /// <param name="length">The length.</param>
        public unsafe UnsafePtrList(T** ptr, int length) : this()
        {
            Ptr = ptr;
            this.m_length = length;
            this.m_capacity = length;
            Allocator = AllocatorManager.None;
        }

        /// <summary>
        /// Initializes and returns an instance of UnsafePtrList.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        public unsafe UnsafePtrList(int initialCapacity, AllocatorManager.AllocatorHandle allocator,
            NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) : this()
        {
            Ptr = null;
            m_length = 0;
            m_capacity = 0;
            Allocator = AllocatorManager.None;

            this.ListData() = new UnsafeList<IntPtr>(initialCapacity, allocator, options);
        }

        /// <summary>
        /// Returns a new list of pointers.
        /// </summary>
        /// <param name="ptr">An existing pointer array to set as the internal buffer.</param>
        /// <param name="length">The length.</param>
        /// <returns>A pointer to the new list.</returns>
        public static UnsafePtrList<T>* Create(T** ptr, int length)
        {
            UnsafePtrList<T>* listData = AllocatorManager.Allocate<UnsafePtrList<T>>(AllocatorManager.Persistent);
            *listData = new UnsafePtrList<T>(ptr, length);
            return listData;
        }

        /// <summary>
        /// Returns a new list of pointers.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        /// <returns>A pointer to the new list.</returns>
        public static UnsafePtrList<T>* Create(int initialCapacity, AllocatorManager.AllocatorHandle allocator,
            NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            UnsafePtrList<T>* listData = AllocatorManager.Allocate<UnsafePtrList<T>>(allocator);
            *listData = new UnsafePtrList<T>(initialCapacity, allocator, options);
            return listData;
        }

        /// <summary>
        /// Destroys the list.
        /// </summary>
        /// <param name="listData">The list to destroy.</param>
        public static void Destroy(UnsafePtrList<T>* listData)
        {
            var allocator = listData->ListData().Allocator.Value == AllocatorManager.Invalid.Value
                    ? AllocatorManager.Persistent
                    : listData->ListData().Allocator
                ;
            listData->Dispose();
            AllocatorManager.Free(allocator, listData);
        }

        /// <summary>
        /// Whether the list is empty.
        /// </summary>
        /// <value>True if the list is empty or the list has not been constructed.</value>
        public bool IsEmpty => !IsCreated || Length == 0;

        /// <summary>
        /// Whether this list has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this list has been allocated (and not yet deallocated).</value>
        public bool IsCreated => Ptr != null;

        /// <summary>
        /// Releases all resources (memory).
        /// </summary>
        public void Dispose()
        {
            this.ListData().Dispose();
        }

        /// <summary>
        /// Sets the length to 0.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear() => this.ListData().Clear();

        /// <summary>
        /// Sets the length, expanding the capacity if necessary.
        /// </summary>
        /// <param name="length">The new length.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        public void Resize(int length, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) => this.ListData().Resize(length, options);

        /// <summary>
        /// Sets the capacity.
        /// </summary>
        /// <param name="capacity">The new capacity.</param>
        public void SetCapacity(int capacity) => this.ListData().SetCapacity(capacity);

        /// <summary>
        /// Returns the index of the first occurrence of a specific pointer in the list.
        /// </summary>
        /// <param name="ptr">The pointer to search for in the list.</param>
        /// <returns>The index of the first occurrence of the pointer. Returns -1 if it is not found in the list.</returns>
        public int IndexOf(void* ptr)
        {
            for (int i = 0; i < Length; ++i)
            {
                if (Ptr[i] == ptr) return i;
            }

            return -1;
        }

        /// <summary>
        /// Returns true if the list contains at least one occurrence of a specific pointer.
        /// </summary>
        /// <param name="ptr">The pointer to search for in the list.</param>
        /// <returns>True if the list contains at least one occurrence of the pointer.</returns>
        public bool Contains(void* ptr)
        {
            return IndexOf(ptr) != -1;
        }

        /// <summary>
        /// Adds a pointer to the end of this list.
        /// </summary>
        /// <remarks>
        /// Increments the length by 1. Never increases the capacity.
        /// </remarks>
        /// <param name="value">The pointer to add to the end of the list.</param>
        /// <exception cref="Exception">Thrown if incrementing the length would exceed the capacity.</exception>
        public void AddNoResize(void* value)
        {
            this.ListData().AddNoResize((IntPtr)value);
        }

        /// <summary>
        /// Copies pointers from a buffer to the end of this list.
        /// </summary>
        /// <remarks>
        /// Increments the length by `count`. Never increases the capacity.
        /// </remarks>
        /// <param name="ptr">The buffer to copy from.</param>
        /// <param name="count">The number of pointers to copy from the buffer.</param>
        /// <exception cref="Exception">Thrown if the increased length would exceed the capacity.</exception>
        public void AddRangeNoResize(void** ptr, int count) => this.ListData().AddRangeNoResize(ptr, count);

        /// <summary>
        /// Copies the pointers of another list to the end of this list.
        /// </summary>
        /// <param name="list">The other list to copy from.</param>
        /// <remarks>
        /// Increments the length by the length of the other list. Never increases the capacity.
        /// </remarks>
        /// <exception cref="Exception">Thrown if the increased length would exceed the capacity.</exception>
        public void AddRangeNoResize(UnsafePtrList<T> list) => this.ListData().AddRangeNoResize(list.Ptr, list.Length);

        /// <summary>
        /// Adds elements from a buffer to this list.
        /// </summary>
        /// <param name="ptr">A pointer to the buffer.</param>
        /// <param name="length">The number of elements to add to the list.</param>
        public void AddRange(void* ptr, int length) => this.ListData().AddRange(ptr, length);

        /// <summary>
        /// Copies the elements of another list to the end of this list.
        /// </summary>
        /// <param name="list">The other list to copy from.</param>
        /// <remarks>
        /// Increments the length by the length of the other list. Increases the capacity if necessary.
        /// </remarks>
        public void AddRange(UnsafePtrList<T> list) => this.ListData().AddRange(list.ListData());

        /// <summary>
        /// Copies the last pointer of this list to the specified index. Decrements the length by 1.
        /// </summary>
        /// <remarks>Useful as a cheap way to remove a pointer from this list when you don't care about preserving order.</remarks>
        /// <param name="index">The index to overwrite with the last pointer.</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if `index` is out of bounds.</exception>
        public void RemoveAtSwapBack(int index) => this.ListData().RemoveAtSwapBack(index);

        /// <summary>
        /// Copies the last *N* pointer of this list to a range in this list. Decrements the length by *N*.
        /// </summary>
        /// <remarks>
        /// Copies the last `count` pointers to the indexes `index` up to `index + count`.
        ///
        /// Useful as a cheap way to remove pointers from a list when you don't care about preserving order.
        /// </remarks>
        /// <param name="index">The index of the first pointer to overwrite.</param>
        /// <param name="count">The number of pointers to copy and remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `index` is out of bounds, `count` is negative,
        /// or `index + count` exceeds the length.</exception>
        public void RemoveRangeSwapBack(int index, int count) => this.ListData().RemoveRangeSwapBack(index, count);

        /// <summary>
        /// Removes the pointer at an index, shifting everything above it down by one. Decrements the length by 1.
        /// </summary>
        /// <param name="index">The index of the pointer to remove.</param>
        /// <remarks>
        /// If you don't care about preserving the order of the pointers, <see cref="RemoveAtSwapBack(int)"/> is a more efficient way to remove pointers.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `index` is out of bounds.</exception>
        public void RemoveAt(int index) => this.ListData().RemoveAt(index);

        /// <summary>
        /// Removes *N* pointers in a range, shifting everything above the range down by *N*. Decrements the length by *N*.
        /// </summary>
        /// <param name="index">The index of the first pointer to remove.</param>
        /// <param name="count">The number of pointers to remove.</param>
        /// <remarks>
        /// If you don't care about preserving the order of the pointers, <see cref="RemoveRangeSwapBackWithBeginEnd"/>
        /// is a more efficient way to remove pointers.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `index` is out of bounds, `count` is negative,
        /// or `index + count` exceeds the length.</exception>
        public void RemoveRange(int index, int count) => this.ListData().RemoveRange(index, count);

        /// <summary>
        /// This method is not implemented. It will throw NotImplementedException if it is used.
        /// </summary>
        /// <remarks>Use Enumerator GetEnumerator() instead.</remarks>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new Exception("Not Implemented");
        }

        /// <summary>
        /// This method is not implemented. It will throw NotImplementedException if it is used.
        /// </summary>
        /// <remarks>Use Enumerator GetEnumerator() instead.</remarks>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<IntPtr> IEnumerable<IntPtr>.GetEnumerator()
        {
            throw new Exception("Not Implemented");
        }

        /// <summary>
        /// Returns a parallel reader of this list.
        /// </summary>
        /// <returns>A parallel reader of this list.</returns>
        public ParallelReader AsParallelReader()
        {
            return new ParallelReader(Ptr, Length);
        }

        /// <summary>
        /// A parallel reader for an UnsafePtrList&lt;T&gt;.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelReader"/> to create a parallel reader for a list.
        /// </remarks>
        public unsafe struct ParallelReader
        {
            /// <summary>
            /// The internal buffer of the list.
            /// </summary>
            public readonly T** Ptr;

            /// <summary>
            /// The number of elements.
            /// </summary>
            public readonly int Length;

            internal ParallelReader(T** ptr, int length)
            {
                Ptr = ptr;
                Length = length;
            }

            /// <summary>
            /// Returns the index of the first occurrence of a specific pointer in the list.
            /// </summary>
            /// <param name="ptr">The pointer to search for in the list.</param>
            /// <returns>The index of the first occurrence of the pointer. Returns -1 if it is not found in the list.</returns>
            public int IndexOf(void* ptr)
            {
                for (int i = 0; i < Length; ++i)
                {
                    if (Ptr[i] == ptr) return i;
                }

                return -1;
            }

            /// <summary>
            /// Returns true if the list contains at least one occurrence of a specific pointer.
            /// </summary>
            /// <param name="ptr">The pointer to search for in the list.</param>
            /// <returns>True if the list contains at least one occurrence of the pointer.</returns>
            public bool Contains(void* ptr)
            {
                return IndexOf(ptr) != -1;
            }
        }

        /// <summary>
        /// Returns a parallel writer of this list.
        /// </summary>
        /// <returns>A parallel writer of this list.</returns>
        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter(Ptr, (UnsafeList_Unboxed<IntPtr>*)UnsafeUtility.AddressOf(ref this));
        }

        /// <summary>
        /// A parallel writer for an UnsafePtrList&lt;T&gt;.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a list.
        /// </remarks>
        public unsafe struct ParallelWriter
        {
            /// <summary>
            /// The data of the list.
            /// </summary>
            public readonly T** Ptr;

            /// <summary>
            /// The UnsafeList to write to.
            /// </summary>
            public UnsafeList_Unboxed<IntPtr>* ListData;

            internal unsafe ParallelWriter(T** ptr, UnsafeList_Unboxed<IntPtr>* listData)
            {
                Ptr = ptr;
                ListData = listData;
            }

            /// <summary>
            /// Adds a pointer to the end of the list.
            /// </summary>
            /// <param name="value">The pointer to add to the end of the list.</param>
            /// <remarks>
            /// Increments the length by 1. Never increases the capacity.
            /// </remarks>
            /// <exception cref="Exception">Thrown if incrementing the length would exceed the capacity.</exception>
            public void AddNoResize(T* value) => ListData->AddNoResize((IntPtr)value);

            /// <summary>
            /// Copies pointers from a buffer to the end of the list.
            /// </summary>
            /// <param name="ptr">The buffer to copy from.</param>
            /// <param name="count">The number of pointers to copy from the buffer.</param>
            /// <remarks>
            /// Increments the length by `count`. Never increases the capacity.
            /// </remarks>
            /// <exception cref="Exception">Thrown if the increased length would exceed the capacity.</exception>
            public void AddRangeNoResize(T** ptr, int count) => ListData->AddRangeNoResize(ptr, count);

            /// <summary>
            /// Copies the pointers of another list to the end of this list.
            /// </summary>
            /// <param name="list">The other list to copy from.</param>
            /// <remarks>
            /// Increments the length by the length of the other list. Never increases the capacity.
            /// </remarks>
            /// <exception cref="Exception">Thrown if the increased length would exceed the capacity.</exception>
            public void AddRangeNoResize(UnsafePtrList<T> list) => ListData->AddRangeNoResize(list.Ptr, list.Length);
        }
    }

    internal static class UnsafePtrListTExtensions
    {
        public static ref UnsafeList<IntPtr> ListData<T>(ref this UnsafePtrList<T> from) where T : unmanaged =>
            ref UnsafeUtility.As<UnsafePtrList<T>, UnsafeList<IntPtr>>(ref from);
    }

    internal sealed class UnsafePtrListTDebugView<T>
        where T : unmanaged
    {
        UnsafePtrList<T> Data;

        public UnsafePtrListTDebugView(UnsafePtrList<T> data)
        {
            Data = data;
        }

        public unsafe T*[] Items
        {
            get
            {
                T*[] result = new T*[Data.Length];

                for (var i = 0; i < result.Length; ++i)
                {
                    result[i] = Data.Ptr[i];
                }

                return result;
            }
        }
    }
}