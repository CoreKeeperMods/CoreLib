using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

namespace CoreLib.Util.Extensions
{
    public static class AllocExtensions
    {
        internal static AllocatorManager.Block AllocateBlock(ref this AllocatorManager.AllocatorHandle t, int sizeOf, int alignOf, int items)
        {
            AllocatorManager.Block block = default;
            block.Range.Pointer = IntPtr.Zero;
            block.Range.Items = items;
            block.Range.Allocator = t.Handle;
            block.BytesPerItem = sizeOf;
            // Make the alignment multiple of cacheline size
            block.Alignment = math.max(JobsUtility.CacheLineSize, alignOf);

            var error = t.Try(ref block);
            return block;
        }
        
        public static unsafe int SizeOf<T>() where T : unmanaged => sizeof (T);
        
        private struct AlignOfHelper<T> where T : struct
        {
#pragma warning disable CS0649
            public byte dummy;
            public T data;
#pragma warning restore CS0649
        }
        
        public static int AlignOf<T>() where T : unmanaged => SizeOf<AlignOfHelper<T>>() - SizeOf<T>();
        
        internal static AllocatorManager.Block AllocateBlock<U>(ref this AllocatorManager.AllocatorHandle t, U u, int items) where U : unmanaged
        {
            return AllocateBlock(ref t, SizeOf<U>(), AlignOf<U>(), items);
        }

        internal static unsafe void* Allocate(ref this AllocatorManager.AllocatorHandle t, int sizeOf, int alignOf, int items)
        {
            return (void*)AllocateBlock(ref t, sizeOf, alignOf, items).Range.Pointer;
        }

        internal static unsafe U* Allocate<U>(ref this AllocatorManager.AllocatorHandle t, U u, int items) where U : unmanaged
        {
            return (U*)Allocate(ref t, SizeOf<U>(), AlignOf<U>(), items);
        }

        internal static unsafe void* AllocateStruct<U>(ref this AllocatorManager.AllocatorHandle t, U u, int items) where U : unmanaged
        {
            return (void*)Allocate(ref t, SizeOf<U>(), AlignOf<U>(), items);
        }
        
        internal static unsafe void FreeBlock(ref this  AllocatorManager.AllocatorHandle t, ref AllocatorManager.Block block)
        {
            block.Range.Items = 0;
            var error = t.Try(ref block);
        }
        
        internal static unsafe void Free(ref this AllocatorManager.AllocatorHandle t, void* pointer, int sizeOf, int alignOf, int items)
        {
            if (pointer == null)
                return;
            AllocatorManager.Block block = default;
            block.AllocatedItems = items;
            block.Range.Pointer = (IntPtr)pointer;
            block.BytesPerItem = sizeOf;
            block.Alignment = alignOf;
            t.FreeBlock(ref block);
        }

        internal static unsafe void Free<U>(ref this AllocatorManager.AllocatorHandle t, U* pointer, int items) where U : unmanaged
        {
            Free(ref t, pointer, SizeOf<U>(), AlignOf<U>(), items);
        }
    }
}