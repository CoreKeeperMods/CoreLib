using System;
using Il2CppInterop.Common;
using Il2CppInterop.Runtime;
using Unity.Burst;
using Unity.Burst.LowLevel;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Hash128 = UnityEngine.Hash128;
using IntPtr = System.IntPtr;
using InvalidOperationException = System.InvalidOperationException;
using Unsafe = System.Runtime.CompilerServices.Unsafe;
using Type = Il2CppSystem.Type;

// Code taken from Unity engine source code, licensed under the Unity Companion License

namespace CoreLib.Submodules.ModComponent
{
    internal static class SharedTypeIndex<TComponent>
    {
        public static readonly ModSharedStatic<int> Ref = ModSharedStatic<int>.GetOrCreate<TypeManager.TypeManagerKeyContext, TComponent>();
    }
    
    /// <summary>
    /// A structure that allows to share mutable static data between C# and HPC#.
    /// </summary>
    /// <typeparam name="T">Type of the data to share (must not contain any reference types)</typeparam>
    public readonly unsafe struct ModSharedStatic<T> where T : struct
    {
        public readonly void* buffer;

        private ModSharedStatic(void* buffer)
        {
            this.buffer = buffer;
        }

        /// <summary>
        /// Get a writable reference to the shared data.
        /// </summary>
        public ref T Data => ref Unsafe.AsRef<T>(buffer);

        /// <summary>
        /// Get a direct unsafe pointer to the shared data.
        /// </summary>
        public void* UnsafeDataPointer => buffer;

        private static readonly IntPtr GetOrCreateSharedMemoryMethodPtr;

        static ModSharedStatic()
        {
            GetOrCreateSharedMemoryMethodPtr = (IntPtr)Il2CppInteropUtils
                .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(BurstCompilerService).GetMethod("GetOrCreateSharedMemory")).GetValue(null);
        }

        internal static unsafe void* GetOrCreateSharedStaticInternal(long getHashCode64, long getSubHashCode64, uint sizeOf, uint alignment)
        {
            if (sizeOf == 0) throw new ArgumentException("sizeOf must be > 0", nameof(sizeOf));
            var hash128 = new Hash128((ulong)getHashCode64, (ulong)getSubHashCode64);
            var result = GetOrCreateSharedMemory(ref hash128, sizeOf, alignment);
            if (result == null)
                throw new InvalidOperationException(
                    "Unable to create a SharedStatic for this key. It is likely that the same key was used to allocate a shared memory with a smaller size while the new size requested is bigger");
            return result;
        }

        internal static unsafe void* GetOrCreateSharedMemory(ref Hash128 key, uint sizeOf, uint alignment)
        {
            IntPtr* ptr = stackalloc IntPtr[3 * sizeof(IntPtr)];
            ptr[0] = (IntPtr)Unsafe.AsPointer(ref key);
            ptr[1] = (IntPtr)Unsafe.AsPointer(ref sizeOf);
            ptr[2] = (IntPtr)Unsafe.AsPointer(ref alignment);
            IntPtr intPtr2 = IntPtr.Zero;

            IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(GetOrCreateSharedMemoryMethodPtr, (IntPtr)0, (void**)ptr, ref intPtr2);
            Il2CppException.RaiseExceptionIfNecessary(intPtr2);

            return (void*)intPtr;
        }


        internal static long GetHashCode64<U>()
        {
            // DOTS Runtime IL2CPP Builds do not use C#'s lazy static initialization order (it uses a C like order, aka random)
            // As such we cannot rely on static init for caching types since any static constructor calling this function
            // may return uninitialized/default-initialized memory
            return BurstRuntime.HashStringWithFNV1A64(Il2CppType.Of<U>().AssemblyQualifiedName);
        }


        /// <summary>
        /// Creates a shared static data for the specified context (usable from both C# and HPC#)
        /// </summary>
        /// <typeparam name="TContext">A type class that uniquely identifies the this shared data.</typeparam>
        /// <param name="alignment">Optional alignment</param>
        /// <returns>A shared static for the specified context</returns>
        public static ModSharedStatic<T> GetOrCreate<TContext>(uint alignment = 0)
        {
            return new ModSharedStatic<T>(GetOrCreateSharedStaticInternal(
                GetHashCode64<TContext>(), 0, (uint)UnsafeUtility.SizeOf<T>(), alignment == 0 ? 4 : alignment));
        }

        /// <summary>
        /// Creates a shared static data for the specified context and sub-context (usable from both C# and HPC#)
        /// </summary>
        /// <typeparam name="TContext">A type class that uniquely identifies the this shared data.</typeparam>
        /// <typeparam name="TSubContext">A type class that uniquely identifies this shared data within a sub-context of the primary context</typeparam>
        /// <param name="alignment">Optional alignment</param>
        /// <returns>A shared static for the specified context</returns>
        public static ModSharedStatic<T> GetOrCreate<TContext, TSubContext>(uint alignment = 0)
        {
            return new ModSharedStatic<T>(GetOrCreateSharedStaticInternal(
                GetHashCode64<TContext>(), GetHashCode64<TSubContext>(),
                (uint)UnsafeUtility.SizeOf<T>(), alignment == 0 ? 4 : alignment));
        }

        /// <summary>
        /// Creates a shared static data for the specified context (reflection based, only usable from C#, but not from HPC#)
        /// </summary>
        /// <param name="contextType">A type class that uniquely identifies the this shared data</param>
        /// <param name="alignment">Optional alignment</param>
        /// <returns>A shared static for the specified context</returns>
        public static ModSharedStatic<T> GetOrCreate(Type contextType, uint alignment = 0)
        {
            return new ModSharedStatic<T>(GetOrCreateSharedStaticInternal(
                BurstRuntime.GetHashCode64(contextType), 0, (uint)UnsafeUtility.SizeOf<T>(), alignment == 0 ? 4 : alignment));
        }

        /// <summary>
        /// Creates a shared static data for the specified context and sub-context (usable from both C# and HPC#)
        /// </summary>
        /// <param name="contextType">A type class that uniquely identifies the this shared data</param>
        /// <param name="subContextType">A type class that uniquely identifies this shared data within a sub-context of the primary context</param>
        /// <param name="alignment">Optional alignment</param>
        /// <returns>A shared static for the specified context</returns>
        public static ModSharedStatic<T> GetOrCreate(Type contextType, Type subContextType, uint alignment = 0)
        {
            return new ModSharedStatic<T>(GetOrCreateSharedStaticInternal(
                BurstRuntime.GetHashCode64(contextType), BurstRuntime.GetHashCode64(subContextType),
                (uint)UnsafeUtility.SizeOf<T>(), alignment == 0 ? 4 : alignment));
        }
    }
}