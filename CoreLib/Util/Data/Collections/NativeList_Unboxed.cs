using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace CoreLib.Util.Data
{
    /// <summary>
    /// Unboxed variant of NativeList&lt;T&gt;
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NativeList_Unboxed<T> : IEnumerable<T> where T : unmanaged
    {
        internal UnsafeList_Unboxed<T>* m_ListData;

        //Unity.Physics currently relies on the specific layout of NativeList in order to
        //workaround a b_ug in 19.1 & 19.2 with atomic safety handle in jobified Dispose.
        internal AllocatorManager.AllocatorHandle m_DeprecatedAllocator;

        /// <summary>
        /// Initializes and returns a NativeList with a capacity of one.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        public NativeList_Unboxed(AllocatorManager.AllocatorHandle allocator)
            : this(1, allocator, 2) { }

        /// <summary>
        /// Initializes and returns a NativeList.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list.</param>
        /// <param name="allocator">The allocator to use.</param>
        public NativeList_Unboxed(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
            : this(initialCapacity, allocator, 2) { }

        internal void Initialize(int initialCapacity, ref AllocatorManager.AllocatorHandle allocator, int disposeSentinelStackDepth)
        {
            var totalSize = sizeof(T) * (long)initialCapacity;

            m_ListData = UnsafeList_Unboxed<T>.Create(initialCapacity, ref allocator);
            m_DeprecatedAllocator = allocator.Handle;
        }

        internal static NativeList<T> New(int initialCapacity, ref AllocatorManager.AllocatorHandle allocator, int disposeSentinelStackDepth)
        {
            var nativelist = new NativeList<T>();
            nativelist.Initialize(initialCapacity, ref allocator, disposeSentinelStackDepth);
            return nativelist;
        }

        internal static NativeList<T> New(int initialCapacity, ref AllocatorManager.AllocatorHandle allocator)
        {
            return New(initialCapacity, ref allocator, 2);
        }

        NativeList_Unboxed(int initialCapacity, AllocatorManager.AllocatorHandle allocator, int disposeSentinelStackDepth)
        {
            this = default;
            AllocatorManager.AllocatorHandle temp = allocator;
            Initialize(initialCapacity, ref temp, disposeSentinelStackDepth);
        }

        /// <summary>
        /// The element at a given index.
        /// </summary>
        /// <param name="index">An index into this list.</param>
        /// <value>The value to store at the `index`.</value>
        /// <exception cref="IndexOutOfRangeException">Thrown if `index` is out of bounds.</exception>
        public T this[int index]
        {
            get { return (*m_ListData)[index]; }
            set { (*m_ListData)[index] = value; }
        }

        /// <summary>
        /// Returns a reference to the element at an index.
        /// </summary>
        /// <param name="index">An index.</param>
        /// <returns>A reference to the element at the index.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if index is out of bounds.</exception>
        public ref T ElementAt(int index)
        {
            return ref m_ListData->ElementAt(index);
        }

        /// <summary>
        /// The count of elements.
        /// </summary>
        /// <value>The current count of elements. Always less than or equal to the capacity.</value>
        /// <remarks>To decrease the memory used by a list, set <see cref="Capacity"/> after reducing the length of the list.</remarks>
        /// <param name="value>">The new length. If the new length is greater than the current capacity, the capacity is increased.
        /// Newly allocated memory is cleared.</param>
        public int Length
        {
            get { return CollectionHelper.AssumePositive(m_ListData->Length); }

            set { m_ListData->Resize(value, NativeArrayOptions.ClearMemory); }
        }

        /// <summary>
        /// The number of elements that fit in the current allocation.
        /// </summary>
        /// <value>The number of elements that fit in the current allocation.</value>
        /// <param name="value">The new capacity. Must be greater or equal to the length.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the new capacity is smaller than the length.</exception>
        public int Capacity
        {
            get { return m_ListData->Capacity; }

            set { m_ListData->Capacity = value; }
        }

        /// <summary>
        /// Returns the internal unsafe list.
        /// </summary>
        /// <remarks>Internally, the elements of a NativeList are stored in an UnsafeList.</remarks>
        /// <returns>The internal unsafe list.</returns>
        public UnsafeList_Unboxed<T>* GetUnsafeList() => m_ListData;

        /// <summary>
        /// Appends an element to the end of this list.
        /// </summary>
        /// <param name="value">The value to add to the end of this list.</param>
        /// <remarks>
        /// Length is incremented by 1. Will not increase the capacity.
        /// </remarks>
        /// <exception cref="Exception">Thrown if incrementing the length would exceed the capacity.</exception>
        public void AddNoResize(T value)
        {
            m_ListData->AddNoResize(value);
        }

        /// <summary>
        /// Appends elements from a buffer to the end of this list.
        /// </summary>
        /// <param name="ptr">The buffer to copy from.</param>
        /// <param name="count">The number of elements to copy from the buffer.</param>
        /// <remarks>
        /// Length is increased by the count. Will not increase the capacity.
        /// </remarks>
        /// <exception cref="Exception">Thrown if the increased length would exceed the capacity.</exception>
        public void AddRangeNoResize(void* ptr, int count)
        {
            m_ListData->AddRangeNoResize(ptr, count);
        }

        /// <summary>
        /// Appends the elements of another list to the end of this list.
        /// </summary>
        /// <param name="list">The other list to copy from.</param>
        /// <remarks>
        /// Length is increased by the length of the other list. Will not increase the capacity.
        /// </remarks>
        /// <exception cref="Exception">Thrown if the increased length would exceed the capacity.</exception>
        public void AddRangeNoResize(NativeList<T> list)
        {
            m_ListData->AddRangeNoResize(*list.m_ListData);
        }

        /// <summary>
        /// Appends an element to the end of this list.
        /// </summary>
        /// <param name="value">The value to add to the end of this list.</param>
        /// <remarks>
        /// Length is incremented by 1. If necessary, the capacity is increased.
        /// </remarks>
        public void Add(in T value)
        {
            m_ListData->Add(value);
        }

        /// <summary>
        /// Appends the elements of an array to the end of this list.
        /// </summary>
        /// <param name="array">The array to copy from.</param>
        /// <remarks>
        /// Length is increased by the number of new elements. Does not increase the capacity.
        /// </remarks>
        /// <exception cref="Exception">Thrown if the increased length would exceed the capacity.</exception>
        public void AddRange(NativeArray<T> array)
        {
            AddRange(array.GetUnsafeReadOnlyPtr(), array.Length);
        }

        /// <summary>
        /// Appends the elements of a buffer to the end of this list.
        /// </summary>
        /// <param name="ptr">The buffer to copy from.</param>
        /// <param name="count">The number of elements to copy from the buffer.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative.</exception>
        public void AddRange(void* ptr, int count)
        {
            m_ListData->AddRange(ptr, CollectionHelper.AssumePositive(count));
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
            m_ListData->InsertRangeWithBeginEnd(CollectionHelper.AssumePositive(begin), CollectionHelper.AssumePositive(end));
        }

        /// <summary>
        /// Copies the last element of this list to the specified index. Decrements the length by 1.
        /// </summary>
        /// <remarks>Useful as a cheap way to remove an element from this list when you don't care about preserving order.</remarks>
        /// <param name="index">The index to overwrite with the last element.</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if `index` is out of bounds.</exception>
        public void RemoveAtSwapBack(int index)
        {
            m_ListData->RemoveAtSwapBack(CollectionHelper.AssumePositive(index));
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
            m_ListData->RemoveRangeSwapBack(CollectionHelper.AssumePositive(index), CollectionHelper.AssumePositive(count));
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
            m_ListData->RemoveRangeSwapBackWithBeginEnd(CollectionHelper.AssumePositive(begin), CollectionHelper.AssumePositive(end));
        }

        /// <summary>
        /// Removes the element at an index, shifting everything above it down by one. Decrements the length by 1.
        /// </summary>
        /// <param name="index">The index of the item to remove.</param>
        /// <remarks>
        /// If you don't care about preserving the order of the elements, <see cref="RemoveAtSwapBack(int)"/> is a more efficient way to remove elements.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `index` is out of bounds.</exception>
        public void RemoveAt(int index)
        {
            m_ListData->RemoveAt(CollectionHelper.AssumePositive(index));
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
            m_ListData->RemoveRange(index, count);
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
            m_ListData->RemoveRangeWithBeginEnd(begin, end);
        }

        /// <summary>
        /// Whether this list is empty.
        /// </summary>
        /// <value>True if the list is empty or if the list has not been constructed.</value>
        public bool IsEmpty => !IsCreated || Length == 0;

        /// <summary>
        /// Whether this list has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this list has been allocated (and not yet deallocated).</value>
        public bool IsCreated => m_ListData != null;

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose()
        {
            UnsafeList_Unboxed<T>.Destroy(m_ListData);
            m_ListData = null;
        }

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// <typeparam name="U">The type of allocator.</typeparam>
        /// <param name="allocator">The allocator that was used to allocate this list.</param>
        /// </summary>
        internal void Dispose(ref AllocatorManager.AllocatorHandle allocator)
        {
            UnsafeList_Unboxed<T>.Destroy(m_ListData, ref allocator);
            m_ListData = null;
        }

        /// <summary>
        /// Sets the length to 0.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            m_ListData->Clear();
        }

        /// <summary>
        /// Returns a native array that aliases the content of this list.
        /// </summary>
        /// <returns>A native array that aliases the content of this list.</returns>
        public NativeArray<T> AsArray()
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(m_ListData->Ptr, m_ListData->Length, Allocator.None);
            return array;
        }

        public NativeArray<T> AsDeferredJobArray()
        {
            byte* buffer = (byte*)m_ListData;
            // We use the first bit of the pointer to infer that the array is in list mode
            // Thus the job scheduling code will need to patch it.
            buffer += 1;
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(buffer, 0, Allocator.Invalid);

            return array;
        }

        /// <summary>
        /// Returns an enumerator over the elements of this list.
        /// </summary>
        /// <returns>An enumerator over the elements of this list.</returns>
        public NativeArray<T>.Enumerator GetEnumerator()
        {
            var array = AsArray();
            return new NativeArray<T>.Enumerator(ref array);
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
        /// Overwrites the elements of this list with the elements of an equal-length array.
        /// </summary>
        /// <param name="array">An array to copy into this list.</param>
        /// <exception cref="ArgumentException">Thrown if the array and list have unequal length.</exception>
        public void CopyFrom(NativeArray<T> array)
        {
            Clear();
            Resize(array.Length, NativeArrayOptions.UninitializedMemory);
            NativeArray<T> thisArray = AsArray();
            thisArray.CopyFrom(array);
        }

        /// <summary>
        /// Sets the length of this list, increasing the capacity if necessary.
        /// </summary>
        /// <param name="length">The new length of this list.</param>
        /// <param name="options">Whether to clear any newly allocated bytes to all zeroes.</param>
        public void Resize(int length, NativeArrayOptions options)
        {
            m_ListData->Resize(length, options);
        }

        /// <summary>
        /// Sets the length of this list, increasing the capacity if necessary.
        /// </summary>
        /// <remarks>Does not clear newly allocated bytes.</remarks>
        /// <param name="length">The new length of this list.</param>
        public void ResizeUninitialized(int length)
        {
            Resize(length, NativeArrayOptions.UninitializedMemory);
        }

        /// <summary>
        /// Sets the capacity.
        /// </summary>
        /// <param name="capacity">The new capacity.</param>
        public void SetCapacity(int capacity)
        {
            m_ListData->SetCapacity(capacity);
        }

        /// <summary>
        /// Sets the capacity to match the length.
        /// </summary>
        public void TrimExcess()
        {
            m_ListData->TrimExcess();
        }

        /// <summary>
        /// Returns a parallel reader of this list.
        /// </summary>
        /// <returns>A parallel reader of this list.</returns>
        public NativeArray<T>.ReadOnly AsParallelReader()
        {
            return new NativeArray<T>.ReadOnly(m_ListData->Ptr, m_ListData->Length);
        }

        /// <summary>
        /// Returns a parallel writer of this list.
        /// </summary>
        /// <returns>A parallel writer of this list.</returns>
        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter(m_ListData);
        }

        /// <summary>
        /// A parallel writer for a NativeList.
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
            /// The internal unsafe list.
            /// </summary>
            /// <value>The internal unsafe list.</value>
            public UnsafeList_Unboxed<T>* ListData;

            internal unsafe ParallelWriter(UnsafeList_Unboxed<T>* listData)
            {
                ListData = listData;
            }

            /// <summary>
            /// Appends an element to the end of this list.
            /// </summary>
            /// <param name="value">The value to add to the end of this list.</param>
            /// <remarks>
            /// Increments the length by 1 unless doing so would exceed the current capacity.
            /// </remarks>
            /// <exception cref="Exception">Thrown if adding an element would exceed the capacity.</exception>
            public void AddNoResize(T value)
            {
                var idx = Interlocked.Increment(ref ListData->m_length) - 1;

                UnsafeUtility.WriteArrayElement(ListData->Ptr, idx, value);
            }

            /// <summary>
            /// Appends elements from a buffer to the end of this list.
            /// </summary>
            /// <param name="ptr">The buffer to copy from.</param>
            /// <param name="count">The number of elements to copy from the buffer.</param>
            /// <remarks>
            /// Increments the length by `count` unless doing so would exceed the current capacity.
            /// </remarks>
            /// <exception cref="Exception">Thrown if adding the elements would exceed the capacity.</exception>
            public void AddRangeNoResize(void* ptr, int count)
            {
                var idx = Interlocked.Add(ref ListData->m_length, count) - count;

                var sizeOf = sizeof(T);
                void* dst = (byte*)ListData->Ptr + idx * sizeOf;
                UnsafeUtility.MemCpy(dst, ptr, count * sizeOf);
            }

            /// <summary>
            /// Appends the elements of another list to the end of this list.
            /// </summary>
            /// <param name="list">The other list to copy from.</param>
            /// <remarks>
            /// Increments the length of this list by the length of the other list unless doing so would exceed the current capacity.
            /// </remarks>
            /// <exception cref="Exception">Thrown if adding the elements would exceed the capacity.</exception>
            public void AddRangeNoResize(UnsafeList<T> list)
            {
                AddRangeNoResize(list.Ptr, list.Length);
            }

            /// <summary>
            /// Appends the elements of another list to the end of this list.
            /// </summary>
            /// <param name="list">The other list to copy from.</param>
            /// <remarks>
            /// Increments the length of this list by the length of the other list unless doing so would exceed the current capacity.
            /// </remarks>
            /// <exception cref="Exception">Thrown if adding the elements would exceed the capacity.</exception>
            public void AddRangeNoResize(NativeList<T> list)
            {
                AddRangeNoResize(*list.m_ListData);
            }
        }
    }
}