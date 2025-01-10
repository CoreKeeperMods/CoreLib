using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Jobs;

namespace Unity.Entities
{
    /// <summary>
    /// Pointer to a single bit, and a safety handle.
    /// </summary>
    /// <remarks>Do not store outside of stack</remarks>
    public readonly struct SafeBitRef
    {
        readonly unsafe ulong* m_Ptr;
        readonly int m_OffsetInBits;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        readonly AtomicSafetyHandle m_Safety;
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        /// <summary>
        /// Build a safe pointer to a bit at address ptr + offsetInBits.
        /// </summary>
        /// <param name="ptr">Base memory address to the bit. In bytes.</param>
        /// <param name="offsetInBits">Offset in bits from the base memory address. in bits</param>
        /// <param name="safety">Safety handle for that memory space</param>
        public unsafe SafeBitRef(ulong* ptr, int offsetInBits, AtomicSafetyHandle safety)
        {
            m_Ptr = ptr;
            m_OffsetInBits = offsetInBits;
            m_Safety = safety;
        }
#else
        /// <summary>
        /// Build a safe pointer to a bit at address ptr + offsetInBits.
        /// </summary>
        /// <param name="ptr">Base memory address to the bit. In bytes.</param>
        /// <param name="offsetInBits">Offset in bits from the base memory address. in bits</param>
        public unsafe SafeBitRef(ulong* ptr, int offsetInBits)
        {
            m_Ptr = ptr;
            m_OffsetInBits = offsetInBits;
        }
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS

        /// <summary>
        /// An invalid pointer.
        /// </summary>
        public static unsafe SafeBitRef Null => new SafeBitRef(null, 0, default);

        /// <summary>
        /// Create a new pointer at a bit offset from this bit pointer
        /// </summary>
        /// <param name="offsetInBits">offset in bit for the new pointer to return</param>
        /// <returns>A new SafeBitRef pointing at the offset in bits</returns>
        public unsafe SafeBitRef Offset(int offsetInBits) => new SafeBitRef(m_Ptr, m_OffsetInBits + offsetInBits, m_Safety);
#else
        /// <summary>
        /// An invalid pointer.
        /// </summary>
        public static unsafe SafeBitRef Null => new SafeBitRef(null, 0);

        /// <summary>
        /// Create a new pointer at a bit offset from this bit pointer
        /// </summary>
        /// <param name="offsetInBits">offset in bit for the new pointer to return</param>
        /// <returns>A new SafeBitRef pointing at the offset in bits</returns>
        public unsafe SafeBitRef Offset(int offsetInBits) => new SafeBitRef(m_Ptr, m_OffsetInBits + offsetInBits);
#endif
        /// <summary>
        /// Test if this pointer is valid (not null).
        /// </summary>
        public unsafe bool IsValid => m_Ptr != null;

        /// <summary>
        /// Get the bool value of the bit pointed at.
        /// </summary>
        /// <returns>The bool value of the bit pointed at.</returns>
        public unsafe bool GetBit()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return Bitwise.IsSet(m_Ptr, m_OffsetInBits);
        }

        /// <summary>
        /// Set the bool value of the bit pointed at
        /// </summary>
        /// <param name="value">The value to write to the pointed bit</param>
        public unsafe void SetBit(bool value)
        {

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            UnsafeBitArray.Set(m_Ptr, m_OffsetInBits, value);
        }
    }

    /// <summary>
    /// Safe pointer to a bit buffer first bit, and a
    /// pointer to the chunk's component disabled count, which is updated
    /// when the bits are written to.
    /// </summary>
    /// <remarks>Do not store outside of stack</remarks>
    public struct EnabledMask
    {
        // pointer to the bit
        readonly SafeBitRef m_EnableBitRef;

        // pointer to chunk disabled count
        readonly unsafe int* m_PtrChunkDisabledCount;

        /// <summary>
        /// Interpret the memory at <see cref="SafeBitRef"/> as an <see cref="EnabledMask"/>.
        /// </summary>
        /// <param name="enableBitRef">First bit to the buffer</param>
        /// <param name="ptrChunkDisabledCount">pointer to the disabled count int associated with the chunk the enabled-bit is part of</param>
        public unsafe EnabledMask(SafeBitRef enableBitRef, int* ptrChunkDisabledCount)
        {
            m_EnableBitRef = enableBitRef;
            m_PtrChunkDisabledCount = ptrChunkDisabledCount;
        }

        /// <summary>
        /// First bit of the EnabledMask buffer
        /// </summary>
        public SafeBitRef EnableBit => m_EnableBitRef;

        /// <summary>
        /// Access the bit at bit-memory-address (first bit) + index
        /// </summary>
        /// <param name="index">Index of the bit to access</param>
        /// <returns>true if bit is set</returns>
        /// <exception cref="InvalidOperationException">Thrown if the EnabledMask is missing a pointer to the ChunkDisabledCount</exception>
        public unsafe bool this[int index]
        {
            get => m_EnableBitRef.Offset(index).GetBit();
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (Hint.Unlikely(m_PtrChunkDisabledCount == null))
                    throw new InvalidOperationException(
                    "ComponentEnabledMask requires a non-null ChunkDisabledCount pointer to be able to write to it");
#endif
                bool wasSet = m_EnableBitRef.Offset(index).GetBit();
                m_EnableBitRef.Offset(index).SetBit(value);
                if (Hint.Likely(wasSet != value))
                {
                    var adjustment = math.select(1, -1, value);
                    Interlocked.Add(ref UnsafeUtility.AsRef<int>(m_PtrChunkDisabledCount), adjustment);
                }
            }
        }

        /// <summary>
        /// Retrieve the value of the bit from bit-memory-address (first bit) + index
        /// </summary>
        /// <param name="index">index in the array of bits to retreive the value from</param>
        /// <returns>The value of the bit from bit-memory-address (first bit) + index</returns>
        /// <exception cref="InvalidOperationException">Thrown if the EnabledMask is missing a pointer to the ChunkDisabledCount</exception>
        public bool GetBit(int index) => m_EnableBitRef.Offset(index).GetBit();

        /// <summary>
        /// Get a <see cref="EnabledRefRW{T}"/> reference to the enabled bit at index.
        /// </summary>
        /// <remarks>This method is called by the code generated by the Aspect source generator.</remarks>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="index">The index to the enabled bit in the chunk.</param>
        /// <returns>A reference to the enabled bit for the component at the specified index.</returns>
        public unsafe EnabledRefRW<T> GetEnabledRefRW<T>(int index)
            where T : unmanaged, IComponentData, IEnableableComponent
        {
            return new EnabledRefRW<T>(m_EnableBitRef.Offset(index), m_PtrChunkDisabledCount);
        }

        /// <summary>
        /// Get a <see cref="EnabledRefRO{T}"/> reference to the enabled bit at index.
        /// </summary>
        /// <remarks>This method is called by the code generated by the Aspect source generator.</remarks>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="index">The index to the enabled bit in the chunk.</param>
        /// <returns>A reference to the enabled bit for the component at the specified index.</returns>
        public EnabledRefRO<T> GetEnabledRefRO<T>(int index)
            where T : unmanaged, IComponentData, IEnableableComponent
        {
            return new EnabledRefRO<T>(m_EnableBitRef.Offset(index));
        }

        /// <summary>
        /// Get a <see cref="EnabledRefRW{T}"/> reference to the enabled bit at index.
        /// Returns EnabledRefRORW&lt;T&gt;.Null if this EnabledMask is not valid.
        /// </summary>
        /// <remarks>This method is called by the code generated by the Aspect source generator.</remarks>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="index">The index to the enabled bit in the chunk.</param>
        /// <returns>A reference to the enabled bit for the component at the specified index or <see cref="EnabledRefRW{T}.Null"/> if this <see cref="EnabledMask"/> is not valid.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the EnabledMask is missing a pointer to the ChunkDisabledCount</exception>
        public unsafe EnabledRefRW<T> GetOptionalEnabledRefRW<T>(int index)
            where T : unmanaged, IComponentData, IEnableableComponent
        {
            if (m_EnableBitRef.IsValid)
            {
                return new EnabledRefRW<T>(m_EnableBitRef.Offset(index), m_PtrChunkDisabledCount);
            }
            return EnabledRefRW<T>.Null;
        }

        /// <summary>
        /// Get a <see cref="EnabledRefRO{T}"/> reference to the enabled bit at index.
        /// </summary>
        /// <remarks>This method is called by the code generated by the Aspect source generator.</remarks>
        /// <typeparam name="T">Type of the IEnableableComponent component</typeparam>
        /// <param name="index">The index to the enabled bit in the chunk.</param>
        /// <returns>null if the buffer is not valid.</returns>
        /// <returns>A read-only reference to the enabled bit for the component at the specified index or <see cref="EnabledRefRO{T}.Null"/> if this <see cref="EnabledMask"/> is not valid.</returns>
        public EnabledRefRO<T> GetOptionalEnabledRefRO<T>(int index)
            where T : unmanaged, IComponentData, IEnableableComponent
        {
            if (m_EnableBitRef.IsValid)
                return new EnabledRefRO<T>(m_EnableBitRef.Offset(index));
            return EnabledRefRO<T>.Null;
        }
    }

}
