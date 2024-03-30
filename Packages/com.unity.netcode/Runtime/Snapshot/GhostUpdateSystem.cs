using System;
using System.Diagnostics;
using Unity.Assertions;
using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.NetCode.LowLevel.Unsafe;
using Unity.Mathematics;
using static Unity.Entities.SystemAPI;

namespace Unity.NetCode
{
    struct GhostPredictionGroupTickState : IComponentData
    {
        public NativeParallelHashMap<NetworkTick, NetworkTick> AppliedPredictedTicks;
    }

    /// <summary>
    /// System present only in client worlds, and responsible for:
    /// <para>- updating the state of interpolated ghosts, by copying and intepolating data from the received snapshosts.</para>
    /// <para>- restore the predicted ghost state from the <see cref="GhostPredictionHistoryState"/> before running the next prediction loop (until new snapshot aren't received).</para>
    /// <para>- updating the <see cref="PredictedGhost"/> properties for all predicted ghost, by reflecting the latest received snapshot (see <see cref="PredictedGhost.AppliedTick"/>)
    /// and setting up the correct tick from which the ghost should start predicting (see <see cref="PredictedGhost.PredictionStartTick"/></para>
    /// </summary>
    [UpdateInGroup(typeof(GhostSimulationSystemGroup))]
    [UpdateAfter(typeof(GhostReceiveSystem))]
    [UpdateBefore(typeof(GhostInputSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [BurstCompile]
    public unsafe partial struct GhostUpdateSystem : ISystem
    {
        // There will be burst/IL problems with using generic job structs, so we're
        // laying out each job size type here manually
        [BurstCompile]
        struct UpdateJob : IJobChunk
        {
            public DynamicTypeList DynamicTypeList;
            public Entity GhostCollectionSingleton;
            [ReadOnly] public BufferLookup<GhostComponentSerializer.State> GhostComponentCollectionFromEntity;
            [ReadOnly] public BufferLookup<GhostCollectionPrefabSerializer> GhostTypeCollectionFromEntity;
            [ReadOnly] public BufferLookup<GhostCollectionComponentIndex> GhostComponentIndexFromEntity;
            [NativeDisableContainerSafetyRestriction] private DynamicBuffer<GhostComponentSerializer.State> GhostComponentCollection;
            [NativeDisableContainerSafetyRestriction] private DynamicBuffer<GhostCollectionPrefabSerializer> GhostTypeCollection;
            [NativeDisableContainerSafetyRestriction] private DynamicBuffer<GhostCollectionComponentIndex> GhostComponentIndex;

            [ReadOnly] public NativeParallelHashMap<SpawnedGhost, Entity>.ReadOnly GhostMap;
    #if UNITY_EDITOR || NETCODE_DEBUG
            [NativeDisableParallelForRestriction] public NativeArray<NetworkTick> minMaxSnapshotTick;
    #endif
    #pragma warning disable 649
            [NativeSetThreadIndex] public int ThreadIndex;
    #pragma warning restore 649
            [ReadOnly] public ComponentTypeHandle<GhostInstance> ghostType;
            [ReadOnly] public ComponentTypeHandle<SnapshotData> ghostSnapshotDataType;
            [ReadOnly] public BufferTypeHandle<SnapshotDataBuffer> ghostSnapshotDataBufferType;
            [ReadOnly] public BufferTypeHandle<SnapshotDynamicDataBuffer> ghostSnapshotDynamicDataBufferType;
            [ReadOnly] public ComponentTypeHandle<PreSpawnedGhostIndex> prespawnGhostIndexType;

            public NetworkTick interpolatedTargetTick;
            public float interpolatedTargetTickFraction;
            public NetworkTick predictedTargetTick;
            public float predictedTargetTickFraction;

            public NativeParallelHashMap<NetworkTick, NetworkTick>.ParallelWriter appliedPredictedTicks;
            public ComponentTypeHandle<PredictedGhost> PredictedGhostType;
            public NetworkTick lastPredictedTick;
            public NetworkTick lastInterpolatedTick;

            [ReadOnly] public EntityStorageInfoLookup childEntityLookup;
            [ReadOnly] public BufferTypeHandle<LinkedEntityGroup> linkedEntityGroupType;

            public NetworkTick predictionStateBackupTick;
            public NativeParallelHashMap<ArchetypeChunk, System.IntPtr>.ReadOnly predictionStateBackup;
            [ReadOnly] public EntityTypeHandle entityType;
            public int ghostOwnerId;
            public uint MaxExtrapolationTicks;
            public NetDebug netDebug;

            private void AddPredictionStartTick(NetworkTick targetTick, NetworkTick predictionStartTick)
            {
                // Add a tick a ghost is predicting from, but avoid setting the start tick to something newer (or same tick) as the target tick
                // since the we don't need to predict in that case and having it newer can cause an almost infinate loop (loop until a uint wraps around)
                // Ticks in the buffer which are newer than target tick usually do not happen but it can happen when time goes out of sync and cannot adjust fast enough
                if (targetTick.IsNewerThan(predictionStartTick))
                {
                    // The prediction loop does not run for more ticks than we have inputs for, so clamp the start tick to keep a max hashmap size
                    var startTick = predictionStartTick;
                    if ((uint)targetTick.TicksSince(startTick) > CommandDataUtility.k_CommandDataMaxSize)
                    {
                        startTick = targetTick;
                        startTick.Subtract(CommandDataUtility.k_CommandDataMaxSize);
                    }
                    appliedPredictedTicks.TryAdd(startTick, predictionStartTick);
                }
            }
            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private static void ValidateReadEnableBits(int enableableMaskOffset, int numEnableBits)
            {
                if(enableableMaskOffset > numEnableBits)
                    throw new InvalidOperationException($"Read only {enableableMaskOffset} enable bits data whics are less than the expected {numEnableBits} for this ghost type. This is not a serializarion error but a problem restoring the component state from the decoded snapshot data.");
            }
            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void ValidateAllEnableBitsHasBeenRead(int enableableMaskOffset, int numEnableBits)
            {
                if (enableableMaskOffset != numEnableBits)
                    throw new InvalidOperationException($"Read only {enableableMaskOffset} enable bits but expected to read exacly {numEnableBits} for this ghost type");
            }

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // This job is not written to support queries with enableable component types.
                Assert.IsFalse(useEnabledMask);

                DynamicComponentTypeHandle* ghostChunkComponentTypesPtr = DynamicTypeList.GetData();
                int ghostChunkComponentTypesLength = DynamicTypeList.Length;
                GhostComponentCollection = GhostComponentCollectionFromEntity[GhostCollectionSingleton];
                GhostTypeCollection = GhostTypeCollectionFromEntity[GhostCollectionSingleton];
                GhostComponentIndex = GhostComponentIndexFromEntity[GhostCollectionSingleton];

                bool predicted = chunk.Has(ref PredictedGhostType);
                NetworkTick targetTick = predicted ? predictedTargetTick : interpolatedTargetTick;
                float targetTickFraction = predicted ? predictedTargetTickFraction : interpolatedTargetTickFraction;

                var deserializerState = new GhostDeserializerState
                {
                    GhostMap = GhostMap,
                    GhostOwner = ghostOwnerId,
                    SendToOwner = SendToOwnerType.All
                };
                var ghostComponents = chunk.GetNativeArray(ref ghostType);
                int ghostTypeId = ghostComponents.GetFirstGhostTypeId(out var firstGhost);
                if (ghostTypeId < 0)
                    return;
                if (ghostTypeId >= GhostTypeCollection.Length)
                    return; // serialization data has not been loaded yet. This can only happen for prespawn objects
                var typeData = GhostTypeCollection[ghostTypeId];
                var ghostSnapshotDataArray = chunk.GetNativeArray(ref ghostSnapshotDataType);
                var ghostSnapshotDataBufferArray = chunk.GetBufferAccessor(ref ghostSnapshotDataBufferType);
                var ghostSnapshotDynamicBufferArray = chunk.GetBufferAccessor(ref ghostSnapshotDynamicDataBufferType);

                int changeMaskUints = GhostComponentSerializer.ChangeMaskArraySizeInUInts(typeData.ChangeMaskBits);
                int enableableMaskUints = GhostComponentSerializer.ChangeMaskArraySizeInUInts(typeData.EnableableBits);

                int headerSize = GhostComponentSerializer.SnapshotSizeAligned(sizeof(uint) + changeMaskUints*sizeof(uint) + enableableMaskUints*sizeof(uint));
                int snapshotDataOffset = headerSize;

                int snapshotDataAtTickSize = UnsafeUtility.SizeOf<SnapshotData.DataAtTick>();
#if UNITY_EDITOR || NETCODE_DEBUG
                var minMaxOffset = ThreadIndex * (JobsUtility.CacheLineSize/4);
#endif
                var dataAtTick = new NativeArray<SnapshotData.DataAtTick>(ghostComponents.Length, Allocator.Temp);
                var entityRange = new NativeList<int2>(ghostComponents.Length, Allocator.Temp);
                int2 nextRange = default;
                var PredictedGhostArray = chunk.GetNativeArray(ref PredictedGhostType);
                bool canBeStatic = typeData.StaticOptimization;
                bool isPrespawn = chunk.Has(ref prespawnGhostIndexType);
                // Find the ranges of entities which have data to apply, store the data to apply in an array while doing so
                for (int ent = firstGhost; ent < ghostComponents.Length; ++ent)
                {
                    // Pre spawned ghosts might not have the ghost type set yet - in that case we need to skip them until the GHostReceiveSystem has assigned the ghost type
                    if (isPrespawn && ghostComponents[ent].ghostType != ghostTypeId)
                    {
                        if (nextRange.y != 0)
                            entityRange.Add(nextRange);
                        nextRange = default;
                        continue;
                    }
#if UNITY_EDITOR || NETCODE_DEBUG
                    // Validate that the ghost entity has been spawned by the client as predicted spawn or because a ghost as been
                    // received. In any case, validate that the ghost component contains pertinent data.
                    if((ghostComponents[ent].ghostId == 0) && (isPrespawn || !ghostComponents[ent].spawnTick.IsValid))
                    {
                        var invalidEntity = chunk.GetNativeArray(entityType)[ent];
                        if (isPrespawn)
                            netDebug.LogError($"Entity {invalidEntity} is not a valid prespawned ghost (ghostId == 0).");
                        else
                            netDebug.LogError($"Entity {invalidEntity} is not a valid ghost (i.e. it is not a real 'replicated ghost', nor is it a 'predicted spawn' ghost). This can happen if you instantiate a ghost entity on the client manually (without marking it as a predicted spawn).");
                        //skip the entity
                        if (nextRange.y != 0)
                            entityRange.Add(nextRange);
                        nextRange = default;
                        continue;
                    }
#endif
                    var snapshotDataBuffer = ghostSnapshotDataBufferArray[ent];
                    var ghostSnapshotData = ghostSnapshotDataArray[ent];
                    var latestTick = ghostSnapshotData.GetLatestTick(snapshotDataBuffer);
                    bool isStatic = canBeStatic && ghostSnapshotData.WasLatestTickZeroChange(snapshotDataBuffer, changeMaskUints);
#if UNITY_EDITOR || NETCODE_DEBUG
                    if (latestTick.IsValid && !isStatic)
                    {
                        if (!minMaxSnapshotTick[minMaxOffset].IsValid || minMaxSnapshotTick[minMaxOffset].IsNewerThan(latestTick))
                            minMaxSnapshotTick[minMaxOffset] = latestTick;
                        if (!minMaxSnapshotTick[minMaxOffset + 1].IsValid || latestTick.IsNewerThan(minMaxSnapshotTick[minMaxOffset + 1]))
                            minMaxSnapshotTick[minMaxOffset + 1] = latestTick;
                    }
#endif

                    bool hasSnapshot = ghostSnapshotData.GetDataAtTick(targetTick, typeData.PredictionOwnerOffset, targetTickFraction, snapshotDataBuffer, out var data, MaxExtrapolationTicks);
                    if (!hasSnapshot)
                    {
                        // If there is no snapshot before our target tick, try to get the oldest tick we do have and use that
                        // This deals better with ticks moving backwards and clamps ghosts at the oldest state we do have data for
                        var oldestSnapshot = ghostSnapshotData.GetOldestTick(snapshotDataBuffer);
                        hasSnapshot = (oldestSnapshot.IsValid && ghostSnapshotData.GetDataAtTick(oldestSnapshot, typeData.PredictionOwnerOffset, 1, snapshotDataBuffer, out data, MaxExtrapolationTicks));
                    }

                    if (hasSnapshot)
                    {
                        if (predicted)
                        {
                            // We might get an interpolation between the tick before and after our target - we have to apply the tick right before our target so we set interpolation to 0
                            data.InterpolationFactor = 0;
                            var snapshotTick = new NetworkTick{SerializedData = *(uint*)data.SnapshotBefore};
                            var predictedData = PredictedGhostArray[ent];
                            // We want to contiue prediction from the last full tick we predicted last time
                            var predictionStartTick = predictionStateBackupTick;
                            // If there is no history, try to use the tick where we left off last time, will only be a valid tick if we ended with a full prediction tick as opposed to a fractional one
                            if (!predictionStartTick.IsValid)
                                predictionStartTick = lastPredictedTick;
                            // If we do not have a backup or we got more data since last time we run from the tick we have snapshot data for
                            if (!predictionStartTick.IsValid || predictedData.AppliedTick != snapshotTick)
                                predictionStartTick = snapshotTick;
                            // If we have newer or equally new data in the
                            else if (!predictionStartTick.IsNewerThan(snapshotTick))
                                predictionStartTick = snapshotTick;

                            // If we want to continue prediction, and this is not the currently applied prediction state we must restore the state from the backup
                            if (predictionStartTick != snapshotTick && predictionStartTick != lastPredictedTick)
                            {
                                // If we cannot restore the backup and continue prediction, we roll back and resimulate
                                if (!RestorePredictionBackup(chunk, ent, typeData, ghostChunkComponentTypesPtr, ghostChunkComponentTypesLength))
                                    predictionStartTick = snapshotTick;
                            }

                            AddPredictionStartTick(targetTick, predictionStartTick);

                            if (predictionStartTick != snapshotTick)
                            {
                                if (nextRange.y != 0)
                                    entityRange.Add(nextRange);
                                nextRange = default;
                            }
                            else
                            {
                                predictedData.AppliedTick = snapshotTick;
                                if (nextRange.y == 0)
                                    nextRange.x = ent;
                                nextRange.y = ent+1;
                            }
                            predictedData.PredictionStartTick = predictionStartTick;
                            PredictedGhostArray[ent] = predictedData;
                        }
                        else
                        {
                            // If this snapshot is static, and the data for the latest tick was applied during last interpolation update, we can just skip copying data
                            if (isStatic && latestTick.IsValid && lastInterpolatedTick.IsValid && !latestTick.IsNewerThan(lastInterpolatedTick))
                            {
                                if (nextRange.y != 0)
                                    entityRange.Add(nextRange);
                                nextRange = default;
                            }
                            else
                            {
                                if (nextRange.y == 0)
                                    nextRange.x = ent;
                                nextRange.y = ent+1;
                            }
                        }
                        dataAtTick[ent] = data;
                    }
                    else
                    {
                        if (nextRange.y != 0)
                        {
                            entityRange.Add(nextRange);
                            nextRange = default;
                        }
                        if (predicted)
                        {
                            //predicted - pre-spawned ghost may not have a valid snapshot until we receive the first snapshot from the server.
                            //This is also happening for static optimized - prespawned ghosts until they change
                            if(!isPrespawn)
                                netDebug.LogWarning($"Trying to predict a ghost without having a state to roll back to {ghostSnapshotData.GetOldestTick(snapshotDataBuffer)} / {targetTick}");
                            // This is a predicted snapshot which does not have any state at all to roll back to, just let it continue from it's last state if possible
                            var predictionStartTick = lastPredictedTick;
                            // Try to restore from backup if last tick was a partial tick
                            if (!predictionStartTick.IsValid && predictionStateBackupTick.IsValid &&
                                RestorePredictionBackup(chunk, ent, typeData, ghostChunkComponentTypesPtr, ghostChunkComponentTypesLength))
                                predictionStartTick = predictionStateBackupTick;
                            if (!predictionStartTick.IsValid)
                            {
                                // There was no last state to continue from, so do not run prediction at all
                                predictionStartTick = targetTick;
                            }
                            AddPredictionStartTick(targetTick, predictionStartTick);
                            var predictedData = PredictedGhostArray[ent];
                            predictedData.PredictionStartTick = predictionStartTick;
                            PredictedGhostArray[ent] = predictedData;
                        }
                    }
                }
                if (nextRange.y != 0)
                    entityRange.Add(nextRange);

                var requiredSendMask = predicted ? GhostComponentSerializer.SendMask.Predicted : GhostComponentSerializer.SendMask.Interpolated;
                int numBaseComponents = typeData.NumComponents - typeData.NumChildComponents;

                // This buffer allowing us to MemCmp changes, which allows us to support change filtering.
                var tempChangeBufferSize = 1_500;
                byte* tempChangeBuffer = stackalloc byte[tempChangeBufferSize];
                NativeArray<byte> tempChangeBufferLarge = default;

                var enableableMaskOffset = 0;
                for (int comp = 0; comp < numBaseComponents; ++comp)
                {
                    int compIdx = GhostComponentIndex[typeData.FirstComponent + comp].ComponentIndex;
                    int serializerIdx = GhostComponentIndex[typeData.FirstComponent + comp].SerializerIndex;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (compIdx >= ghostChunkComponentTypesLength)
                        throw new System.InvalidOperationException("Component index out of range");
#endif
                    var snapshotSize = GhostComponentCollection[serializerIdx].ComponentType.IsBuffer
                        ? GhostComponentSerializer.SnapshotSizeAligned(GhostSystemConstants.DynamicBufferComponentSnapshotSize)
                        : GhostComponentSerializer.SnapshotSizeAligned(GhostComponentCollection[serializerIdx].SnapshotSize);
                    if (!chunk.Has(ref ghostChunkComponentTypesPtr[compIdx]) || (GhostComponentIndex[typeData.FirstComponent + comp].SendMask&requiredSendMask) == 0)
                    {
                        snapshotDataOffset += snapshotSize;
                        continue;
                    }

                    var componentHasChanges = false;
                    var compSize = GhostComponentCollection[serializerIdx].ComponentSize;
                    if (!GhostComponentCollection[serializerIdx].ComponentType.IsBuffer)
                    {
                        deserializerState.SendToOwner = GhostComponentCollection[serializerIdx].SendToOwner;
                        if (GhostComponentCollection[serializerIdx].HasGhostFields)
                        {
                            var roDynamicComponentTypeHandle = ghostChunkComponentTypesPtr[compIdx].CopyToReadOnly();
                            var roCompArray = chunk.GetDynamicComponentDataArrayReinterpret<byte>(ref roDynamicComponentTypeHandle, compSize);

                            for (var rangeIdx = 0; rangeIdx < entityRange.Length; ++rangeIdx)
                            {
                                var range = entityRange[rangeIdx];

                                var snapshotData = (byte*) dataAtTick.GetUnsafeReadOnlyPtr();
                                snapshotData += snapshotDataAtTickSize * range.x;

                                // Fast path: If we already have changes, just fetch the RW version and write directly.
                                if (componentHasChanges)
                                {
                                    var rwCompArray = chunk.GetDynamicComponentDataArrayReinterpret<byte>(ref ghostChunkComponentTypesPtr[compIdx], compSize);
                                    var rwCompData = (byte*) rwCompArray.GetUnsafePtr();
                                    rwCompData += range.x * compSize;
                                    GhostComponentCollection[serializerIdx].CopyFromSnapshot.Ptr.Invoke((System.IntPtr) UnsafeUtility.AddressOf(ref deserializerState), (System.IntPtr) snapshotData, snapshotDataOffset, snapshotDataAtTickSize, (System.IntPtr) rwCompData, compSize, range.y - range.x);
                                    continue;
                                }

                                // 1. Get Readonly version from chunk.
                                var roCompData = (byte*) roCompArray.GetUnsafeReadOnlyPtr();
                                roCompData += range.x * compSize;

                                // 2. Copy it into a temp buffer large enough to hold values (inside the range loop).
                                var requiredNumBytes = (range.y - range.x) * compSize;
                                CopyRODataIntoTempChangeBuffer(requiredNumBytes, ref tempChangeBuffer, ref tempChangeBufferSize, ref tempChangeBufferLarge, roCompData);

                                // 3. Invoke CopyFromSnapshot with the ro buffer as destination (yes, hacky!).
                                GhostComponentCollection[serializerIdx].CopyFromSnapshot.Ptr.Invoke((System.IntPtr) UnsafeUtility.AddressOf(ref deserializerState), (System.IntPtr) snapshotData, snapshotDataOffset, snapshotDataAtTickSize, (System.IntPtr) roCompData, compSize, range.y - range.x);

                                // 4. Compare the two buffers (for changes).
                                k_ChangeFiltering.Begin();
                                if (UnsafeUtility.MemCmp(roCompData, tempChangeBuffer, requiredNumBytes) != 0)
                                {
                                    // 5. Fetch as RW to bump change version. We've already written to it, so nothing to do.
                                    componentHasChanges = true;
                                    chunk.GetDynamicComponentDataArrayReinterpret<byte>(ref ghostChunkComponentTypesPtr[compIdx], compSize);
                                }
                                k_ChangeFiltering.End();
                            }
                            snapshotDataOffset += snapshotSize;
                        }

                        if (typeData.EnableableBits > 0 && GhostComponentCollection[serializerIdx].SerializesEnabledBit != 0)
                        {
                            for (var rangeIdx = 0; rangeIdx < entityRange.Length; ++rangeIdx)
                            {
                                var range = entityRange[rangeIdx];
                                var snapshotData = (byte*) dataAtTick.GetUnsafeReadOnlyPtr();
                                snapshotData += snapshotDataAtTickSize * range.x;
                                var dataAtTickPtr = (SnapshotData.DataAtTick*) snapshotData;

                                UpdateEnableableMask(chunk, dataAtTickPtr, changeMaskUints, enableableMaskOffset, range, ghostChunkComponentTypesPtr, compIdx, ref componentHasChanges);
                            }
                            ++enableableMaskOffset;
                            ValidateReadEnableBits(enableableMaskOffset, typeData.EnableableBits);
                        }
                    }
                    else
                    {
                        var roDynamicComponentTypeHandle = ghostChunkComponentTypesPtr[compIdx].CopyToReadOnly();
                        var roBufferAccessor = chunk.GetUntypedBufferAccessor(ref roDynamicComponentTypeHandle);
                        UnsafeUntypedBufferAccessor rwBufferAccessor = default;
                        var dynamicDataSize = GhostComponentCollection[serializerIdx].SnapshotSize;
                        var maskBits = GhostComponentCollection[serializerIdx].ChangeMaskBits;
                        deserializerState.SendToOwner = GhostComponentCollection[serializerIdx].SendToOwner;
                        for (var rangeIdx = 0; rangeIdx < entityRange.Length; ++rangeIdx)
                        {
                            var range = entityRange[rangeIdx];
                            for (int ent = range.x; ent < range.y; ++ent)
                            {
                                //Compute the required owner mask for the buffers and skip the copyfromsnapshot. The check must be done
                                //for each entity.
                                if (dataAtTick[ent].GhostOwner > 0)
                                {
                                    var requiredOwnerMask = dataAtTick[ent].GhostOwner == deserializerState.GhostOwner
                                        ? SendToOwnerType.SendToOwner
                                        : SendToOwnerType.SendToNonOwner;
                                    if ((GhostComponentCollection[serializerIdx].SendToOwner & requiredOwnerMask) == 0)
                                        continue;
                                }

                                var dynamicDataBuffer = ghostSnapshotDynamicBufferArray[ent];
                                var dynamicDataAtTick = SetupDynamicDataAtTick(dataAtTick[ent], snapshotDataOffset, dynamicDataSize, maskBits, dynamicDataBuffer, out var bufLen);
                                var prevBufLen = roBufferAccessor.GetBufferLength(ent);

                                componentHasChanges |= prevBufLen != bufLen;
                                if (componentHasChanges)
                                {
                                    // FIXME: Only fetch this buffer if it's default.
                                    rwBufferAccessor = chunk.GetUntypedBufferAccessor(ref ghostChunkComponentTypesPtr[compIdx]);
                                    rwBufferAccessor.ResizeUninitialized(ent, bufLen);
                                    var rwBufData = rwBufferAccessor.GetUnsafePtr(ent);
                                    GhostComponentCollection[serializerIdx].CopyFromSnapshot.Ptr.Invoke(
                                        (System.IntPtr)UnsafeUtility.AddressOf(ref deserializerState),
                                        (System.IntPtr) UnsafeUtility.AddressOf(ref dynamicDataAtTick), 0, dynamicDataSize,
                                        (IntPtr)rwBufData, compSize, bufLen);
                                    continue;
                                }

                                var requiredNumBytes = bufLen * compSize;
                                var roBufData = (byte*) roBufferAccessor.GetUnsafeReadOnlyPtr(ent);
                                CopyRODataIntoTempChangeBuffer(requiredNumBytes, ref tempChangeBuffer, ref tempChangeBufferSize, ref tempChangeBufferLarge, roBufData);

                                // Again, hack to pass in the roBufData to be written into.
                                // NOTE: We know that these two buffers will be the EXACT same size, due to the above assurances.
                                GhostComponentCollection[serializerIdx].CopyFromSnapshot.Ptr.Invoke(
                                    (System.IntPtr)UnsafeUtility.AddressOf(ref deserializerState),
                                    (System.IntPtr) UnsafeUtility.AddressOf(ref dynamicDataAtTick), 0, dynamicDataSize,
                                    (IntPtr)roBufData, compSize, bufLen);

                                k_ChangeFiltering.Begin();
                                if (UnsafeUtility.MemCmp(roBufData, tempChangeBuffer, requiredNumBytes) != 0)
                                {
                                    componentHasChanges = true;
                                    // Bump change version.
                                    chunk.GetUntypedBufferAccessor(ref ghostChunkComponentTypesPtr[compIdx]);
                                }
                                k_ChangeFiltering.End();
                            }

                            var snapshotData = (byte*)dataAtTick.GetUnsafeReadOnlyPtr();
                            snapshotData += snapshotDataAtTickSize * range.x;
                            var dataAtTickPtr = (SnapshotData.DataAtTick*) snapshotData;
                            if (typeData.EnableableBits > 0 && GhostComponentCollection[serializerIdx].SerializesEnabledBit != 0)
                                UpdateEnableableMask(chunk, dataAtTickPtr, changeMaskUints, enableableMaskOffset, range, ghostChunkComponentTypesPtr, compIdx, ref componentHasChanges);
                        }

                        if (typeData.EnableableBits > 0 && GhostComponentCollection[serializerIdx].SerializesEnabledBit != 0)
                        {
                            ++enableableMaskOffset;
                            ValidateReadEnableBits(enableableMaskOffset, typeData.EnableableBits);
                        }
                        snapshotDataOffset += snapshotSize;

                    }
                }
                if (typeData.NumChildComponents > 0)
                {
                    var linkedEntityGroupAccessor = chunk.GetBufferAccessor(ref linkedEntityGroupType);
                    for (int comp = numBaseComponents; comp < typeData.NumComponents; ++comp)
                    {
                        int compIdx = GhostComponentIndex[typeData.FirstComponent + comp].ComponentIndex;
                        int serializerIdx = GhostComponentIndex[typeData.FirstComponent + comp].SerializerIndex;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                        if (compIdx >= ghostChunkComponentTypesLength)
                            throw new System.InvalidOperationException("Component index out of range");
#endif

                        var snapshotSize = GhostComponentCollection[serializerIdx].ComponentType.IsBuffer
                            ? GhostComponentSerializer.SnapshotSizeAligned(GhostSystemConstants.DynamicBufferComponentSnapshotSize)
                            : GhostComponentSerializer.SnapshotSizeAligned(GhostComponentCollection[serializerIdx].SnapshotSize);
                        if ((GhostComponentIndex[typeData.FirstComponent + comp].SendMask & requiredSendMask) == 0)
                        {
                            snapshotDataOffset += snapshotSize;
                            continue;
                        }

                        var compSize = GhostComponentCollection[serializerIdx].ComponentSize;
                        if (!GhostComponentCollection[serializerIdx].ComponentType.IsBuffer)
                        {
                            deserializerState.SendToOwner = GhostComponentCollection[serializerIdx].SendToOwner;
                            for (var rangeIdx = 0; rangeIdx < entityRange.Length; ++rangeIdx)
                            {
                                var range = entityRange[rangeIdx];
                                for (int ent = range.x; ent < range.y; ++ent)
                                {
                                    var linkedEntityGroup = linkedEntityGroupAccessor[ent];
                                    var childEntity = linkedEntityGroup[GhostComponentIndex[typeData.FirstComponent + comp].EntityIndex].Value;
                                    if (!childEntityLookup.Exists(childEntity))
                                        continue;
                                    var childChunk = childEntityLookup[childEntity];
                                    if (!childChunk.Chunk.Has(ref ghostChunkComponentTypesPtr[compIdx]))
                                        continue;

                                    // We fetch these via `GetUnsafeReadOnlyPtr` only for performance reasons. It's safe.
                                    var snapshotData = (byte*)dataAtTick.GetUnsafeReadOnlyPtr();
                                    snapshotData += snapshotDataAtTickSize * ent;
                                    if (GhostComponentCollection[serializerIdx].HasGhostFields)
                                    {
                                        // No fast-path here!
                                        // 1. Get Readonly version from chunk.
                                        var roDynamicComponentTypeHandle = ghostChunkComponentTypesPtr[compIdx].CopyToReadOnly();
                                        var roCompArray = childChunk.Chunk.GetDynamicComponentDataArrayReinterpret<byte>(ref roDynamicComponentTypeHandle, compSize);
                                        var roCompData = (byte*) roCompArray.GetUnsafeReadOnlyPtr();
                                        roCompData += childChunk.IndexInChunk * compSize;

                                        // 2. Copy it into a temp buffer large enough to hold values (inside the range loop).
                                        var requiredNumBytes = compSize;
                                        CopyRODataIntoTempChangeBuffer(requiredNumBytes, ref tempChangeBuffer, ref tempChangeBufferSize, ref tempChangeBufferLarge, roCompData);

                                        // 3. Invoke CopyFromSnapshot with the ro buffer as destination (yes, hacky!).
                                        GhostComponentCollection[serializerIdx].CopyFromSnapshot.Ptr.Invoke((System.IntPtr) UnsafeUtility.AddressOf(ref deserializerState), (System.IntPtr) snapshotData, snapshotDataOffset, snapshotDataAtTickSize, (System.IntPtr) roCompData, compSize, 1);

                                        // 4. MemCmp the two buffers.
                                        k_ChangeFiltering.Begin();
                                        if (UnsafeUtility.MemCmp(tempChangeBuffer, roCompData, compSize) != 0)
                                        {
                                            // 5. Get RW, if changes, MemCpy
                                            childChunk.Chunk.GetDynamicComponentDataArrayReinterpret<byte>(ref ghostChunkComponentTypesPtr[compIdx], compSize);
                                        }
                                        k_ChangeFiltering.End();
                                    }

                                    var dataAtTickPtr = (SnapshotData.DataAtTick*) snapshotData;
                                    if (typeData.EnableableBits > 0 && GhostComponentCollection[serializerIdx].SerializesEnabledBit != 0)
                                    {
                                        var childRange = new int2 { x = childChunk.IndexInChunk, y = childChunk.IndexInChunk + 1 };
                                        var unused = false;
                                        UpdateEnableableMask(childChunk.Chunk, dataAtTickPtr, changeMaskUints, enableableMaskOffset, childRange, ghostChunkComponentTypesPtr, compIdx, ref unused);
                                    }
                                }
                            }
                            if (typeData.EnableableBits > 0 && GhostComponentCollection[serializerIdx].SerializesEnabledBit != 0)
                            {
                                ++enableableMaskOffset;
                                ValidateReadEnableBits(enableableMaskOffset, typeData.EnableableBits);
                            }
                            snapshotDataOffset += snapshotSize;
                        }
                        else
                        {
                            var dynamicDataSize = GhostComponentCollection[serializerIdx].SnapshotSize;
                            var maskBits = GhostComponentCollection[serializerIdx].ChangeMaskBits;
                            deserializerState.SendToOwner = GhostComponentCollection[serializerIdx].SendToOwner;
                            for (var rangeIdx = 0; rangeIdx < entityRange.Length; ++rangeIdx)
                            {
                                var range = entityRange[rangeIdx];
                                var maskOffset = enableableMaskOffset;
                                for (int ent = range.x; ent < range.y; ++ent)
                                {
                                    var linkedEntityGroup = linkedEntityGroupAccessor[ent];
                                    var childEntity = linkedEntityGroup[GhostComponentIndex[typeData.FirstComponent + comp].EntityIndex].Value;
                                    if (!childEntityLookup.Exists(childEntity))
                                        continue;
                                    var childChunk = childEntityLookup[childEntity];
                                    if (!childChunk.Chunk.Has(ref ghostChunkComponentTypesPtr[compIdx]))
                                        continue;

                                    //Compute the required owner mask for the buffers and skip the copyfromsnapshot. The check must be done
                                    if (dataAtTick[ent].GhostOwner > 0)
                                    {
                                        var requiredOwnerMask = dataAtTick[ent].GhostOwner == deserializerState.GhostOwner
                                            ? SendToOwnerType.SendToOwner
                                            : SendToOwnerType.SendToNonOwner;
                                        if ((deserializerState.SendToOwner & requiredOwnerMask) == 0)
                                            continue;
                                    }

                                    var roDynamicComponentTypeHandle = ghostChunkComponentTypesPtr[compIdx].CopyToReadOnly();
                                    var roBufferAccessor = childChunk.Chunk.GetUntypedBufferAccessor(ref roDynamicComponentTypeHandle);

                                    var dynamicDataBuffer = ghostSnapshotDynamicBufferArray[ent];
                                    var dynamicDataAtTick = SetupDynamicDataAtTick(dataAtTick[ent], snapshotDataOffset, dynamicDataSize, maskBits, dynamicDataBuffer, out var bufLen);
                                    var prevBufLen = roBufferAccessor.GetBufferLength(ent);

                                    if (prevBufLen != bufLen)
                                    {
                                        var rwBufferAccessor = childChunk.Chunk.GetUntypedBufferAccessor(ref ghostChunkComponentTypesPtr[compIdx]);
                                        rwBufferAccessor.ResizeUninitialized(ent, bufLen);
                                        var rwBufData = rwBufferAccessor.GetUnsafePtr(ent);

                                        GhostComponentCollection[serializerIdx].CopyFromSnapshot.Ptr.Invoke(
                                            (System.IntPtr) UnsafeUtility.AddressOf(ref deserializerState),
                                            (System.IntPtr) UnsafeUtility.AddressOf(ref dynamicDataAtTick), 0, dynamicDataSize,
                                            (IntPtr) rwBufData, compSize, bufLen);
                                    }
                                    else
                                    {
                                        var roBufData = (byte*) roBufferAccessor.GetUnsafeReadOnlyPtr(ent);
                                        var requiredNumBytes = bufLen * compSize;
                                        CopyRODataIntoTempChangeBuffer(requiredNumBytes, ref tempChangeBuffer, ref tempChangeBufferSize, ref tempChangeBufferLarge, roBufData);

                                        // Again, hack to pass in the roBufData to be written into.
                                        // NOTE: We know that these two buffers will be the EXACT same size, due to the above assurances.
                                        GhostComponentCollection[serializerIdx].CopyFromSnapshot.Ptr.Invoke(
                                            (System.IntPtr) UnsafeUtility.AddressOf(ref deserializerState),
                                            (System.IntPtr) UnsafeUtility.AddressOf(ref dynamicDataAtTick), 0, dynamicDataSize,
                                            (IntPtr) roBufData, compSize, bufLen);

                                        k_ChangeFiltering.Begin();

                                        if (UnsafeUtility.MemCmp(roBufData, tempChangeBuffer, requiredNumBytes) != 0)
                                        {
                                            // Bump change version.
                                            childChunk.Chunk.GetUntypedBufferAccessor(ref ghostChunkComponentTypesPtr[compIdx]);
                                        }
                                        k_ChangeFiltering.End();
                                    }

                                    if (typeData.EnableableBits > 0 && GhostComponentCollection[serializerIdx].SerializesEnabledBit != 0)
                                    {
                                        var snapshotData = (byte*) dataAtTick.GetUnsafeReadOnlyPtr();
                                        snapshotData += snapshotDataAtTickSize * ent;
                                        var dataAtTickPtr = (SnapshotData.DataAtTick*) snapshotData;

                                        var childRange = new int2 {x = childChunk.IndexInChunk, y = childChunk.IndexInChunk + 1};
                                        var unused = false;
                                        UpdateEnableableMask(childChunk.Chunk, dataAtTickPtr, changeMaskUints, maskOffset, childRange, ghostChunkComponentTypesPtr, compIdx, ref unused);
                                    }
                                }
                            }
                            if (typeData.EnableableBits > 0 && GhostComponentCollection[serializerIdx].SerializesEnabledBit != 0)
                            {
                                ++enableableMaskOffset;
                                ValidateReadEnableBits(enableableMaskOffset, typeData.EnableableBits);
                            }
                            snapshotDataOffset += snapshotSize;
                        }
                    }
                }
                ValidateAllEnableBitsHasBeenRead(enableableMaskOffset, typeData.EnableableBits);
            }

            private static void CopyRODataIntoTempChangeBuffer(int requiredCompDataLength, ref byte* tempChangeBuffer, ref int tempChangeBufferSize, ref NativeArray<byte> tempChangeBufferLarge, byte* roCompData)
            {
                k_ChangeFiltering.Begin();
                if (requiredCompDataLength > tempChangeBufferSize)
                {
                    tempChangeBufferLarge = new NativeArray<byte>(math.ceilpow2(requiredCompDataLength), Allocator.Temp);
                    tempChangeBuffer = (byte*) tempChangeBufferLarge.GetUnsafePtr();
                    tempChangeBufferSize = tempChangeBufferLarge.Length;
                }
                UnsafeUtility.MemCpy(tempChangeBuffer, roCompData, requiredCompDataLength);
                k_ChangeFiltering.End();
            }

            // TODO - We can perform this logic faster using the EnabledMask.
            private static void UpdateEnableableMask(ArchetypeChunk chunk, SnapshotData.DataAtTick* dataAtTickPtr,
                int changeMaskUints, int enableableMaskOffset, int2 range,
                DynamicComponentTypeHandle* ghostChunkComponentTypesPtr, int compIdx, ref bool componentHasChanges)
            {
                var uintOffset = enableableMaskOffset >> 5;
                var maskOffset = enableableMaskOffset & 0x1f;

                for (int i = range.x; i < range.y; ++i)
                {
                    var snapshotDataPtr = (byte*)dataAtTickPtr->SnapshotBefore;
                    uint* enableableMasks = (uint*)(snapshotDataPtr + sizeof(uint) + changeMaskUints * sizeof(uint));
                    enableableMasks += uintOffset;

                    var isSet = ((*enableableMasks) & (1U << maskOffset)) != 0;

                    k_ChangeFiltering.Begin();
                    if (isSet != chunk.IsComponentEnabled(ref ghostChunkComponentTypesPtr[compIdx], i))
                    {
                        componentHasChanges = true;
                        k_ChangeFiltering.End();
                        chunk.SetComponentEnabled(ref ghostChunkComponentTypesPtr[compIdx], i, isSet);
                    }
                    else k_ChangeFiltering.End();

                    dataAtTickPtr++;
                }
            }

            SnapshotData.DataAtTick SetupDynamicDataAtTick(in SnapshotData.DataAtTick dataAtTick,
                int snapshotOffset, int snapshotSize, int maskBits, in DynamicBuffer<SnapshotDynamicDataBuffer> ghostSnapshotDynamicBuffer, out int buffernLen)
            {
                // Retrieve from the snapshot the buffer information and
                var snapshotData = (int*)(dataAtTick.SnapshotBefore + snapshotOffset);
                var bufLen = snapshotData[0];
                var dynamicDataOffset = snapshotData[1];
                //The dynamic snapshot data is associated with the root entity not the children
                var dynamicSnapshotDataBeforePtr = SnapshotDynamicBuffersHelper.GetDynamicDataPtr((byte*)ghostSnapshotDynamicBuffer.GetUnsafeReadOnlyPtr(),
                    dataAtTick.BeforeIdx, ghostSnapshotDynamicBuffer.Length);
                //var dynamicSnapshotDataCapacity = SnapshotDynamicBuffersHelper.GetDynamicDataCapacity(SnapshotDynamicBuffersHelper.GetHeaderSize(),ghostSnapshotDynamicBuffer.Length);
                var dynamicMaskSize = SnapshotDynamicBuffersHelper.GetDynamicDataChangeMaskSize(maskBits, bufLen);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if ((dynamicDataOffset + bufLen*snapshotSize) > ghostSnapshotDynamicBuffer.Length)
                    throw new System.InvalidOperationException("Overflow reading data from dynamic snapshot memory buffer");
#endif
                //Copy into the buffer the snapshot data. Use a temporary DataTick to pass some information to the serializer function.
                //No need to use a DataAtTick per element (would be overkill)
                buffernLen = bufLen;
                return new SnapshotData.DataAtTick
                {
                    SnapshotBefore = (System.IntPtr)(dynamicSnapshotDataBeforePtr + dynamicDataOffset + dynamicMaskSize),
                    SnapshotAfter = (System.IntPtr)(dynamicSnapshotDataBeforePtr + dynamicDataOffset + dynamicMaskSize),
                    //No interpolation factor is necessary
                    InterpolationFactor = 0.0f,
                    Tick = dataAtTick.Tick
                };
            }
            // TODO - Determine if we need change filtering on RestoreFromBackup.
            bool RestorePredictionBackup(ArchetypeChunk chunk, int ent, in GhostCollectionPrefabSerializer typeData, DynamicComponentTypeHandle* ghostChunkComponentTypesPtr, int ghostChunkComponentTypesLength)
            {
                // Try to get the backup state
                if (!predictionStateBackup.TryGetValue(chunk, out var state) ||
                    (*(PredictionBackupState*)state).entityCapacity != chunk.Capacity)
                    return false;

                // Verify that the backup is for the correct entity
                Entity* entities = PredictionBackupState.GetEntities(state);
                var entity = chunk.GetNativeArray(entityType)[ent];
                if (entity != entities[ent])
                    return false;

                int baseOffset = typeData.FirstComponent;
                const GhostComponentSerializer.SendMask requiredSendMask = GhostComponentSerializer.SendMask.Predicted;

                byte* dataPtr = PredictionBackupState.GetData(state);
                ulong* enabledBitPtr = PredictionBackupState.GetEnabledBits(state);
                //bufferBackupDataPtr is null in case there are no buffer for that ghost type
                byte* bufferBackupDataPtr = PredictionBackupState.GetBufferDataPtr(state);
                int numBaseComponents = typeData.NumComponents - typeData.NumChildComponents;
                var ghostOwner = PredictionBackupState.GetGhostOwner(state);
                var requiredOwnerMask = SendToOwnerType.All;
                if (ghostOwnerId != 0 && ghostOwner != 0)
                {
                    requiredOwnerMask = ghostOwnerId == ghostOwner
                        ? SendToOwnerType.SendToOwner
                        : SendToOwnerType.SendToNonOwner;
                }
                for (int comp = 0; comp < numBaseComponents; ++comp)
                {
                    int compIdx = GhostComponentIndex[baseOffset + comp].ComponentIndex;
                    int serializerIdx = GhostComponentIndex[baseOffset + comp].SerializerIndex;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (compIdx >= ghostChunkComponentTypesLength)
                        throw new System.InvalidOperationException("Component index out of range");
#endif
                    if ((GhostComponentIndex[baseOffset + comp].SendMask&requiredSendMask) == 0)
                        continue;

                    if (GhostComponentCollection[serializerIdx].SerializesEnabledBit != 0)
                    {
                        if (chunk.Has(ref ghostChunkComponentTypesPtr[compIdx]))
                        {
                            bool isSet = (enabledBitPtr[ent>>6] & (1ul<<(ent&0x3f))) != 0;
                            chunk.SetComponentEnabled(ref ghostChunkComponentTypesPtr[compIdx], ent, isSet);
                        }
                        enabledBitPtr = PredictionBackupState.GetNextEnabledBits(enabledBitPtr, chunk.Capacity);
                    }

                    var compSize = GhostComponentCollection[serializerIdx].ComponentType.IsBuffer
                        ? GhostSystemConstants.DynamicBufferComponentSnapshotSize
                        : GhostComponentCollection[serializerIdx].ComponentSize;

                    //If the component does not have any ghost fields (so nothing to restore)
                    //we don't need to restore the data and we don't need to advance the
                    //data ptr either. No space has been reserved for this component in the backup buffer, see the
                    //GhostPredictionHistorySystem)
                    if (!GhostComponentCollection[serializerIdx].HasGhostFields)
                    {
                        continue;
                    }

                    if (!chunk.Has(ref ghostChunkComponentTypesPtr[compIdx]))
                    {
                        dataPtr = PredictionBackupState.GetNextData(dataPtr, compSize, chunk.Capacity);
                        continue;
                    }
                    //Do not restore the backup if the component is never received by this client (PlayerGhostFilter setting)
                    if ((GhostComponentCollection[serializerIdx].SendToOwner & requiredOwnerMask) == 0)
                    {
                        dataPtr = PredictionBackupState.GetNextData(dataPtr, compSize, chunk.Capacity);
                        continue;
                    }

                    if (!GhostComponentCollection[serializerIdx].ComponentType.IsBuffer)
                    {
                        var compData = (byte*)chunk.GetDynamicComponentDataArrayReinterpret<byte>(ref ghostChunkComponentTypesPtr[compIdx], compSize).GetUnsafeReadOnlyPtr();
                        GhostComponentCollection[serializerIdx].RestoreFromBackup.Ptr.Invoke((System.IntPtr)(compData + ent * compSize), (System.IntPtr)(dataPtr + ent * compSize));
                    }
                    else
                    {
                        var backupData = (int*)(dataPtr + ent * compSize);
                        var bufLen = backupData[0];
                        var bufOffset = backupData[1];
                        var elemSize = GhostComponentCollection[serializerIdx].ComponentSize;
                        var bufferDataPtr = bufferBackupDataPtr + bufOffset;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                        if ((bufOffset + bufLen*elemSize) > PredictionBackupState.GetBufferDataCapacity(state))
                            throw new System.InvalidOperationException("Overflow reading data from dynamic snapshot memory buffer");
#endif
                        var bufferAccessor = chunk.GetUntypedBufferAccessor(ref ghostChunkComponentTypesPtr[compIdx]);

                        //IMPORTANT NOTE: The RestoreFromBackup restore only the serialized fields for a given struct.
                        //Differently from the component counterpart, when the dynamic snapshot buffer get resized the memory is not
                        //cleared (for performance reason) and some portion of the data could be left "uninitialized" with random values
                        //in case some of the element fields does not have a [GhostField] annotation.
                        //For such a reason we enforced a rule: BufferElementData MUST have all fields annotated with the GhostFieldAttribute.
                        //This solve the problem and we might relax that condition later.

                        bufferAccessor.ResizeUninitialized(ent, bufLen);
                        var bufferPointer = (byte*)bufferAccessor.GetUnsafePtr(ent);

                        for(int i=0;i<bufLen;++i)
                        {
                            GhostComponentCollection[serializerIdx].RestoreFromBackup.Ptr.Invoke((System.IntPtr)(bufferPointer), (System.IntPtr)(bufferDataPtr));
                            bufferPointer += elemSize;
                            bufferDataPtr += elemSize;
                        }
                    }
                    dataPtr = PredictionBackupState.GetNextData(dataPtr, compSize, chunk.Capacity);
                }
                if (typeData.NumChildComponents > 0)
                {
                    var linkedEntityGroupAccessor = chunk.GetBufferAccessor(ref linkedEntityGroupType);
                    for (int comp = numBaseComponents; comp < typeData.NumComponents; ++comp)
                    {
                        int compIdx = GhostComponentIndex[baseOffset + comp].ComponentIndex;
                        int serializerIdx = GhostComponentIndex[baseOffset + comp].SerializerIndex;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                        if (compIdx >= ghostChunkComponentTypesLength)
                            throw new System.InvalidOperationException("Component index out of range");
#endif
                        if ((GhostComponentIndex[baseOffset + comp].SendMask&requiredSendMask) == 0)
                            continue;

                        var compSize = GhostComponentCollection[serializerIdx].ComponentType.IsBuffer
                            ? GhostSystemConstants.DynamicBufferComponentSnapshotSize
                            : GhostComponentCollection[serializerIdx].ComponentSize;

                        var linkedEntityGroup = linkedEntityGroupAccessor[ent];
                        var childEnt = linkedEntityGroup[GhostComponentIndex[typeData.FirstComponent + comp].EntityIndex].Value;

                        if (GhostComponentCollection[serializerIdx].SerializesEnabledBit != 0)
                        {
                            if (childEntityLookup.TryGetValue(childEnt, out var enabledChildChunk) &&
                                enabledChildChunk.Chunk.Has(ref ghostChunkComponentTypesPtr[compIdx]))
                            {
                                bool isSet = (enabledBitPtr[ent>>6] & (1ul<<(ent&0x3f))) != 0;
                                enabledChildChunk.Chunk.SetComponentEnabled(ref ghostChunkComponentTypesPtr[compIdx], enabledChildChunk.IndexInChunk, isSet);
                            }
                            enabledBitPtr = PredictionBackupState.GetNextEnabledBits(enabledBitPtr, chunk.Capacity);
                        }

                        //If the component does not have any ghost fields (so nothing to restore)
                        //we don't need to restore the data and we don't need to advance the
                        //data ptr either. No space has been reserved for this component in the backup buffer, see the
                        //GhostPredictionHistorySystem)
                        if(!GhostComponentCollection[serializerIdx].HasGhostFields)
                            continue;

                        if ((GhostComponentCollection[serializerIdx].SendToOwner & requiredOwnerMask) == 0)
                        {
                            dataPtr = PredictionBackupState.GetNextData(dataPtr, compSize, chunk.Capacity);
                            continue;
                        }

                        if (childEntityLookup.TryGetValue(childEnt, out var childChunk) &&
                            childChunk.Chunk.Has(ref ghostChunkComponentTypesPtr[compIdx]))
                        {
                            if (!GhostComponentCollection[serializerIdx].ComponentType.IsBuffer)
                            {
                                var compData = (byte*)childChunk.Chunk.GetDynamicComponentDataArrayReinterpret<byte>(ref ghostChunkComponentTypesPtr[compIdx], compSize).GetUnsafeReadOnlyPtr();
                                GhostComponentCollection[serializerIdx].RestoreFromBackup.Ptr.Invoke((System.IntPtr)(compData + childChunk.IndexInChunk * compSize), (System.IntPtr)(dataPtr + ent * compSize));
                            }
                            else
                            {
                                var backupData = (int*)(dataPtr + ent * compSize);
                                var bufLen = backupData[0];
                                var bufOffset = backupData[1];
                                var elemSize = GhostComponentCollection[serializerIdx].ComponentSize;
                                var bufferDataPtr = bufferBackupDataPtr + bufOffset;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                                if ((bufOffset + bufLen*elemSize) > PredictionBackupState.GetBufferDataCapacity(state))
                                    throw new System.InvalidOperationException("Overflow reading data from dynamic snapshot memory buffer");
#endif
                                var bufferAccessor = childChunk.Chunk.GetUntypedBufferAccessor(ref ghostChunkComponentTypesPtr[compIdx]);
                                bufferAccessor.ResizeUninitialized(childChunk.IndexInChunk, bufLen);
                                var bufferPointer = (byte*)bufferAccessor.GetUnsafePtr(childChunk.IndexInChunk);
                                for(int i=0;i<bufLen;++i)
                                {
                                    GhostComponentCollection[serializerIdx].RestoreFromBackup.Ptr.Invoke((System.IntPtr)(bufferPointer), (System.IntPtr)(bufferDataPtr));
                                    bufferPointer += elemSize;
                                    bufferDataPtr += elemSize;
                                }
                            }
                        }
                        dataPtr = PredictionBackupState.GetNextData(dataPtr, compSize, chunk.Capacity);
                    }
                }

                return true;
            }
        }
        [BurstCompile]
        struct UpdateGhostOwnerIsLocal : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<GhostOwner> ghostOwnerType;
            public ComponentTypeHandle<GhostOwnerIsLocal> ghostOwnerIsLocalType;
            public int localNetworkId;
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // This job is not written to support queries with enableable component types.
                Assert.IsFalse(useEnabledMask);

                var owners = chunk.GetNativeArray(ref ghostOwnerType);
                for (int i = 0; i < owners.Length; ++i)
                    chunk.SetComponentEnabled(ref ghostOwnerIsLocalType, i, owners[i].NetworkId == localNetworkId);
            }
        }

        [BurstCompile]
        struct UpdateLastInterpolatedTick : IJob
        {
            [ReadOnly]
            public ComponentLookup<NetworkSnapshotAck> AckFromEntity;
            public Entity                                               AckSingleton;
            public NativeReference<NetworkTick>                         LastInterpolatedTick;
            public NetworkTick                                          InterpolationTick;
            public float                                                InterpolationTickFraction;

            public void Execute()
            {
                var ack = AckFromEntity[AckSingleton];
                if (InterpolationTick.IsValid && ack.LastReceivedSnapshotByLocal.IsValid && !InterpolationTick.IsNewerThan(ack.LastReceivedSnapshotByLocal))
                {
                    var lastInterpolTick = InterpolationTick;
                    // Make sure it is the last full interpolated tick. It is only used to see if a static ghost already has the latest state applied
                    if (InterpolationTickFraction < 1)
                        lastInterpolTick.Decrement();
                    LastInterpolatedTick.Value = lastInterpolTick;
                }
            }
        }

        static readonly Unity.Profiling.ProfilerMarker k_Scheduling = new Unity.Profiling.ProfilerMarker("GhostUpdateSystem_Scheduling");
        static readonly Unity.Profiling.ProfilerMarker k_ChangeFiltering = new Unity.Profiling.ProfilerMarker("GhostUpdateSystem_ChangeFiltering");
        private EntityQuery m_ghostQuery;
        private EntityQuery m_GhostOwnerIsLocalQuery;
        private NetworkTick m_LastPredictedTick;
        private NativeReference<NetworkTick> m_LastInterpolatedTick;
        private NativeParallelHashMap<NetworkTick, NetworkTick> m_AppliedPredictedTicks;

        BufferLookup<GhostComponentSerializer.State> m_GhostComponentCollectionFromEntity;
        BufferLookup<GhostCollectionPrefabSerializer> m_GhostTypeCollectionFromEntity;
        BufferLookup<GhostCollectionComponentIndex> m_GhostComponentIndexFromEntity;
        ComponentLookup<NetworkSnapshotAck> m_NetworkSnapshotAckLookup;

        ComponentTypeHandle<PredictedGhost> m_PredictedGhostTypeHandle;
        ComponentTypeHandle<GhostInstance> m_GhostComponentTypeHandle;
        ComponentTypeHandle<SnapshotData> m_SnapshotDataTypeHandle;
        BufferTypeHandle<SnapshotDataBuffer> m_SnapshotDataBufferTypeHandle;
        BufferTypeHandle<SnapshotDynamicDataBuffer> m_SnapshotDynamicDataBufferTypeHandle;
        BufferTypeHandle<LinkedEntityGroup> m_LinkedEntityGroupTypeHandle;
        ComponentTypeHandle<PreSpawnedGhostIndex> m_PreSpawnedGhostIndexTypeHandle;
        EntityTypeHandle m_EntityTypeHandle;
        ComponentTypeHandle<GhostOwner> m_GhostOwnerType;
        ComponentTypeHandle<GhostOwnerIsLocal> m_GhostOwnerIsLocalType;

        public void OnCreate(ref SystemState systemState)
        {
#if UNITY_2022_2_14F1_OR_NEWER
            int maxThreadCount = JobsUtility.ThreadIndexCount;
#else
            int maxThreadCount = JobsUtility.MaxJobThreadCount;
#endif

            var ghostUpdateVersionSingleton = systemState.EntityManager.CreateEntity(ComponentType.ReadWrite<GhostUpdateVersion>());
            systemState.EntityManager.SetName(ghostUpdateVersionSingleton, "GhostUpdateVersion-Singleton");

            m_AppliedPredictedTicks = new NativeParallelHashMap<NetworkTick, NetworkTick>(CommandDataUtility.k_CommandDataMaxSize*maxThreadCount / 4, Allocator.Persistent);
            var singletonEntity = systemState.EntityManager.CreateEntity(ComponentType.ReadWrite<GhostPredictionGroupTickState>());
            systemState.EntityManager.SetName(singletonEntity, "AppliedPredictedTicks-Singleton");
            SystemAPI.SetSingleton(new GhostPredictionGroupTickState { AppliedPredictedTicks = m_AppliedPredictedTicks });

            var queryBuilder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SnapshotData, GhostInstance>()
                .WithAllRW<SnapshotDataBuffer>()
                .WithAbsent<PendingSpawnPlaceholder, PredictedGhostSpawnRequest>();
            m_ghostQuery = queryBuilder.Build(systemState.EntityManager);
            queryBuilder.Reset();
            m_GhostOwnerIsLocalQuery = queryBuilder.WithAllRW<GhostOwnerIsLocal>().WithAll<GhostOwner>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                .Build(systemState.EntityManager);
            systemState.RequireForUpdate<NetworkStreamInGame>();
            systemState.RequireForUpdate<GhostCollection>();

            m_LastInterpolatedTick = new NativeReference<NetworkTick>(Allocator.Persistent);


            m_GhostComponentCollectionFromEntity = systemState.GetBufferLookup<GhostComponentSerializer.State>(true);
            m_GhostTypeCollectionFromEntity = systemState.GetBufferLookup<GhostCollectionPrefabSerializer>(true);
            m_GhostComponentIndexFromEntity = systemState.GetBufferLookup<GhostCollectionComponentIndex>(true);
            m_NetworkSnapshotAckLookup = systemState.GetComponentLookup<NetworkSnapshotAck>(true);

            m_PredictedGhostTypeHandle = systemState.GetComponentTypeHandle<PredictedGhost>();
            m_GhostComponentTypeHandle = systemState.GetComponentTypeHandle<GhostInstance>(true);
            m_SnapshotDataTypeHandle = systemState.GetComponentTypeHandle<SnapshotData>(true);
            m_SnapshotDataBufferTypeHandle = systemState.GetBufferTypeHandle<SnapshotDataBuffer>(true);
            m_SnapshotDynamicDataBufferTypeHandle = systemState.GetBufferTypeHandle<SnapshotDynamicDataBuffer>(true);
            m_LinkedEntityGroupTypeHandle = systemState.GetBufferTypeHandle<LinkedEntityGroup>(true);
            m_PreSpawnedGhostIndexTypeHandle = systemState.GetComponentTypeHandle<PreSpawnedGhostIndex>(true);
            m_EntityTypeHandle = systemState.GetEntityTypeHandle();
            m_GhostOwnerType = systemState.GetComponentTypeHandle<GhostOwner>(true);
            m_GhostOwnerIsLocalType = systemState.GetComponentTypeHandle<GhostOwnerIsLocal>();
        }
        public void OnDestroy(ref SystemState systemState)
        {
            m_LastInterpolatedTick.Dispose();
            m_AppliedPredictedTicks.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState systemState)
        {
            var clientTickRate = NetworkTimeSystem.DefaultClientTickRate;
            if (HasSingleton<ClientTickRate>())
                clientTickRate = GetSingleton<ClientTickRate>();

            var networkTime = GetSingleton<NetworkTime>();
            var lastBackupTick = GetSingleton<GhostSnapshotLastBackupTick>();
            var ghostHistoryPrediction = GetSingleton<GhostPredictionHistoryState>();

            if (!networkTime.ServerTick.IsValid)
                return;

            var backupTick = lastBackupTick.Value;
            // If tick has moved backwards we might have a backup that is newer than the target tick, if that is the case we do not want to use it
            if (backupTick.IsValid && !networkTime.ServerTick.IsNewerThan(backupTick))
                backupTick = NetworkTick.Invalid;

            var interpolationTick = networkTime.InterpolationTick;
            var interpolationTickFraction = networkTime.InterpolationTickFraction;
            if (!m_ghostQuery.IsEmptyIgnoreFilter)
            {
                m_GhostComponentCollectionFromEntity.Update(ref systemState);
                m_GhostTypeCollectionFromEntity.Update(ref systemState);
                m_GhostComponentIndexFromEntity.Update(ref systemState);
                m_PredictedGhostTypeHandle.Update(ref systemState);
                m_GhostComponentTypeHandle.Update(ref systemState);
                m_SnapshotDataTypeHandle.Update(ref systemState);
                m_SnapshotDataBufferTypeHandle.Update(ref systemState);
                m_SnapshotDynamicDataBufferTypeHandle.Update(ref systemState);
                m_LinkedEntityGroupTypeHandle.Update(ref systemState);
                m_PreSpawnedGhostIndexTypeHandle.Update(ref systemState);
                m_EntityTypeHandle.Update(ref systemState);
                var localNetworkId = GetSingleton<NetworkId>().Value;
                var updateJob = new UpdateJob
                {
                    GhostCollectionSingleton = GetSingletonEntity<GhostCollection>(),
                    GhostComponentCollectionFromEntity = m_GhostComponentCollectionFromEntity,
                    GhostTypeCollectionFromEntity = m_GhostTypeCollectionFromEntity,
                    GhostComponentIndexFromEntity = m_GhostComponentIndexFromEntity,

                    GhostMap = GetSingleton<SpawnedGhostEntityMap>().Value,
#if UNITY_EDITOR || NETCODE_DEBUG
                    minMaxSnapshotTick = GetSingletonRW<GhostStatsCollectionMinMaxTick>().ValueRO.Value,
#endif

                    interpolatedTargetTick = interpolationTick,
                    interpolatedTargetTickFraction = interpolationTickFraction,

                    predictedTargetTick = networkTime.ServerTick,
                    predictedTargetTickFraction = networkTime.ServerTickFraction,
                    appliedPredictedTicks = m_AppliedPredictedTicks.AsParallelWriter(),
                    PredictedGhostType = m_PredictedGhostTypeHandle,
                    lastPredictedTick = m_LastPredictedTick,
                    lastInterpolatedTick = m_LastInterpolatedTick.Value,

                    ghostType = m_GhostComponentTypeHandle,
                    ghostSnapshotDataType = m_SnapshotDataTypeHandle,
                    ghostSnapshotDataBufferType = m_SnapshotDataBufferTypeHandle,
                    ghostSnapshotDynamicDataBufferType = m_SnapshotDynamicDataBufferTypeHandle,
                    childEntityLookup = systemState.GetEntityStorageInfoLookup(),
                    linkedEntityGroupType = m_LinkedEntityGroupTypeHandle,
                    prespawnGhostIndexType = m_PreSpawnedGhostIndexTypeHandle,

                    predictionStateBackupTick = backupTick,
                    predictionStateBackup = ghostHistoryPrediction.PredictionState,
                    entityType = m_EntityTypeHandle,
                    ghostOwnerId = localNetworkId,
                    MaxExtrapolationTicks = clientTickRate.MaxExtrapolationTimeSimTicks,
                    netDebug = GetSingleton<NetDebug>()
                };
                //@TODO: Use BufferFromEntity
                var ghostComponentCollection = systemState.EntityManager.GetBuffer<GhostCollectionComponentType>(updateJob.GhostCollectionSingleton);
                DynamicTypeList.PopulateList(ref systemState, ghostComponentCollection, false, ref updateJob.DynamicTypeList); // Change Filtering is handled on a per-chunk basis, inside the job.
                k_Scheduling.Begin();
                systemState.Dependency = updateJob.ScheduleParallelByRef(m_ghostQuery, systemState.Dependency);
                k_Scheduling.End();

                m_GhostOwnerType.Update(ref systemState);
                m_GhostOwnerIsLocalType.Update(ref systemState);
                var updateOwnerIsLocal = new UpdateGhostOwnerIsLocal
                {
                    ghostOwnerType = m_GhostOwnerType,
                    ghostOwnerIsLocalType = m_GhostOwnerIsLocalType,
                    localNetworkId = localNetworkId
                };
                k_Scheduling.Begin();
                systemState.Dependency = updateOwnerIsLocal.ScheduleParallel(m_GhostOwnerIsLocalQuery, systemState.Dependency);
                k_Scheduling.End();
            }

            m_LastPredictedTick = networkTime.ServerTick;
            if (networkTime.IsPartialTick)
                m_LastPredictedTick = NetworkTick.Invalid;

            // If the interpolation target for this frame was received we can update which the latest fully applied interpolation tick is
            m_NetworkSnapshotAckLookup.Update(ref systemState);
            var updateInterpolatedTickJob = new UpdateLastInterpolatedTick
            {
                AckFromEntity = m_NetworkSnapshotAckLookup,
                AckSingleton = SystemAPI.GetSingletonEntity<NetworkSnapshotAck>(),
                LastInterpolatedTick = m_LastInterpolatedTick,
                InterpolationTick = interpolationTick,
                InterpolationTickFraction = interpolationTickFraction
            };
            k_Scheduling.Begin();
            systemState.Dependency = updateInterpolatedTickJob.Schedule(systemState.Dependency);
            k_Scheduling.End();

            GetSingletonRW<GhostUpdateVersion>().ValueRW.LastSystemVersion = systemState.LastSystemVersion;
        }
    }
}
