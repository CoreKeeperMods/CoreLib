using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Assertions;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Properties;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Entities
{
    [BurstCompile]
    static unsafe partial class EntityDiffer
    {
        static bool TryGetEntityGuidComponent(EntityComponentStore* ecs, Entity entity, TypeIndex entityGuidTypeIndex, out EntityGuid entityGuid)
        {
            entityGuid = default;
            if (!ecs->HasComponent(entity, entityGuidTypeIndex))
            {
                return false;
            }

            entityGuid = *(EntityGuid*)ecs->GetComponentDataWithTypeRO(entity, entityGuidTypeIndex);
            return true;
        }

        // This value has to be power of two.
        public const int ComponentChangesBatchCount = 128;
        public struct DeferredSharedComponentChange
        {
            public EntityGuid EntityGuid;
            public TypeIndex  TypeIndex;
            public int BeforeSharedComponentIndex;
            public int AfterSharedComponentIndex;
        }

        public struct DeferredManagedComponentChange
        {
            public EntityGuid EntityGuid;
            public TypeIndex  TypeIndex;
            public int AfterManagedComponentIndex;
            public int BeforeManagedComponentIndex;
        }

        internal struct NameChangeSet : IDisposable
        {
            public readonly NativeArray<FixedString64Bytes> Names;
            public readonly NativeArray<EntityGuid> NameChangedEntityGuids;
            public int NameChangedCount;

            public NameChangeSet(int length, AllocatorManager.AllocatorHandle allocator)
            {
                Names = CollectionHelper.CreateNativeArray<FixedString64Bytes>(length, allocator);
                NameChangedEntityGuids = CollectionHelper.CreateNativeArray<EntityGuid>(length, allocator);
                NameChangedCount = 0;
            }

            public void Dispose()
            {
                Names.Dispose();
                NameChangedEntityGuids.Dispose();
            }
        }
        public struct ComponentChanges : IDisposable
        {
            public readonly PackedEntityGuidsCollection Entities;
            public readonly PackedCollection<ComponentTypeHash> ComponentTypes;
            public readonly NativeList<PackedComponent> AddComponents;
            public readonly NativeList<FilteredArchetype> AddArchetypes;
            public readonly NativeList<PackedComponent> RemoveComponents;
            public readonly NativeList<PackedComponentDataChange> SetComponents;
            public readonly NativeList<LinkedEntityGroupChange> LinkedEntityGroupAdditions;
            public readonly NativeList<LinkedEntityGroupChange> LinkedEntityGroupRemovals;
            public readonly NativeList<EntityReferenceChange> EntityReferenceChanges;
            public readonly NativeList<BlobAssetReferenceChange> BlobAssetReferenceChanges;
            public readonly NativeList<byte> ComponentData;
            public readonly NativeList<DeferredSharedComponentChange> SharedComponentChanges;
            public readonly NativeList<DeferredManagedComponentChange> ManagedComponentChanges;

            public readonly bool IsCreated;
            private int ReserveSize;
            public ComponentChanges(int count)
            {
                Entities = new PackedEntityGuidsCollection(count, Allocator.Persistent);
                ComponentTypes = new PackedCollection<ComponentTypeHash>(count, Allocator.Persistent);
                AddComponents = new NativeList<PackedComponent>(count * 16, Allocator.Persistent);
                AddArchetypes = new NativeList<FilteredArchetype>(count, Allocator.Persistent);
                RemoveComponents = new NativeList<PackedComponent>(count, Allocator.Persistent);
                SetComponents = new NativeList<PackedComponentDataChange>(count * 8, Allocator.Persistent);
                LinkedEntityGroupAdditions = new NativeList<LinkedEntityGroupChange>(count, Allocator.Persistent);
                LinkedEntityGroupRemovals = new NativeList<LinkedEntityGroupChange>(count, Allocator.Persistent);
                EntityReferenceChanges = new NativeList<EntityReferenceChange>(count, Allocator.Persistent);
                BlobAssetReferenceChanges = new NativeList<BlobAssetReferenceChange>(count, Allocator.Persistent);
                ComponentData = new NativeList<byte>(count * 256, Allocator.Persistent);
                SharedComponentChanges = new NativeList<DeferredSharedComponentChange>(count * 16, Allocator.Persistent);
                ManagedComponentChanges = new NativeList<DeferredManagedComponentChange>(count, Allocator.Persistent);
                IsCreated = true;
                ReserveSize = count;

                for (int i = 0; i < count; i++)
                {
                    AddArchetypes.Add(new FilteredArchetype()
                    {
                        EntityCount = 0,
                        PackedEntityIndices = new UnsafeList<int>(1, Allocator.Persistent),
                        TypeIndices = new UnsafeList<TypeIndex>(1, Allocator.Persistent)
                    });
                }
            }

            public void ResizeAndClear(int count)
            {
                if (count > ReserveSize)
                {
                    // Instead of doing resize on the buffers we delete/new them so
                    // we don't do a memcpy of all the data (which won't be used anyway)
                    Dispose();
                    this = new ComponentChanges(count);
                    ReserveSize = count;
                }
                else
                {
                    Clear();
                }
            }

            public void Clear()
            {
                Entities.Clear();
                ComponentTypes.Clear();
                if (AddComponents.IsCreated) AddComponents.Clear();
                if (AddArchetypes.IsCreated)
                {
                    for (int i = 0; i < AddArchetypes.Length; i++)
                    {
                        ref var arch = ref AddArchetypes.ElementAt(i);
                        arch.EntityCount = 0;
                        arch.TypeIndices.Clear();
                        arch.PackedEntityIndices.Clear();
                    }
                }

                if (RemoveComponents.IsCreated) RemoveComponents.Clear();
                if (SetComponents.IsCreated) SetComponents.Clear();
                if (LinkedEntityGroupAdditions.IsCreated) LinkedEntityGroupAdditions.Clear();
                if (LinkedEntityGroupRemovals.IsCreated) LinkedEntityGroupRemovals.Clear();
                if (EntityReferenceChanges.IsCreated) EntityReferenceChanges.Clear();
                if (BlobAssetReferenceChanges.IsCreated) BlobAssetReferenceChanges.Clear();
                if (ComponentData.IsCreated) ComponentData.Clear();
                if (SharedComponentChanges.IsCreated) SharedComponentChanges.Clear();
                if (ManagedComponentChanges.IsCreated) ManagedComponentChanges.Clear();
            }

            public void Dispose()
            {
                Entities.Dispose();
                ComponentTypes.Dispose();

                if (AddComponents.IsCreated) AddComponents.Dispose();
                if (AddArchetypes.IsCreated)
                {
                    foreach (var arch in AddArchetypes)
                    {
                        arch.TypeIndices.Dispose();
                        arch.PackedEntityIndices.Dispose();
                    }
                    AddArchetypes.Dispose();
                }
                if (RemoveComponents.IsCreated) RemoveComponents.Dispose();
                if (SetComponents.IsCreated) SetComponents.Dispose();
                if (LinkedEntityGroupAdditions.IsCreated) LinkedEntityGroupAdditions.Dispose();
                if (LinkedEntityGroupRemovals.IsCreated) LinkedEntityGroupRemovals.Dispose();
                if (EntityReferenceChanges.IsCreated) EntityReferenceChanges.Dispose();
                if (BlobAssetReferenceChanges.IsCreated) BlobAssetReferenceChanges.Dispose();
                if (ComponentData.IsCreated) ComponentData.Dispose();
                if (SharedComponentChanges.IsCreated) SharedComponentChanges.Dispose();
                if (ManagedComponentChanges.IsCreated) ManagedComponentChanges.Dispose();
            }
        }


        public struct PackedEntityGuidsCollection : IDisposable
        {
            // This array contains both added and modified entities where AddedCount is the number
            // number of added entities at the start of the list (rest is modified entities)
            [NativeDisableContainerSafetyRestriction]
            public NativeList<EntityGuid> List;

            public NativeReference<int> AddedCount;

            public PackedEntityGuidsCollection(int capacity, AllocatorManager.AllocatorHandle label)
            {
                List = new NativeList<EntityGuid>(capacity, label);
                AddedCount = new NativeReference<int>(label);
            }
            public void ResizeUninitialized(int count)
            {
                List.ResizeUninitialized(count);
            }

            public void Clear()
            {
                if (List.IsCreated) List.Clear();
            }

            public void Dispose()
            {
                if (List.IsCreated) List.Dispose();
                if (AddedCount.IsCreated) AddedCount.Dispose();
            }

            public int BinarySearchRange(EntityGuid key, int startIndex, int endIndex, int entryHint)
            {
                if (startIndex == endIndex)
                    return -1;

                // First we try with the hint index and validate that it's within range
                int index = startIndex + entryHint;

                if (index >= 0 && index < endIndex)
                {
                    if (key == List[index])
                    {
                        return index;
                    }
                }

                int min = startIndex;
                int max = endIndex - 1;

                // If we only have one entry
                if (min == max)
                {
                    if (key == List[0])
                        return 0;
                }

                while (min <= max)
                {
                    int middle = min + (max - min) / 2;
                    EntityGuid guid = List[middle];
                    var r = guid.CompareTo(key);
                    if (r == 0)
                        return middle;

                    if (r > 0)
                        max = middle - 1;
                    else
                        min = middle + 1;
                }

                return -1;
            }

            public int Get(EntityGuid value, int tableHint, int entryHint)
            {
                int index = 0;
                var addedCount = AddedCount.Value;

                // If tableHint == 0 we search for addedCount range
                if (tableHint == 0)
                {
                    // search added
                    index = BinarySearchRange(value, 0, addedCount, entryHint);

                    if (index == -1)
                    {
                        index = BinarySearchRange(value, addedCount, List.Length, entryHint);
                    }
                }
                else
                {
                    index = BinarySearchRange(value, addedCount, List.Length, entryHint);

                    if (index == -1)
                    {
                        index = BinarySearchRange(value, 0, addedCount, entryHint);
                    }
                }

                if (index == -1)
                {
                    throw new Exception($"Unable to find the correct for {value} - index, ranges: {addedCount} - {List.Length}");
                }

                return index;
            }
        }

        public struct PackedCollection<T> : IDisposable where T : unmanaged, IEquatable<T>
        {
            [NativeDisableContainerSafetyRestriction]
            public NativeList<T> List;

            [NativeDisableContainerSafetyRestriction]
            public NativeParallelHashMap<T, int> Lookup;

            public PackedCollection(int capacity, AllocatorManager.AllocatorHandle label)
            {
                List = new NativeList<T>(capacity, label);
                Lookup = new NativeParallelHashMap<T, int>(capacity, label);
            }

            public void ResizeUninitialized(int count)
            {
                List.ResizeUninitialized(count);
                Lookup.Clear();
            }

            public void Clear()
            {
                if (List.IsCreated) List.Clear();
                if (Lookup.IsCreated) Lookup.Clear();
            }

            public void Dispose()
            {
                if (List.IsCreated) List.Dispose();
                if (Lookup.IsCreated) Lookup.Dispose();
            }

            public int GetOrAdd(T value)
            {
                if (Lookup.TryGetValue(value, out var index))
                {
                    return index;
                }
                index = List.Length;
                List.Add(value);
                Lookup.TryAdd(value, index);
                return index;
            }

            public int Get(T value)
            {
                if (Lookup.TryGetValue(value, out var index))
                {
                    return index;
                }
                else
                {
                    return -1;
                }
            }
        }

        static internal void GatherLinkedEntityGroupChanges(
            EntityGuid entityGuid,
            NativeArray<EntityGuid> beforeLinkedEntityGroup,
            NativeArray<EntityGuid> afterLinkedEntityGroup,
            ref NativeList<LinkedEntityGroupChange> additions,
            ref NativeList<LinkedEntityGroupChange> removals)
        {
            beforeLinkedEntityGroup.Sort();
            afterLinkedEntityGroup.Sort();

            var beforeIndex = 0;
            var afterIndex = 0;

            int beforeLength = beforeLinkedEntityGroup.Length;
            int afterLength = afterLinkedEntityGroup.Length;

            while (beforeIndex < beforeLength && afterIndex < afterLength)
            {
                var beforeEntityGuid = beforeLinkedEntityGroup[beforeIndex];
                var afterEntityGuid = afterLinkedEntityGroup[afterIndex];

                var comparison = beforeEntityGuid.CompareTo(afterEntityGuid);

                if (comparison == 0)
                {
                    // If the guids are equal, we know that the entity exists in both states.
                    beforeIndex++;
                    afterIndex++;
                }
                else if (comparison > 0)
                {
                    // If the before guid is greater, then we know that whatever guid we compared to
                    // belongs to an entity that was added. Otherwise, we would already have matched it
                    // in the first case before.
                    additions.Add(new LinkedEntityGroupChange
                        {RootEntityGuid = entityGuid, ChildEntityGuid = afterEntityGuid});
                    afterIndex++;
                }
                else if (comparison < 0)
                {
                    // If the before guid is smaller, then we know that that entity must have been
                    // removed
                    removals.Add(new LinkedEntityGroupChange
                        {RootEntityGuid = entityGuid, ChildEntityGuid = beforeEntityGuid});
                    beforeIndex++;
                }
            }

            while (beforeIndex < beforeLength)
            {
                // If the entity is in "before" but not "after", it's been removed.
                removals.Add(new LinkedEntityGroupChange
                    {RootEntityGuid = entityGuid, ChildEntityGuid = beforeLinkedEntityGroup[beforeIndex++]});
            }

            while (afterIndex < afterLength)
            {
                // If the entity is in "after" but not "before", it's been added.
                additions.Add(new LinkedEntityGroupChange
                    {RootEntityGuid = entityGuid, ChildEntityGuid = afterLinkedEntityGroup[afterIndex++]});
            }
        }

        [BurstCompile]
        struct GatherComponentChangesBuildPacked : IJob
        {
            [ReadOnly] public NativeList<CreatedEntity> CreatedEntities;
            [ReadOnly] public NativeList<ModifiedEntity> ModifiedEntities;
            [ReadOnly] [NativeDisableUnsafePtrRestriction] public TypeManager.TypeInfo* TypeInfo;

            [NativeDisableUnsafePtrRestriction] public EntityComponentStore* AfterEntityComponentStore;
            [NativeDisableUnsafePtrRestriction] public EntityComponentStore* BeforeEntityComponentStore;

            public PackedEntityGuidsCollection Entities;
            public PackedCollection<ComponentTypeHash> ComponentTypes;

            void AddStableTypeHash(TypeIndex typeIndex)
            {
                var flags = ComponentTypeFlags.None;

                if (typeIndex.IsChunkComponent)
                    flags |= ComponentTypeFlags.ChunkComponent;

                var stableTypeHash = TypeInfo[typeIndex.Index].StableTypeHash;

                ComponentTypes.GetOrAdd(new ComponentTypeHash
                {
                    StableTypeHash = stableTypeHash,
                    Flags = flags
                });
            }

            public void Execute()
            {
                var archLookup = new UnsafeParallelHashSet<ulong>(128, Allocator.Temp);

                for (int i = 0; i < CreatedEntities.Length; ++i)
                {
                    var entityGuid = CreatedEntities[i].EntityGuid;
                    var afterEntity = CreatedEntities[i].AfterEntityInChunk;
                    var afterChunk = afterEntity.Chunk;
                    var afterArchetype = AfterEntityComponentStore->GetArchetype(afterChunk);
                    var afterTypesCount = afterArchetype->TypesCount;

                    Entities.List.Add(entityGuid);

                    if (archLookup.Add((ulong)(IntPtr)afterArchetype))
                    {
                        for (var afterIndexInTypeArray = 1; afterIndexInTypeArray < afterTypesCount; afterIndexInTypeArray++)
                        {
                            var afterTypeInArchetype = afterArchetype->Types[afterIndexInTypeArray];

                            if (afterTypeInArchetype.IsCleanupComponent || afterTypeInArchetype.IsBakeOnlyType || afterTypeInArchetype.IsChunkComponent)
                                continue;

                            AddStableTypeHash(afterTypeInArchetype.TypeIndex);
                        }

                    }
                }

                Entities.AddedCount.Value = Entities.List.Length;

                for (int i = 0; i < ModifiedEntities.Length; ++i)
                {
                    var modification = ModifiedEntities[i];
                    var entityGuid = modification.EntityGuid;

                    var afterEntity = modification.AfterEntityInChunk;
                    var afterChunk = afterEntity.Chunk;
                    var afterArchetype = AfterEntityComponentStore->GetArchetype(afterChunk);
                    var afterTypesCount = afterArchetype->TypesCount;

                    var beforeEntity = modification.BeforeEntityInChunk;
                    var beforeChunk = beforeEntity.Chunk;

                    var beforeArchetype = BeforeEntityComponentStore->GetArchetype(beforeChunk);
                    var beforeTypesCount = beforeArchetype->TypesCount;

                    Entities.List.Add(entityGuid);

                    if (archLookup.Add((ulong)(IntPtr)afterArchetype))
                    {
                        for (var afterIndexInTypeArray = 1; afterIndexInTypeArray < afterTypesCount; afterIndexInTypeArray++)
                        {
                            var afterTypeInArchetype = afterArchetype->Types[afterIndexInTypeArray];

                            if (afterTypeInArchetype.IsCleanupComponent || afterTypeInArchetype.IsBakeOnlyType || afterTypeInArchetype.IsChunkComponent)
                                continue;

                            var typeIndex = afterTypeInArchetype.TypeIndex;

                            AddStableTypeHash(afterTypeInArchetype.TypeIndex);
                        }

                    }

                    if (archLookup.Add((ulong)(IntPtr)beforeArchetype))
                    {
                        for (var beforeTypeIndexInArchetype = 1; beforeTypeIndexInArchetype < beforeTypesCount; beforeTypeIndexInArchetype++)
                        {
                            var beforeComponentTypeInArchetype = beforeArchetype->Types[beforeTypeIndexInArchetype];

                            if (beforeComponentTypeInArchetype.IsCleanupComponent || beforeComponentTypeInArchetype.IsBakeOnlyType || beforeComponentTypeInArchetype.IsChunkComponent)
                                continue;

                            var beforeTypeIndex = beforeComponentTypeInArchetype.TypeIndex;

                            if (-1 == ChunkDataUtility.GetIndexInTypeArray(afterArchetype, beforeTypeIndex))
                            {
                                AddStableTypeHash(beforeTypeIndex);
                            }
                        }

                    }
                }

                archLookup.Dispose();
            }
        }

        struct GatherComponentChanges
        {
            // Read-only variables set from the outside
            public GatherComponentChangesReadOnlyData ReadData;
            // Used for writing local (on stack/temp) data in a thread manner
            public GatherComponentChangesWriteOnlyData CacheData;
            // Atomic outputs written from the cached data
            public GatherComponentChangesOutput OutputData;
            public NativeHashMap<ulong, int> AddedArchetypes;
            public EntityComponentStore* AfterEntityComponentStore;
            public EntityComponentStore* BeforeEntityComponentStore;

            public void ProcessRange(int index)
            {
                int createdCount = ReadData.CreatedEntities.Length;
                int amountToCompute = ComponentChangesBatchCount;

                // The way the code works is that index can be both for Creations and Modifications in order to reduce the size of the code
                if (index < createdCount)
                {
                    int maxCreatedCount = math.min(createdCount, index + ComponentChangesBatchCount);

                    for (int i = index; i < maxCreatedCount; ++i)
                    {
                        var entityGuid = ReadData.CreatedEntities[i].EntityGuid;
                        var afterEntity = ReadData.CreatedEntities[i].AfterEntityInChunk;
                        var afterChunk = afterEntity.Chunk;
                        var afterArchetype = AfterEntityComponentStore->GetArchetype(afterChunk);
                        var afterTypesCount = afterArchetype->TypesCount;
                        var archetypeHash = afterArchetype->StableHash;

                        // The packedEntityIndex is used to store on which indices of the other packed arrays (e.g. AddComponents array)
                        // this archetype is used. This is necessary to match the entities up again in the patcher with the othe arrays
                        // Without this, the wrong archetype will be assigned to the wrong entity indices
                        var packedEntityIndex = ReadData.Entities.Get(entityGuid, 0, i);

                        // If the archetype is already known, update the entry
                        if (AddedArchetypes.TryGetValue(archetypeHash, out int indexInAddArchetypes))
                        {
                            // Add the current index to the PackedEntityIndices of the filteredArchetype
                            var previous = CacheData.AddArchetypes[indexInAddArchetypes];
                            previous.PackedEntityIndices.Add(packedEntityIndex);
                            previous.EntityCount++;

                            // Overwrite the previous entry for this filteredArchetype with the new one
                            CacheData.AddArchetypes[indexInAddArchetypes] = previous;
                        }
                        else
                        {
                            // Add all components to the archetype in typeIndex form, to allow (de)serialization
                            UnsafeList<TypeIndex> typeSet =
                                new UnsafeList<TypeIndex>(afterTypesCount - 1, Allocator.TempJob);
                            for (var indexInTypeArray = 1; indexInTypeArray < afterTypesCount; indexInTypeArray++)
                            {
                                var afterTypeInArchetype = afterArchetype->Types[indexInTypeArray];

                                if (afterTypeInArchetype.IsCleanupComponent || afterTypeInArchetype.IsBakeOnlyType || afterTypeInArchetype.IsChunkComponent)
                                    continue;

                                ValidateTypeForSerialization(afterTypeInArchetype, entityGuid);

                                typeSet.Add(afterTypeInArchetype.TypeIndex);
                            }

                            // Add both to the CacheData for the EntityPatcher and to the local AddedArchetypes
                            // for checking against future entities being processed
                            AddedArchetypes.Add(archetypeHash, CacheData.AddArchetypes.Length);
                            CacheData.AddArchetypes.Add(new FilteredArchetype()
                            {
                                EntityCount = 1,
                                TypeIndices = typeSet,
                                PackedEntityIndices = new UnsafeList<int>(16, Allocator.TempJob){packedEntityIndex},
                            });
                        }

                        for (var afterIndexInTypeArray = 1; afterIndexInTypeArray < afterTypesCount; afterIndexInTypeArray++)
                        {
                            var afterTypeInArchetype = afterArchetype->Types[afterIndexInTypeArray];

                            if (afterTypeInArchetype.IsCleanupComponent || afterTypeInArchetype.IsBakeOnlyType || afterTypeInArchetype.IsChunkComponent)
                                continue;

                            // This handles special component types that need additional/special adding
                            // (managed components). It doesn't add normal components that are covered in the archetype
                            AddComponentData(
                                afterChunk,
                                afterArchetype,
                                afterTypeInArchetype,
                                afterIndexInTypeArray,
                                afterEntity.IndexInChunk,
                                entityGuid, 0, i, false);
                        }
                    }
                    AddedArchetypes.Clear();

                    // handle the case where we have few entites to process and we need to fall into the other
                    // loop in case we need to
                    amountToCompute = (index + ComponentChangesBatchCount) - maxCreatedCount;
                    if (amountToCompute <= 0)
                    {
                        return;
                    }

                    index = 0;
                }
                else
                {
                    index -= createdCount;
                }

                // Offset as we are handling modifications here
                // We might need to process less than ComponentChangesBatchCount if we are part of a Created Batch
                int maxCount = math.min(index + amountToCompute, ReadData.ModifiedEntities.Length);

                for (var i = index; i < maxCount; ++i)
                {
                    var modification = ReadData.ModifiedEntities[i];
                    var entityGuid = modification.EntityGuid;

                    var afterEntity = modification.AfterEntityInChunk;
                    var afterChunk = afterEntity.Chunk;
                    var afterArchetype = AfterEntityComponentStore->GetArchetype(afterChunk);
                    var afterTypesCount = afterArchetype->TypesCount;

                    var beforeEntity = modification.BeforeEntityInChunk;
                    var beforeChunk = beforeEntity.Chunk;
                    var beforeArchetype = BeforeEntityComponentStore->GetArchetype(beforeChunk);
                    var beforeTypesCount = beforeArchetype->TypesCount;

                    for (var afterIndexInTypeArray = 1; afterIndexInTypeArray < afterTypesCount; afterIndexInTypeArray++)
                    {
                        var afterTypeInArchetype = afterArchetype->Types[afterIndexInTypeArray];

                        if (afterTypeInArchetype.IsCleanupComponent || afterTypeInArchetype.IsBakeOnlyType || afterTypeInArchetype.IsChunkComponent)
                        {
                            continue;
                        }

                        var typeIndex = afterTypeInArchetype.TypeIndex;
                        var beforeIndexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(beforeArchetype, typeIndex);

                        // This type is missing in the before entity.
                        // This means we are dealing with a newly added component.
                        if (-1 == beforeIndexInTypeArray)
                        {
                            ValidateTypeForSerialization(afterTypeInArchetype, entityGuid);

                            // This type does not exist on the before world. This was a newly added component.
                            AddComponentData(
                                afterChunk,
                                afterArchetype,
                                afterTypeInArchetype,
                                afterIndexInTypeArray,

                                afterEntity.IndexInChunk,
                                entityGuid, 1, i,
                                true
                            );

                            continue;
                        }

                        if (!afterTypeInArchetype.IsManagedComponent && modification.CanCompareChunkVersions)
                        {
                            var afterVersion = afterArchetype->Chunks.GetChangeVersion(afterIndexInTypeArray, afterChunk.ListIndex);
                            var beforeVersion = beforeArchetype->Chunks.GetChangeVersion(beforeIndexInTypeArray, beforeChunk.ListIndex);
                            if (afterVersion == beforeVersion)
                            {
                                continue;
                            }
                        }

                        SetComponentData(
                            afterChunk,
                            afterArchetype,
                            afterTypeInArchetype,
                            afterIndexInTypeArray,
                            afterEntity.IndexInChunk,
                            beforeChunk,
                            beforeArchetype,
                            beforeIndexInTypeArray,
                            beforeEntity.IndexInChunk,
                            entityGuid,
                            i);
                    }

                   for (var beforeTypeIndexInArchetype = 1; beforeTypeIndexInArchetype < beforeTypesCount; beforeTypeIndexInArchetype++)
                    {
                        var beforeComponentTypeInArchetype = beforeArchetype->Types[beforeTypeIndexInArchetype];

                        if (beforeComponentTypeInArchetype.IsCleanupComponent || beforeComponentTypeInArchetype.IsBakeOnlyType || beforeComponentTypeInArchetype.IsChunkComponent)
                        {
                            continue;
                        }

                        var beforeTypeIndex = beforeComponentTypeInArchetype.TypeIndex;

                        if (-1 == ChunkDataUtility.GetIndexInTypeArray(afterArchetype, beforeTypeIndex))
                        {
                            var packedComponent = PackComponent(entityGuid, beforeTypeIndex, 1, i);
                            CacheData.RemoveComponents.Add(packedComponent);
                        }
                    }
                }
            }

            void AddComponentData(
                ChunkIndex afterChunk,
                Archetype* afterArchetype,
                ComponentTypeInArchetype afterTypeInArchetype,
                int afterIndexInTypeArray,
                int afterEntityIndexInChunk,
                EntityGuid entityGuid,
                int tableHint,
                int entryHint,
                bool addComponent)
            {
                var packedComponent = PackComponent(entityGuid, afterTypeInArchetype.TypeIndex, tableHint, entryHint);
                var chunkBuffer = afterChunk.Buffer;

                if (addComponent)
                {
                    CacheData.AddComponents.Add(packedComponent);
                }

                if (afterTypeInArchetype.IsSharedComponent)
                {
                    var offset = afterIndexInTypeArray - AfterEntityComponentStore->GetArchetype(afterChunk)->FirstSharedComponent;
                    var sharedComponentIndex = afterArchetype->Chunks.GetSharedComponentValue(offset, afterChunk.ListIndex);

                    // No managed objects in burst land. Do what we can a defer the actual unpacking until later.
                    AddendSharedComponentData(entityGuid, afterTypeInArchetype.TypeIndex, sharedComponentIndex);
                    return;
                }

                if (afterTypeInArchetype.IsManagedComponent)
                {
                    var afterManagedComponentIndex  = ((int*)(chunkBuffer + afterArchetype->Offsets[afterIndexInTypeArray]))[afterEntityIndexInChunk];
                    AppendManagedComponentData(entityGuid, afterTypeInArchetype.TypeIndex, afterManagedComponentIndex);
                    return;
                }

                int isEnabled = -1;
                if (afterTypeInArchetype.IsEnableable)
                {
                    var isComponentEnabled = ChunkDataUtility.GetEnabledRefRO(afterChunk, afterArchetype, afterIndexInTypeArray);
                    // Default value of an enableable component is true, so we only need to process new components that are false
                    isEnabled = isComponentEnabled.IsSet(afterEntityIndexInChunk) ? -1 : 0;
                }

                // IMPORTANT This means `IsZeroSizedInChunk` which is always true for shared components.
                // Always check shared components first.
                if (afterTypeInArchetype.IsZeroSized)
                {
                    if (isEnabled >= 0)
                    {
                        AppendEmptyComponentData(packedComponent, isEnabled);
                    }

                    return;
                }

                if (afterTypeInArchetype.IsBuffer)
                {
                    var sizeOf = afterArchetype->SizeOfs[afterIndexInTypeArray];
                    var buffer = (BufferHeader*)(chunkBuffer + afterArchetype->Offsets[afterIndexInTypeArray] + afterEntityIndexInChunk * sizeOf);
                    var length = buffer->Length;

                    if (length == 0)
                    {
                        if (isEnabled >= 0)
                        {
                            AppendEmptyComponentData(packedComponent, isEnabled);
                        }
                        return;
                    }

                    var elementPtr = BufferHeader.GetElementPointer(buffer);

                    if (afterTypeInArchetype.TypeIndex == ReadData.LinkedEntityGroupTypeIndex)
                    {
                        for (var elementIndex = 0; elementIndex < length; elementIndex++)
                        {
                            var childEntity = ((Entity*)elementPtr)[elementIndex];
                            var childEntityGuid = GetEntityGuid(ReadData.AfterEntityComponentStore, ReadData.EntityGuidTypeIndex, childEntity);

                            CacheData.LinkedEntityGroupAdditions.Add(new LinkedEntityGroupChange
                            {
                                RootEntityGuid = entityGuid,
                                ChildEntityGuid = childEntityGuid
                            });
                        }
                    }
                    else
                    {
                        var typeInfo = &ReadData.TypeInfo[afterTypeInArchetype.TypeIndex.Index];
                        AppendComponentData(packedComponent, elementPtr, typeInfo->ElementSize * length, isEnabled);
                        ExtractPatches(typeInfo, packedComponent, elementPtr, length);
                    }
                }
                else
                {
                    var typeInfo = &ReadData.TypeInfo[afterTypeInArchetype.TypeIndex.Index];
                    var sizeOf = afterArchetype->SizeOfs[afterIndexInTypeArray];
                    var ptr = chunkBuffer + afterArchetype->Offsets[afterIndexInTypeArray] + afterEntityIndexInChunk * sizeOf;
                    AppendComponentData(packedComponent, ptr, sizeOf, isEnabled);
                    ExtractPatches(typeInfo, packedComponent, ptr, 1);
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            private static void AssertArchetypeSizeOfsMatch(Archetype* beforeArchetype, int beforeIndexInTypeArray, Archetype* afterArchetype, int afterIndexInTypeArray)
            {
                if (beforeArchetype->SizeOfs[beforeIndexInTypeArray] != afterArchetype->SizeOfs[afterIndexInTypeArray])
                    throw new Exception("Archetype->SizeOfs do not match");
            }

            void SetComponentData(
                ChunkIndex afterChunk,
                Archetype* afterArchetype,
                ComponentTypeInArchetype afterTypeInArchetype,
                int afterIndexInTypeArray,
                int afterEntityIndexInChunk,
                ChunkIndex beforeChunk,
                Archetype* beforeArchetype,
                int beforeIndexInTypeArray,
                int beforeEntityIndexInChunk,
                EntityGuid entityGuid,
                int entryHint)
            {
                var afterChunkBuffer = afterChunk.Buffer;
                var beforeChunkBuffer = beforeChunk.Buffer;

                if (afterTypeInArchetype.IsSharedComponent)
                {
                    var beforeOffset = beforeIndexInTypeArray - BeforeEntityComponentStore->GetArchetype(beforeChunk)->FirstSharedComponent;
                    var beforeSharedComponentIndex = beforeArchetype->Chunks.GetSharedComponentValue(beforeOffset, beforeChunk.ListIndex);

                    var afterOffset = afterIndexInTypeArray - AfterEntityComponentStore->GetArchetype(afterChunk)->FirstSharedComponent;
                    var afterSharedComponentIndex = afterArchetype->Chunks.GetSharedComponentValue(afterOffset, afterChunk.ListIndex);

                    // No managed objects in burst land. Do what we can and defer the actual unpacking until later.
                    AddendSharedComponentData(entityGuid, afterTypeInArchetype.TypeIndex, afterSharedComponentIndex, beforeSharedComponentIndex);
                    return;
                }

                if (afterTypeInArchetype.IsManagedComponent)
                {
                    var afterManagedComponentIndex  = ((int*)(afterChunkBuffer + afterArchetype->Offsets[afterIndexInTypeArray]))[afterEntityIndexInChunk];
                    var beforeManagedComponentIndex  = ((int*)(beforeChunkBuffer + beforeArchetype->Offsets[beforeIndexInTypeArray]))[beforeEntityIndexInChunk];

                    AppendManagedComponentData(entityGuid, afterTypeInArchetype.TypeIndex, afterManagedComponentIndex, beforeManagedComponentIndex);
                    return;
                }

                int isEnabledAfter = -1;
                if (afterTypeInArchetype.IsEnableable)
                {
                    AreEnableableComponentsEqual(afterChunk, afterArchetype, afterIndexInTypeArray, afterEntityIndexInChunk,
                        beforeChunk, beforeArchetype, beforeIndexInTypeArray, beforeEntityIndexInChunk, out isEnabledAfter);
                }

                // IMPORTANT This means `IsZeroSizedInChunk` which is always true for shared components.
                // Always check shared components first.
                if (afterTypeInArchetype.IsZeroSized)
                {
                    if (isEnabledAfter >= 0)
                    {
                        var packedComponent = PackComponent(entityGuid, afterTypeInArchetype.TypeIndex, 1, entryHint);
                        AppendEmptyComponentData(packedComponent, isEnabledAfter);
                    }
                    return;
                }

                if (afterTypeInArchetype.IsBuffer)
                {
                    var beforeBuffer = (BufferHeader*)(beforeChunkBuffer
                        + beforeArchetype->Offsets[beforeIndexInTypeArray]
                        + beforeEntityIndexInChunk
                        * beforeArchetype->SizeOfs[beforeIndexInTypeArray]);

                    var beforeElementPtr = BufferHeader.GetElementPointer(beforeBuffer);
                    var beforeLength = beforeBuffer->Length;

                    var afterBuffer = (BufferHeader*)(afterChunkBuffer
                        + afterArchetype->Offsets[afterIndexInTypeArray]
                        + afterEntityIndexInChunk
                        * afterArchetype->SizeOfs[afterIndexInTypeArray]);

                    var afterElementPtr = BufferHeader.GetElementPointer(afterBuffer);
                    var afterLength = afterBuffer->Length;

                    if (afterTypeInArchetype.TypeIndex == ReadData.LinkedEntityGroupTypeIndex)
                    {
                        var beforeLinkedEntityGroups = (LinkedEntityGroup*)beforeElementPtr;
                        var afterLinkedEntityGroups = (LinkedEntityGroup*)afterElementPtr;

                        // Using is not supported by burst.
                        var beforeLinkedEntityGroupEntityGuids = new NativeArray<EntityGuid>(beforeLength, Allocator.Temp);
                        var afterLinkedEntityGroupEntityGuids = new NativeArray<EntityGuid>(afterLength, Allocator.Temp);
                        {
                            for (var i = 0; i < beforeLength; i++)
                            {
                                var beforeEntityGuid = GetEntityGuid(ReadData.BeforeEntityComponentStore, ReadData.EntityGuidTypeIndex, beforeLinkedEntityGroups[i].Value);
                                beforeLinkedEntityGroupEntityGuids[i] = beforeEntityGuid;
                            }

                            for (var i = 0; i < afterLength; i++)
                            {
                                var afterEntityGuid = GetEntityGuid(ReadData.AfterEntityComponentStore, ReadData.EntityGuidTypeIndex, afterLinkedEntityGroups[i].Value);
                                afterLinkedEntityGroupEntityGuids[i] = afterEntityGuid;
                            }

                            GatherLinkedEntityGroupChanges(entityGuid,
                                beforeLinkedEntityGroupEntityGuids, afterLinkedEntityGroupEntityGuids,
                                ref CacheData.LinkedEntityGroupAdditions, ref CacheData.LinkedEntityGroupRemovals);
                        }
                    }
                    else
                    {
                        var typeInfo = &ReadData.TypeInfo[afterTypeInArchetype.TypeIndex.Index];

                        // If the value, or the enableable value is changed
                        if (afterLength != beforeLength || !AreComponentsEqual(typeInfo, beforeElementPtr, afterElementPtr, afterLength) || isEnabledAfter >= 0)
                        {
                            var packedComponent = PackComponent(entityGuid, afterTypeInArchetype.TypeIndex, 1, entryHint);
                            AppendComponentData(packedComponent, afterElementPtr, typeInfo->ElementSize * afterLength, isEnabledAfter);
                            ExtractPatches(typeInfo, packedComponent, afterElementPtr, afterLength);
                        }
                    }
                }
                else
                {
                    AssertArchetypeSizeOfsMatch(beforeArchetype, beforeIndexInTypeArray, afterArchetype,
                        afterIndexInTypeArray);

                    var beforeAddress = beforeChunkBuffer
                                        + beforeArchetype->Offsets[beforeIndexInTypeArray]
                                        + beforeArchetype->SizeOfs[beforeIndexInTypeArray]
                                        * beforeEntityIndexInChunk;

                    var afterAddress = afterChunkBuffer
                                       + afterArchetype->Offsets[afterIndexInTypeArray]
                                       + afterArchetype->SizeOfs[afterIndexInTypeArray]
                                       * afterEntityIndexInChunk;

                    var typeInfo = &ReadData.TypeInfo[afterTypeInArchetype.TypeIndex.Index];

                    // If the value, or the enableable value is changed
                    if (!AreComponentsEqual(typeInfo, beforeAddress, afterAddress, 1) || isEnabledAfter >= 0)
                    {
                        var packedComponent = PackComponent(entityGuid, afterTypeInArchetype.TypeIndex, 1, entryHint);
                        ExtractPatches(typeInfo, packedComponent, afterAddress, 1);
                        AppendComponentData(packedComponent, afterAddress,
                            beforeArchetype->SizeOfs[beforeIndexInTypeArray], isEnabledAfter);
                    }
                }
            }



            void ValidateTypeForSerialization(ComponentTypeInArchetype afterTypeInArchetype, EntityGuid entityGuid)
            {
                // Make sure that the component is serializable
                if (!afterTypeInArchetype.IsChunkSerializable)
                {
                    bool sharedComponent = afterTypeInArchetype.IsSharedComponent && afterTypeInArchetype.TypeIndex.HasEntityReferences;

                    CacheData.NonSerializableComponents.Add(new NonSerializableDebugInfo()
                    {
                        OriginatingId = entityGuid.OriginatingId,
                        OriginatingSubId = entityGuid.OriginatingSubId,
                        TypeIndex = afterTypeInArchetype.TypeIndex,
                        SharedComponent = sharedComponent ? 1 : 0
                    });

                }
            }

            static bool ShouldSkip(int offset, ref int nextOffsetIndex, int offsetCount, TypeManager.EntityOffsetInfo* offsets)
            {
                while (nextOffsetIndex < offsetCount)
                {
                    int cmp = offset - offsets[nextOffsetIndex].Offset;
                    if (cmp < 0)
                        return false;
                    if (cmp == 0)
                        return true;
                    nextOffsetIndex++;
                }

                return false;
            }


            bool AreEnableableComponentsEqual(ChunkIndex afterChunk, Archetype* afterArchetype, int afterIndexInTypeArray, int afterEntityIndexInChunk,
                ChunkIndex beforeChunk, Archetype* beforeArchetype, int beforeIndexInTypeArray, int beforeEntityIndexInChunk, out int enabled)
            {
                var isComponentEnabledAfter = ChunkDataUtility.GetEnabledRefRO(afterChunk, afterArchetype, afterIndexInTypeArray);
                bool isEnabledAfter = isComponentEnabledAfter.IsSet(afterEntityIndexInChunk);

                var isComponentEnabledBefore = ChunkDataUtility.GetEnabledRefRO(beforeChunk, beforeArchetype, beforeIndexInTypeArray);
                bool isEnabledBefore = isComponentEnabledBefore.IsSet(beforeEntityIndexInChunk);

                bool equal = isEnabledAfter == isEnabledBefore;
                enabled = equal ? -1 : isEnabledAfter ? 1 : 0;
                return equal;
            }


            bool AreComponentsEqual(TypeManager.TypeInfo* typeInfo, byte* beforeAddress, byte* afterAddress, int elementCount)
            {
                int elementSize = typeInfo->ElementSize;
                if (ReadData.UseReferentialEquivalence == 0 || typeInfo->EntityOffsetCount == 0 && typeInfo->BlobAssetRefOffsetCount == 0)
                {
                    return UnsafeUtility.MemCmp(beforeAddress, afterAddress, elementCount * elementSize) == 0
                           && !BlobAssetHashesAreDifferent(typeInfo, beforeAddress, afterAddress, elementCount);
                }

                // otherwise do deep comparison
                if (BlobAssetHashesAreDifferent(typeInfo, beforeAddress, afterAddress, elementCount))
                    return false;
                if (EntityGuidsAreDifferent(typeInfo, beforeAddress, afterAddress, elementCount))
                    return false;

                var blobAssetOffsets = ReadData.BlobAssetRefOffsets + typeInfo->BlobAssetRefOffsetStartIndex;
                var entityOffsets = ReadData.EntityOffsets + typeInfo->EntityOffsetStartIndex;
                int blobAssetOffsetCount = typeInfo->BlobAssetRefOffsetCount;
                int entityOffsetCount = typeInfo->EntityOffsetCount;

                byte* mask = stackalloc byte[elementSize];
                {
                    int currentEntityOffset = 0;
                    int currentBlobAssetOffset = 0;
                    for (int offset = 0; offset < elementSize;)
                    {
                        if (ShouldSkip(offset, ref currentEntityOffset, entityOffsetCount, entityOffsets))
                        {
                            UnsafeUtility.MemSet(&mask[offset], 0, sizeof(Entity));
                            offset += sizeof(Entity);
                            continue;
                        }
                        if (ShouldSkip(offset, ref currentBlobAssetOffset, blobAssetOffsetCount, blobAssetOffsets))
                        {
                            UnsafeUtility.MemSet(&mask[offset], 0, sizeof(BlobAssetReferenceData));
                            offset += sizeof(BlobAssetReferenceData);
                            continue;
                        }

                        mask[offset] = 0xFF;
                        offset += 1;
                    }
                }

                for (var elementIndex = 0; elementIndex < elementCount; ++elementIndex)
                {
                    for (int offset = 0; offset < elementSize; )
                    {
                        if (mask[offset] == 0 || beforeAddress[offset] == afterAddress[offset])
                        {
                            offset++;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            /// <summary>
            /// IMPORTANT. This function does *NO* validation. It is assumed to be called after a memcmp == 0
            /// </summary>
            bool BlobAssetHashesAreDifferent(
                TypeManager.TypeInfo* typeInfo,
                byte* beforeAddress,
                byte* afterAddress,
                int elementCount)
            {
                if (typeInfo->BlobAssetRefOffsetCount == 0)
                    return false;

                var offsets = ReadData.BlobAssetRefOffsets + typeInfo->BlobAssetRefOffsetStartIndex;

                var elementOffset = 0;
                int elementSize = typeInfo->ElementSize;
                int blobAssetRefOffsetCount = typeInfo->BlobAssetRefOffsetCount;
                for (var elementIndex = 0; elementIndex < elementCount; ++elementIndex)
                {
                    for (var offsetIndex = 0; offsetIndex < blobAssetRefOffsetCount; ++offsetIndex)
                    {
                        var offset = elementOffset + offsets[offsetIndex].Offset;

                        var beforeBlobAssetReference = (BlobAssetReferenceData*)(beforeAddress + offset);
                        var afterBlobAssetReference = (BlobAssetReferenceData*)(afterAddress + offset);

                        if (GetBlobAssetHash(ReadData.BeforeBlobAssetRemap, beforeBlobAssetReference) != GetBlobAssetHash(ReadData.AfterBlobAssetRemap, afterBlobAssetReference))
                            return true;
                    }

                    elementOffset += elementSize;
                }

                return false;
            }

            bool EntityGuidsAreDifferent(
                TypeManager.TypeInfo* typeInfo,
                byte* beforeAddress,
                byte* afterAddress,
                int elementCount)
            {
                if (typeInfo->EntityOffsetCount == 0)
                    return false;

                var offsets = ReadData.EntityOffsets + typeInfo->EntityOffsetStartIndex;

                var elementOffset = 0;
                int elementSize = typeInfo->ElementSize;
                int entityOffsetCount = typeInfo->EntityOffsetCount;
                for (var elementIndex = 0; elementIndex < elementCount; ++elementIndex)
                {
                    for (var offsetIndex = 0; offsetIndex < entityOffsetCount; ++offsetIndex)
                    {
                        var offset = elementOffset + offsets[offsetIndex].Offset;

                        var afterEntity = *(Entity*) (afterAddress + offset);
                        var beforeEntity = *(Entity*) (beforeAddress + offset);

                        // If the entity has no guid, then guid will be null (desired)
                        TryGetEntityGuidComponent(ReadData.BeforeEntityComponentStore, beforeEntity, ReadData.EntityGuidTypeIndex, out var beforeGuid);
                        TryGetEntityGuidComponent(ReadData.AfterEntityComponentStore, afterEntity, ReadData.EntityGuidTypeIndex, out var afterGuid);
                        if (!beforeGuid.Equals(afterGuid))
                            return true;
                    }

                    elementOffset += elementSize;
                }

                return false;
            }

            void ExtractPatches(
                TypeManager.TypeInfo* typeInfo,
                PackedComponent component,
                byte* afterAddress,
                int elementCount)
            {
                ExtractEntityReferencePatches(typeInfo, component, afterAddress, elementCount);
                ExtractBlobAssetReferencePatches(typeInfo, component, afterAddress, elementCount);
            }

            void ExtractEntityReferencePatches(
                TypeManager.TypeInfo* typeInfo,
                PackedComponent component,
                byte* afterAddress,
                int elementCount)
            {
                if (typeInfo->EntityOffsetCount == 0)
                {
                    return;
                }

                var offsets = ReadData.EntityOffsets + typeInfo->EntityOffsetStartIndex;

                var elementOffset = 0;
                int elementSize = typeInfo->ElementSize;
                int entityOffsetCount = typeInfo->EntityOffsetCount;
                for (var elementIndex = 0; elementIndex < elementCount; ++elementIndex)
                {
                    for (var offsetIndex = 0; offsetIndex < entityOffsetCount; ++offsetIndex)
                    {
                        var offset = elementOffset + offsets[offsetIndex].Offset;
                        var entity = *(Entity*)(afterAddress + offset);

                        // If the entity has no guid, then guid will be null (desired)
                        TryGetEntityGuidComponent(ReadData.AfterEntityComponentStore, entity, ReadData.EntityGuidTypeIndex, out var entityGuid);

                        CacheData.EntityReferencePatches.Add(new EntityReferenceChange
                        {
                            Component = component,
                            Offset = offset,
                            Value = entityGuid
                        });
                    }

                    elementOffset += elementSize;
                }
            }

            void ExtractBlobAssetReferencePatches(
                TypeManager.TypeInfo* typeInfo,
                PackedComponent component,
                byte* afterAddress,
                int elementCount)
            {
                if (typeInfo->BlobAssetRefOffsetCount == 0)
                {
                    return;
                }

                var offsets = ReadData.BlobAssetRefOffsets + typeInfo->BlobAssetRefOffsetStartIndex;

                var elementOffset = 0;
                int elementSize = typeInfo->ElementSize;
                int blobAssetRefOffsetCount = typeInfo->BlobAssetRefOffsetCount;
                for (var elementIndex = 0; elementIndex < elementCount; ++elementIndex)
                {
                    for (var offsetIndex = 0; offsetIndex < blobAssetRefOffsetCount; ++offsetIndex)
                    {
                        var offset = elementOffset + offsets[offsetIndex].Offset;
                        var blobAssetReference = (BlobAssetReferenceData*)(afterAddress + offset);
                        var hash = GetBlobAssetHash(ReadData.AfterBlobAssetRemap, blobAssetReference);

                        CacheData.BlobAssetReferenceChanges.Add(new BlobAssetReferenceChange
                        {
                            Component = component,
                            Offset = offset,
                            Value = hash
                        });
                    }

                    elementOffset += elementSize;
                }
            }

            static ulong GetBlobAssetHash(NativeParallelHashMap<BlobAssetPtr, BlobAssetPtr> remap, BlobAssetReferenceData* blobAssetReferenceData)
            {
                if (blobAssetReferenceData->m_Ptr == null)
                    return 0;

                if (remap.IsCreated && remap.TryGetValue(new BlobAssetPtr(((BlobAssetHeader*)blobAssetReferenceData->m_Ptr) - 1), out var header))
                    return header.Hash;

                return blobAssetReferenceData->Header->Hash;
            }

            void AppendComponentData(PackedComponent component, void* ptr, int sizeOf)
            {
                CacheData.SetComponents.Add(new PackedComponentDataChange
                {
                    Component = component,
                    Offset = 0,
                    Size = sizeOf,
                    Enabled = -1
                });

                CacheData.ComponentData.AddRange(ptr, sizeOf);
            }

            void AppendComponentData(PackedComponent component, void* ptr, int sizeOf, int enabled)
            {
                CacheData.SetComponents.Add(new PackedComponentDataChange
                {
                    Component = component,
                    Offset = 0,
                    Size = sizeOf,
                    Enabled = enabled
                });

                CacheData.ComponentData.AddRange(ptr, sizeOf);
            }

            void AppendEmptyComponentData(PackedComponent component, int enabled)
            {
                CacheData.SetComponents.Add(new PackedComponentDataChange
                {
                    Component = component,
                    Offset = 0,
                    Size = 0,
                    Enabled = enabled
                });

            }

            void AddendSharedComponentData(EntityGuid entityGuid, TypeIndex typeIndex, int afterSharedComponentIndex, int beforeSharedComponentIndex = -1)
            {
                CacheData.SharedComponentChanges.Add(new DeferredSharedComponentChange
                {
                    EntityGuid = entityGuid,
                    TypeIndex = typeIndex,
                    AfterSharedComponentIndex = afterSharedComponentIndex,
                    BeforeSharedComponentIndex = beforeSharedComponentIndex
                });
            }

            void AppendManagedComponentData(EntityGuid entityGuid, TypeIndex typeIndex, int afterManagedComponentIndex, int beforeManagedComponentIndex = -1)
            {
                CacheData.ManagedComponentChanges.Add(new DeferredManagedComponentChange
                {
                    EntityGuid = entityGuid,
                    TypeIndex = typeIndex,
                    AfterManagedComponentIndex = afterManagedComponentIndex,
                    BeforeManagedComponentIndex = beforeManagedComponentIndex
                });
            }
            void AppendEnableableComponentData(PackedComponent component, void* ptr, int sizeOf)
            {

                CacheData.SetComponents.Add(new PackedComponentDataChange
                {
                    Component = component,
                    Offset = 0,
                    Size = sizeOf
                });

                CacheData.ComponentData.AddRange(ptr, sizeOf);
            }


            PackedComponent PackComponent(EntityGuid entityGuid, TypeIndex typeIndex, int tableHint, int entryHint)
            {
                var flags = ComponentTypeFlags.None;

                if (typeIndex.IsChunkComponent)
                    flags |= ComponentTypeFlags.ChunkComponent;

                ulong stableTypeHash = ReadData.TypeInfo[typeIndex.Index].StableTypeHash;

                var packedEntityIndex = ReadData.Entities.Get(entityGuid, tableHint, entryHint);
                var packedTypeIndex = ReadData.ComponentTypes.Get(new ComponentTypeHash
                {
                    StableTypeHash = stableTypeHash,
                    Flags = flags
                });

                if (packedTypeIndex == -1)
                {
                    throw new Exception ($"ComponentTypes: Unable to find {stableTypeHash}:{flags}");
                }

                return new PackedComponent
                {
                    PackedEntityIndex = packedEntityIndex,
                    PackedTypeIndex = packedTypeIndex
                };
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            private void ThrowOnEntityGuidMissing()
            {
                throw new Exception("LinkedEntityGroup child is missing an EntityGuid component.");
            }

            EntityGuid GetEntityGuid(EntityComponentStore* entityComponentStore, TypeIndex entityGuidTypeIndex, Entity entity)
            {
                if (!TryGetEntityGuidComponent(entityComponentStore, entity, entityGuidTypeIndex, out var result))
                {
                    ThrowOnEntityGuidMissing();
                }

                return result;
            }

            private void WriteArray<T>(NativeList<T> dest, ref NativeList<T> src) where T : unmanaged
            {
                if (src.IsEmpty)
                    return;

                dest.AddRange(src.AsArray());
                src.Clear();
            }

            private void WriteArrayDeepCopy(NativeList<FilteredArchetype> dest, ref NativeList<FilteredArchetype> src, ref int currentIndex)
            {
                if (src.IsEmpty)
                    return;

                for (int srcIndex = 0; srcIndex < src.Length; srcIndex++)
                {
                    int destIndex = currentIndex + srcIndex;
                    ref var dst = ref dest.ElementAt(destIndex);

                    dst.EntityCount += src[srcIndex].EntityCount;
                    dst.TypeIndices.AddRange(src[srcIndex].TypeIndices);
                    dst.PackedEntityIndices.AddRange(src[srcIndex].PackedEntityIndices);

                    src[srcIndex].TypeIndices.Dispose();
                    src[srcIndex].PackedEntityIndices.Dispose();
                }

                currentIndex += src.Length;
                src.Clear();
            }

            public void WriteToOutputData(ref int archteypeCount)
            {
                WriteArray(OutputData.AddComponents, ref CacheData.AddComponents);
                WriteArrayDeepCopy(OutputData.AddArchetypes, ref CacheData.AddArchetypes, ref archteypeCount);
                WriteArray(OutputData.SetComponents, ref CacheData.SetComponents);
                WriteArray(OutputData.RemoveComponents, ref CacheData.RemoveComponents);
                WriteArray(OutputData.EntityReferencePatches, ref CacheData.EntityReferencePatches);
                WriteArray(OutputData.BlobAssetReferenceChanges, ref CacheData.BlobAssetReferenceChanges);
                WriteArray(OutputData.LinkedEntityGroupAdditions, ref CacheData.LinkedEntityGroupAdditions);
                WriteArray(OutputData.LinkedEntityGroupRemovals, ref CacheData.LinkedEntityGroupRemovals);
                WriteArray(OutputData.ComponentData, ref CacheData.ComponentData);
                WriteArray(OutputData.SharedComponentChanges, ref CacheData.SharedComponentChanges);
                WriteArray(OutputData.ManagedComponentChanges, ref CacheData.ManagedComponentChanges);
                WriteArray(OutputData.NonSerializableComponents, ref CacheData.NonSerializableComponents);
            }
        }

        struct NonSerializableDebugInfo
        {
            public int OriginatingId;
            public int OriginatingSubId;
            public TypeIndex TypeIndex;
            public int SharedComponent;
        }

        struct GatherComponentChangesOutput
        {
            [WriteOnly] public NativeList<PackedComponent> AddComponents;
            [WriteOnly] public NativeList<FilteredArchetype> AddArchetypes;
            [WriteOnly] public NativeList<PackedComponentDataChange> SetComponents;
            [WriteOnly] public NativeList<PackedComponent> RemoveComponents;
            [WriteOnly] public NativeList<EntityReferenceChange> EntityReferencePatches;
            [WriteOnly] public NativeList<BlobAssetReferenceChange> BlobAssetReferenceChanges;
            [WriteOnly] public NativeList<LinkedEntityGroupChange> LinkedEntityGroupAdditions;
            [WriteOnly] public NativeList<LinkedEntityGroupChange> LinkedEntityGroupRemovals;
            [WriteOnly] public NativeList<byte> ComponentData;
            [WriteOnly] public NativeList<DeferredSharedComponentChange> SharedComponentChanges;
            [WriteOnly] public NativeList<DeferredManagedComponentChange> ManagedComponentChanges;
            [WriteOnly] public NativeList<NonSerializableDebugInfo> NonSerializableComponents;
        }

        struct GatherComponentChangesReadOnlyData
        {
            // If these two variables are marked as [ReadOnly] they may endup being set to zero instead of correct value.
            public TypeIndex EntityGuidTypeIndex;
            public TypeIndex LinkedEntityGroupTypeIndex;

            [ReadOnly] [NativeDisableUnsafePtrRestriction] public TypeManager.TypeInfo* TypeInfo;
            [ReadOnly] [NativeDisableUnsafePtrRestriction] public TypeManager.EntityOffsetInfo* EntityOffsets;
            [ReadOnly] [NativeDisableUnsafePtrRestriction] public TypeManager.EntityOffsetInfo* BlobAssetRefOffsets;
            [ReadOnly] [NativeDisableUnsafePtrRestriction] public EntityComponentStore* AfterEntityComponentStore;
            [ReadOnly] [NativeDisableUnsafePtrRestriction] public EntityComponentStore* BeforeEntityComponentStore;

            [ReadOnly] public NativeList<CreatedEntity> CreatedEntities;
            [ReadOnly] public NativeList<ModifiedEntity> ModifiedEntities;

            [ReadOnly] public PackedEntityGuidsCollection Entities;
            [ReadOnly] public PackedCollection<ComponentTypeHash> ComponentTypes;

            [ReadOnly] [NativeDisableContainerSafetyRestriction] public NativeParallelHashMap<BlobAssetPtr, BlobAssetPtr> AfterBlobAssetRemap;
            [ReadOnly] [NativeDisableContainerSafetyRestriction] public NativeParallelHashMap<BlobAssetPtr, BlobAssetPtr> BeforeBlobAssetRemap;

            /// <summary>
            /// If set, components are not compared bit-wise. Bit-wise comparison implies that two components that
            /// have references to entities that have the same GUID but different indices/versions are different.
            /// Similarly blob asset references to blob assets that have the same hash but live at different addresses
            /// will be considered different as well. This is often not desirable. For these cases, it is more apt to
            /// check that GUIDs and hashes match.
            /// </summary>
            [ReadOnly] public byte UseReferentialEquivalence;
        }

        struct GatherComponentChangesWriteOnlyData
        {
            [WriteOnly] public NativeList<PackedComponent> AddComponents;
            [WriteOnly] public NativeList<FilteredArchetype> AddArchetypes;
            [WriteOnly] public NativeList<PackedComponentDataChange> SetComponents;
            [WriteOnly] public NativeList<PackedComponent> RemoveComponents;
            [WriteOnly] public NativeList<EntityReferenceChange> EntityReferencePatches;
            [WriteOnly] public NativeList<BlobAssetReferenceChange> BlobAssetReferenceChanges;
            [WriteOnly] public NativeList<LinkedEntityGroupChange> LinkedEntityGroupAdditions;
            [WriteOnly] public NativeList<LinkedEntityGroupChange> LinkedEntityGroupRemovals;
            [WriteOnly] public NativeList<byte> ComponentData;
            [WriteOnly] public NativeList<DeferredSharedComponentChange> SharedComponentChanges;
            [WriteOnly] public NativeList<DeferredManagedComponentChange> ManagedComponentChanges;
            [WriteOnly] public NativeList<NonSerializableDebugInfo> NonSerializableComponents;
        }

        [BurstCompile]
        struct GatherComponentChangesJob : IJob
        {
            public GatherComponentChangesReadOnlyData ReadData;
            public GatherComponentChangesOutput OutputData;
            [NativeDisableUnsafePtrRestriction] public EntityComponentStore* AfterEntityComponentStore;
            [NativeDisableUnsafePtrRestriction] public EntityComponentStore* BeforeEntityComponentStore;

            public void Execute()
            {
                var addComponentsSize = ComponentChangesBatchCount * 16;
                var addArchetypeSize = ComponentChangesBatchCount;
                var removeComponentsSize  = ComponentChangesBatchCount;
                var setComponentsSize  = ComponentChangesBatchCount * 8;
                var linkedEntityGroupAdditionsSize = ComponentChangesBatchCount;
                var linkedEntityGroupRemovalsSize = ComponentChangesBatchCount;
                var blobAssetReferenceChangesSize = ComponentChangesBatchCount;
                var entityReferencePatchesSize = ComponentChangesBatchCount;
                var componentDataSize = ComponentChangesBatchCount * 256;
                var sharedComponentChangesSize = ComponentChangesBatchCount * 16;
                var managedComponentChangesSize = ComponentChangesBatchCount;
                var nonSerializableComponentsSize = ComponentChangesBatchCount;

                var cacheData = new GatherComponentChangesWriteOnlyData
                {
                    AddComponents = new NativeList<PackedComponent>(addComponentsSize, Allocator.Temp),
                    AddArchetypes = new NativeList<FilteredArchetype>(addArchetypeSize, Allocator.Temp),
                    RemoveComponents = new NativeList<PackedComponent>(removeComponentsSize, Allocator.Temp),
                    SetComponents = new NativeList<PackedComponentDataChange>(setComponentsSize, Allocator.Temp),
                    LinkedEntityGroupAdditions = new NativeList<LinkedEntityGroupChange>(linkedEntityGroupAdditionsSize, Allocator.Temp),
                    LinkedEntityGroupRemovals = new NativeList<LinkedEntityGroupChange>(linkedEntityGroupRemovalsSize, Allocator.Temp),
                    BlobAssetReferenceChanges = new NativeList<BlobAssetReferenceChange>(blobAssetReferenceChangesSize, Allocator.Temp),
                    EntityReferencePatches = new NativeList<EntityReferenceChange>(entityReferencePatchesSize, Allocator.Temp),
                    ComponentData = new NativeList<byte>(componentDataSize, Allocator.Temp),
                    SharedComponentChanges = new NativeList<DeferredSharedComponentChange>(sharedComponentChangesSize, Allocator.Temp),
                    ManagedComponentChanges = new NativeList<DeferredManagedComponentChange>(managedComponentChangesSize, Allocator.Temp),
                    NonSerializableComponents = new NativeList<NonSerializableDebugInfo>(nonSerializableComponentsSize, Allocator.Temp)
                };
                var addedArchetypes = new NativeHashMap<ulong, int>(16, Allocator.Temp);

                var data = new GatherComponentChanges
                {
                    ReadData = ReadData,
                    CacheData = cacheData,
                    OutputData = OutputData,
                    AddedArchetypes = addedArchetypes,
                    AfterEntityComponentStore = AfterEntityComponentStore,
                    BeforeEntityComponentStore = BeforeEntityComponentStore,
                };

                int count = ReadData.CreatedEntities.Length + ReadData.ModifiedEntities.Length;
                int archteypeCount = 0;

                for (int i = 0; i < count; i += ComponentChangesBatchCount)
                {
                    data.ProcessRange(i);
                    data.WriteToOutputData(ref archteypeCount);
                }

                data.CacheData.ManagedComponentChanges.Dispose();
                data.CacheData.SharedComponentChanges.Dispose();
                data.CacheData.ComponentData.Dispose();
                data.CacheData.EntityReferencePatches.Dispose();
                data.CacheData.BlobAssetReferenceChanges.Dispose();
                data.CacheData.LinkedEntityGroupRemovals.Dispose();
                data.CacheData.LinkedEntityGroupAdditions.Dispose();
                data.CacheData.SetComponents.Dispose();
                data.CacheData.RemoveComponents.Dispose();
                data.CacheData.AddComponents.Dispose();
                foreach (var arch in data.CacheData.AddArchetypes)
                {
                    arch.TypeIndices.Dispose();
                    arch.PackedEntityIndices.Dispose();
                }
                data.CacheData.AddArchetypes.Dispose();
                data.CacheData.NonSerializableComponents.Dispose();
                addedArchetypes.Dispose();
            }
        }

        static readonly PackedSharedComponentDataChange[] s_EmptySetSharedComponentDiff = new PackedSharedComponentDataChange[0];
        static readonly PackedManagedComponentDataChange[] s_EmptySetManagedComponentDiff = new PackedManagedComponentDataChange[0];

        static GatherComponentChangesReadOnlyData InitGatherComponentChangesReadOnlyData(
            EntityInChunkChanges entityChanges,
            ComponentChanges componentChanges,
            bool useReferentialEquivalence,
            NativeParallelHashMap<BlobAssetPtr, BlobAssetPtr> afterBlobAssetRemap,
            NativeParallelHashMap<BlobAssetPtr, BlobAssetPtr> beforeBlobAssetRemap)
        {
            return new GatherComponentChangesReadOnlyData
            {
                EntityGuidTypeIndex = TypeManager.GetTypeIndex<EntityGuid>(),
                LinkedEntityGroupTypeIndex = TypeManager.GetTypeIndex<LinkedEntityGroup>(),
                TypeInfo = TypeManager.GetTypeInfoPointer(),
                EntityOffsets = TypeManager.GetEntityOffsetsPointer(),
                BlobAssetRefOffsets = TypeManager.GetBlobAssetRefOffsetsPointer(),
                AfterEntityComponentStore = entityChanges.AfterEntityManager.GetCheckedEntityDataAccess()->EntityComponentStore,
                BeforeEntityComponentStore = entityChanges.BeforeEntityManager.GetCheckedEntityDataAccess()->EntityComponentStore,
                CreatedEntities = entityChanges.CreatedEntities,
                ModifiedEntities = entityChanges.ModifiedEntities,
                Entities = componentChanges.Entities,
                ComponentTypes = componentChanges.ComponentTypes,

                AfterBlobAssetRemap = afterBlobAssetRemap,
                BeforeBlobAssetRemap = beforeBlobAssetRemap,
                UseReferentialEquivalence = (byte) (useReferentialEquivalence ? 1 : 0),
            };
        }

        static GatherComponentChangesOutput InitGatherComponentChangesOutput(ComponentChanges componentChanges)
        {
            return new GatherComponentChangesOutput
            {
                AddComponents = componentChanges.AddComponents,
                AddArchetypes = componentChanges.AddArchetypes,
                RemoveComponents = componentChanges.RemoveComponents,
                SetComponents = componentChanges.SetComponents,
                EntityReferencePatches = componentChanges.EntityReferenceChanges,
                BlobAssetReferenceChanges = componentChanges.BlobAssetReferenceChanges,
                LinkedEntityGroupAdditions = componentChanges.LinkedEntityGroupAdditions,
                LinkedEntityGroupRemovals = componentChanges.LinkedEntityGroupRemovals,
                ComponentData = componentChanges.ComponentData,
                SharedComponentChanges = componentChanges.SharedComponentChanges,
                ManagedComponentChanges = componentChanges.ManagedComponentChanges,
                NonSerializableComponents = new NativeList<NonSerializableDebugInfo>(16, Allocator.TempJob)
            };
        }

        static ComponentChanges GetComponentChanges(
            ref ComponentChanges componentChanges,
            EntityInChunkChanges entityChanges,
            bool useReferentialEquivalence,
            NativeParallelHashMap<BlobAssetPtr, BlobAssetPtr> afterBlobAssetRemap,
            NativeParallelHashMap<BlobAssetPtr, BlobAssetPtr> beforeBlobAssetRemap,
            AllocatorManager.AllocatorHandle allocator,
            out JobHandle jobHandle,
            JobHandle dependsOn = default)
        {
            dependsOn.Complete();
            dependsOn = default;

            int count = entityChanges.CreatedEntities.Length + entityChanges.ModifiedEntities.Length;

            var readOnlyData = InitGatherComponentChangesReadOnlyData(
                entityChanges,
                componentChanges,
                useReferentialEquivalence,
                afterBlobAssetRemap,
                beforeBlobAssetRemap);

            var outputData = InitGatherComponentChangesOutput(componentChanges);

            // Pre-populate the entities/component types
            new GatherComponentChangesBuildPacked
            {
                CreatedEntities = entityChanges.CreatedEntities,
                ModifiedEntities = entityChanges.ModifiedEntities,
                TypeInfo = TypeManager.GetTypeInfoPointer(),
                Entities = readOnlyData.Entities,
                ComponentTypes = readOnlyData.ComponentTypes,
                AfterEntityComponentStore = entityChanges.AfterEntityManager.GetCheckedEntityDataAccess()->EntityComponentStore,
                BeforeEntityComponentStore = entityChanges.BeforeEntityManager.GetCheckedEntityDataAccess()->EntityComponentStore,
            }.Run();

            if (count >= 1)
            {
                new GatherComponentChangesJob
                {
                    ReadData = readOnlyData,
                    OutputData = outputData,
                    AfterEntityComponentStore = entityChanges.AfterEntityManager.GetCheckedEntityDataAccess()->EntityComponentStore,
                    BeforeEntityComponentStore = entityChanges.BeforeEntityManager.GetCheckedEntityDataAccess()->EntityComponentStore,
                }.Run();
            }
#if UNITY_EDITOR
            ThrowNonSerializationExceptions(outputData.NonSerializableComponents);
#endif
            outputData.NonSerializableComponents.Dispose();

            jobHandle = default;

            return componentChanges;
        }

#if UNITY_EDITOR
        static void ThrowNonSerializationExceptions(NativeList<NonSerializableDebugInfo> debugInfo)
        {
            foreach (var info in debugInfo)
            {
                var typeName = TypeManager.GetTypeInfo(info.TypeIndex).DebugTypeName;
                var gameObject = EditorUtility.InstanceIDToObject(info.OriginatingId).name;
                string variables = info.OriginatingSubId == 0 ?
                    $"'{typeName}' on GameObject '{gameObject}'" :
                    $"'{typeName}' on GameObject '{gameObject}', '{EditorUtility.InstanceIDToObject(info.OriginatingSubId).GetType()}'";

                if (info.SharedComponent == 1)
                {
                    throw new ArgumentException(
                        $"Shared component type {variables} might contain a (potentially nested) Entity field. " +
                        $"Serializing of shared components with Entity fields is not supported as Entity references are not patched when deserializing. " +
                        $"If for whatever reason this component should still be serialized, add the [ChunkSerializable] attribute to your type to bypass this error.");
                }

                throw new ArgumentException($"Blittable component type {variables} contains a (potentially nested) pointer field. " +
                                            $"Serializing bare pointers will likely lead to runtime errors. Remove this field and consider serializing the data " +
                                            $"it points to another way such as by using a BlobAssetReference or a [Serializable] ISharedComponent. If for whatever " +
                                            $"reason the pointer field should in fact be serialized, add the [ChunkSerializable] attribute to your type to bypass this error.");

            }
        }
#endif

        static EntityChangeSet CreateEntityChangeSet(
            EntityInChunkChanges entityInChunkChanges,
            ComponentChanges componentChanges,
            BlobAssetChanges blobAssetChanges,
            AllocatorManager.AllocatorHandle allocator)
        {
            if (!entityInChunkChanges.IsCreated || !componentChanges.IsCreated || !blobAssetChanges.IsCreated)
            {
                return default;
            }
            s_CreateEntityChangeSetProfilerMarker.Begin();

            // IMPORTANT. This can add to the packed collections. It must be done before adding destroyed entities.
            var sharedComponentDataChanges = GetChangedSharedComponents(
                componentChanges.Entities,
                componentChanges.ComponentTypes,
                componentChanges.SharedComponentChanges,
                componentChanges.BlobAssetReferenceChanges,
                entityInChunkChanges.BeforeEntityManager.GetCheckedEntityDataAccess()->EntityComponentStore,
                entityInChunkChanges.AfterEntityManager.GetCheckedEntityDataAccess()->EntityComponentStore,
                entityInChunkChanges.BeforeEntityManager.GetCheckedEntityDataAccess()->ManagedComponentStore,
                entityInChunkChanges.AfterEntityManager.GetCheckedEntityDataAccess()->ManagedComponentStore,
                allocator,
                out var unmanagedSharedComponentData);

            var managedComponentDataChanges = GetChangedManagedComponents(
                componentChanges.Entities,
                componentChanges.ComponentTypes,
                componentChanges.ManagedComponentChanges,
                componentChanges.EntityReferenceChanges,
                componentChanges.BlobAssetReferenceChanges,
                entityInChunkChanges.AfterEntityManager.GetCheckedEntityDataAccess()->EntityComponentStore,
                entityInChunkChanges.BeforeEntityManager.GetCheckedEntityDataAccess()->ManagedComponentStore,
                entityInChunkChanges.AfterEntityManager.GetCheckedEntityDataAccess()->ManagedComponentStore);

            // Add destroyed entities to componentChanges.Entities
            var entities = componentChanges.Entities.List;
            for (var i = 0; i < entityInChunkChanges.DestroyedEntities.Length; i++)
            {
                entities.Add(entityInChunkChanges.DestroyedEntities[i].EntityGuid);
            }

            s_GetEntityNamesProfilerMarker.Begin();
            var nameChanges = GetEntityNames(
                entityInChunkChanges.CreatedEntities,
                entityInChunkChanges.DestroyedEntities,
                entityInChunkChanges.NameModifiedEntities,
                entityInChunkChanges.AfterEntityManager,
                entityInChunkChanges.BeforeEntityManager,
                allocator);
            s_GetEntityNamesProfilerMarker.End();

            // Allocate and copy in to the results buffers.
            var result = new EntityChangeSet
                (
                entityInChunkChanges.CreatedEntities.Length,
                entityInChunkChanges.DestroyedEntities.Length,
                nameChanges.NameChangedCount,
                componentChanges.Entities.List.ToArray(allocator),
                componentChanges.ComponentTypes.List.ToArray(allocator),
                nameChanges.Names,
                nameChanges.NameChangedEntityGuids,
                componentChanges.AddComponents.ToArray(allocator),
                DeepCopyToArray(componentChanges.AddArchetypes, allocator),
                componentChanges.RemoveComponents.ToArray(allocator),
                componentChanges.SetComponents.ToArray(allocator),
                componentChanges.ComponentData.ToArray(allocator),
                componentChanges.EntityReferenceChanges.ToArray(allocator),
                componentChanges.BlobAssetReferenceChanges.ToArray(allocator),
                managedComponentDataChanges,
                sharedComponentDataChanges,
                unmanagedSharedComponentData,
                componentChanges.LinkedEntityGroupAdditions.ToArray(allocator),
                componentChanges.LinkedEntityGroupRemovals.ToArray(allocator),
                blobAssetChanges.CreatedBlobAssets.ToArray(allocator),
                blobAssetChanges.DestroyedBlobAssets.ToArray(allocator),
                blobAssetChanges.BlobAssetData.ToArray(allocator)
                );

            s_CreateEntityChangeSetProfilerMarker.End();
            return result;
        }

        static NativeArray<FilteredArchetype> DeepCopyToArray(NativeList<FilteredArchetype> addArchetype, AllocatorManager.AllocatorHandle allocator)
        {
            int filledArchetypeCount = 0;
            foreach (var archetype in addArchetype)
            {
                // Means it is an empty entry that was pre-populated
                // Entries are contiguous so return at the first empty Entry, all others will be empty too
                if (archetype.EntityCount == 0)
                    break;

                filledArchetypeCount++;
            }

            // Deep copy the filled Archetype data
            NativeArray<FilteredArchetype> copyAddArchetype = CollectionHelper.CreateNativeArray<FilteredArchetype>(filledArchetypeCount, allocator);
            for (int i = 0; i < filledArchetypeCount; i++)
            {
                var typeIndices = new UnsafeList<TypeIndex>(addArchetype[i].TypeIndices.Length, allocator);
                var packedEntityIndices = new UnsafeList<int>(addArchetype[i].PackedEntityIndices.Length, allocator);
                typeIndices.CopyFrom(addArchetype[i].TypeIndices);
                packedEntityIndices.CopyFrom(addArchetype[i].PackedEntityIndices);

                copyAddArchetype[i] = new FilteredArchetype()
                {
                    EntityCount = addArchetype[i].EntityCount,
                    TypeIndices = typeIndices,
                    PackedEntityIndices = packedEntityIndices
                };
            }
            return copyAddArchetype;
        }

        static PackedSharedComponentDataChange[] GetChangedSharedComponents(
            PackedEntityGuidsCollection packedEntityCollection,
            PackedCollection<ComponentTypeHash> packedStableTypeHashCollection,
            NativeList<DeferredSharedComponentChange> changes,
            NativeList<BlobAssetReferenceChange> blobAssetReferencePatches,
            EntityComponentStore* beforeEntityComponentStore,
            EntityComponentStore* afterEntityComponentStore,
            ManagedComponentStore beforeManagedComponentStore,
            ManagedComponentStore afterManagedComponentStore,
            AllocatorManager.AllocatorHandle allocator,
            out UnsafeAppendBuffer unmanagedSharedComponentData)
        {
            if (changes.Length == 0)
            {
                unmanagedSharedComponentData = default;
                return s_EmptySetSharedComponentDiff;
            }

            s_GetChangedSharedComponentsProfilerMarker.Begin();
            var result = new List<PackedSharedComponentDataChange>();
            unmanagedSharedComponentData = new UnsafeAppendBuffer(0, 8, allocator);

            var managedObjectPatches = new ManagedObjectPatches(afterEntityComponentStore);

            for (var i = 0; i < changes.Length; i++)
            {
                var change = changes[i];
                ref readonly var typeInfo = ref TypeManager.GetTypeInfo(change.TypeIndex);

                object afterValue = null;
                void* afterValueAddr = null;

                if (change.AfterSharedComponentIndex == 0 && change.BeforeSharedComponentIndex == 0)
                    continue;

                if (change.AfterSharedComponentIndex != 0)
                {
                    if (EntityComponentStore.IsUnmanagedSharedComponentIndex(change.AfterSharedComponentIndex))
                    {
                        afterValueAddr = afterEntityComponentStore->GetSharedComponentDataAddr_Unmanaged(change.AfterSharedComponentIndex, change.TypeIndex);
                    }
                    else
                    {
                        afterValue = afterManagedComponentStore.GetSharedComponentDataBoxed(change.AfterSharedComponentIndex, change.TypeIndex);
                    }
                }

                if (change.BeforeSharedComponentIndex != -1 && change.AfterSharedComponentIndex != 0)
                {
                    object beforeValue;
                    void* beforeValueAddr = null;
                    if (EntityComponentStore.IsUnmanagedSharedComponentIndex(change.BeforeSharedComponentIndex))
                    {
                        beforeValueAddr = beforeEntityComponentStore->GetSharedComponentDataAddr_Unmanaged(change.BeforeSharedComponentIndex, change.TypeIndex);
                        if (afterValueAddr != null &&
                            TypeManager.SharedComponentEquals(beforeValueAddr, afterValueAddr, change.TypeIndex))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        beforeValue = beforeManagedComponentStore.GetSharedComponentDataBoxed(change.BeforeSharedComponentIndex, change.TypeIndex);
                        if (TypeManager.Equals(beforeValue, afterValue, change.TypeIndex))
                        {
                            continue;
                        }
                    }
                }

                var packedEntityIndex = packedEntityCollection.Get(change.EntityGuid, 0, 0);
                var packedTypeIndex = packedStableTypeHashCollection.GetOrAdd(new ComponentTypeHash
                {
                    StableTypeHash = typeInfo.StableTypeHash
                });

                var packedComponent = new PackedComponent
                {
                    PackedEntityIndex = packedEntityIndex,
                    PackedTypeIndex = packedTypeIndex
                };

                if (afterValueAddr != null)
                    afterValue = TypeManager.ConstructComponentFromBuffer(change.TypeIndex, afterValueAddr);
                (afterValue as IRefCounted)?.Retain();

                // NOTE: Extracting entity patches from shared components is intentionally disabled here until a full solution is ready.
                if (null != afterValue && (typeInfo.HasBlobAssetRefs || TypeManager.HasEntityReferences(change.TypeIndex)))
                {
                    if (EntityComponentStore.IsUnmanagedSharedComponentIndex(change.BeforeSharedComponentIndex))
                    {
                        var blobAssetRefOffsets = TypeManager.GetBlobAssetRefOffsets(typeInfo);

                        for (int refIndex = 0; refIndex < typeInfo.BlobAssetRefOffsetCount; ++refIndex)
                        {
                            var blobAssetRefOffset = blobAssetRefOffsets[refIndex].Offset;
                            var blobAssetRefPtr = (BlobAssetReferenceData*)((byte*)afterValueAddr + blobAssetRefOffset);

                            var hash = default(ulong);

                            if (blobAssetRefPtr->m_Ptr != null)
                                hash = blobAssetRefPtr->Header->Hash;

                            blobAssetReferencePatches.Add(new BlobAssetReferenceChange
                            {
                                Component = packedComponent,
                                Offset = blobAssetRefOffset,
                                Value = hash
                            });
                        }
                    }
                    else
                    {
                        managedObjectPatches.ExtractPatches(ref afterValue, packedComponent, default, blobAssetReferencePatches);
                    }
                }

                var packedSharedComponentDataChange = new PackedSharedComponentDataChange
                {
                    Component = packedComponent,
                    BoxedSharedValue = afterValueAddr != null ? null : afterValue,
                    UnmanagedSharedValueDataOffsetWithManagedFlag = -1
                };
                if (afterValueAddr != null)
                {
                    packedSharedComponentDataChange.UnmanagedSharedValueDataOffsetWithManagedFlag = unmanagedSharedComponentData.Length;
                    packedSharedComponentDataChange.UnmanagedSharedValueDataOffsetWithManagedFlag &=
                        ~(PackedSharedComponentDataChange.kManagedFlag);

                    unmanagedSharedComponentData.Add(afterValueAddr, typeInfo.TypeSize);
                }
                result.Add(packedSharedComponentDataChange);
            }
            s_GetChangedSharedComponentsProfilerMarker.End();
            return result.ToArray();
        }

        static PackedManagedComponentDataChange[] GetChangedManagedComponents(
            PackedEntityGuidsCollection packedEntityCollection,
            PackedCollection<ComponentTypeHash> packedStableTypeHashCollection,
            NativeList<DeferredManagedComponentChange> changes,
            NativeList<EntityReferenceChange> entityReferencePatches,
            NativeList<BlobAssetReferenceChange> blobAssetReferencePatches,
            EntityComponentStore* afterEntityComponentStore,
            ManagedComponentStore beforeManagedComponentStore,
            ManagedComponentStore afterManagedComponentStore)
        {
            if (changes.Length == 0)
            {
                return s_EmptySetManagedComponentDiff;
            }
            s_GetChangedManagedComponentsProfilerMarker.Begin();

            var result = new List<PackedManagedComponentDataChange>();

            var managedObjectClone = new ManagedObjectClone();

            var managedObjectPatches = new ManagedObjectPatches(afterEntityComponentStore);

            for (var i = 0; i < changes.Length; i++)
            {
                var change = changes[i];
                ref readonly var typeInfo = ref TypeManager.GetTypeInfo(change.TypeIndex);

                if (typeInfo.Category == TypeManager.TypeCategory.UnityEngineObject)
                {
                    // Hybrid Components should be ignored in the diff, the Companion Link will clone the Companion GameObject
                    // and when we apply the diff we'll relink the Hybrid Component to the ones from the Companion GameObject
                    continue;
                }

                var afterValue = afterManagedComponentStore.GetManagedComponent(change.AfterManagedComponentIndex);

                if (change.BeforeManagedComponentIndex > -1)
                {
                    var beforeValue = beforeManagedComponentStore.GetManagedComponent(change.BeforeManagedComponentIndex);

                    if (TypeManager.Equals(beforeValue, afterValue, change.TypeIndex))
                    {
                        continue;
                    }
                }

                var packedEntityIndex = packedEntityCollection.Get(change.EntityGuid, 1, i);
                var packedTypeIndex = packedStableTypeHashCollection.GetOrAdd(new ComponentTypeHash
                {
                    StableTypeHash = typeInfo.StableTypeHash
                });

                var packedComponent = new PackedComponent
                {
                    PackedEntityIndex = packedEntityIndex,
                    PackedTypeIndex = packedTypeIndex
                };

                afterValue = managedObjectClone.Clone(afterValue);

                if (null != afterValue && (typeInfo.HasBlobAssetRefs || TypeManager.HasEntityReferences(change.TypeIndex)))
                    managedObjectPatches.ExtractPatches(ref afterValue, packedComponent, entityReferencePatches, blobAssetReferencePatches);

                result.Add(new PackedManagedComponentDataChange
                {
                    Component = packedComponent,
                    BoxedValue = afterValue
                });
            }
            s_GetChangedManagedComponentsProfilerMarker.End();
            return result.ToArray();
        }

#if !DOTS_DISABLE_DEBUG_NAMES
        [BurstCompile]
        struct GetEntityNamesJob : IJob
        {
            public TypeIndex EntityGuidTypeIndex;
            public NameChangeSet NameChanges;
            public NativeList<CreatedEntity> CreatedEntities;
            public NativeList<DestroyedEntity> DestroyedEntities;
            public NativeList<NameModifiedEntity> NameModifiedEntities;
#if ENTITY_STORE_V1
            public UnsafeBitArray NameChangeBitsByEntity;
#else
            public EntityNameStoreAccess NameStoreAccess;
#endif
            [NativeDisableUnsafePtrRestriction] public int* NameChangeCount;
            [NativeDisableUnsafePtrRestriction] public EntityComponentStore* AfterEntityComponentStore;
            [NativeDisableUnsafePtrRestriction] public EntityComponentStore* BeforeEntityComponentStore;

            bool TryGetEntityGuid(EntityComponentStore* entityComponentStoreEntity, Entity entity, out EntityGuid entityGuid)
            {
                entityGuid = default;

                if (entityComponentStoreEntity->Exists(entity) &&
                    TryGetEntityGuidComponent(entityComponentStoreEntity, entity, EntityGuidTypeIndex, out entityGuid))
                {
                    return true;
                }

                return false;
            }

            [BurstCompile]
            public void Execute()
            {
                var namesPtr = (FixedString64Bytes*)NameChanges.Names.GetUnsafeReadOnlyPtr();
                var entityGuidsPtr = (EntityGuid*)NameChanges.NameChangedEntityGuids.GetUnsafeReadOnlyPtr();

                var length = CreatedEntities.Length + NameModifiedEntities.Length + DestroyedEntities.Length;
                var entitiesLookup = new UnsafeParallelHashSet<EntityGuid>(length, Allocator.TempJob);

                // Created entities will ALWAYS show up in the entityGuid set so we can safely grab the names.
                // They will exist in the after world.
                int nameIndex = 0;
                int guidIndex = 0;
                for (var i = 0; i < CreatedEntities.Length; i++)
                {
                    var afterEntity = ChunkDataUtility.GetEntityFromEntityInChunk(AfterEntityComponentStore->GetArchetype(CreatedEntities[i].AfterEntityInChunk.Chunk), CreatedEntities[i].AfterEntityInChunk);
                    AfterEntityComponentStore->GetName(afterEntity, out namesPtr[nameIndex++]);
                    entitiesLookup.Add(CreatedEntities[i].EntityGuid);

                }

                // Entities with name and component changes
                for (var i = 0; i < NameModifiedEntities.Length; i++)
                {
                    var entity = NameModifiedEntities[i].Entity;
                    AfterEntityComponentStore->GetName(entity, out namesPtr[nameIndex++]);
                    entityGuidsPtr[guidIndex++] = NameModifiedEntities[i].EntityGuid;
                    entitiesLookup.Add(NameModifiedEntities[i].EntityGuid);
                }

#if ENTITY_STORE_V1
                // Only check name change bits when the sequence number of 2 worlds are the same.
                // When the sequence numbers are the same, it is possible that there are entities with only
                // name changes that are not captured in GetEntityInChunkChanges.
                if (AfterEntityComponentStore->NameChangeBitsSequenceNum == BeforeEntityComponentStore->NameChangeBitsSequenceNum)
                {
                    int checkLength = Math.Min(AfterEntityComponentStore->EntitiesCapacity, NameChangeBitsByEntity.Length);
                    // Entities with name changes only
                    for (var i = 0; i < checkLength; i++)
                    {
                        if (NameChangeBitsByEntity.IsSet(i))
                        {
                            var entity = AfterEntityComponentStore->GetEntityByEntityIndex(i);

                            if (TryGetEntityGuid(AfterEntityComponentStore, entity, out var entityGuid))
                            {
                                if (!entitiesLookup.Contains(entityGuid))
                                {
                                    AfterEntityComponentStore->GetName(entity, out namesPtr[nameIndex++]);
                                    entityGuidsPtr[guidIndex++] = entityGuid;
                                }
                            }
                        }
                    }
                }
#else
                // Only check name change bits when the sequence number of 2 worlds are the same.
                // When the sequence numbers are the same, it is possible that there are entities with only
                // name changes that are not captured in GetEntityInChunkChanges.
                if (AfterEntityComponentStore->NameChangeBitsSequenceNum == BeforeEntityComponentStore->NameChangeBitsSequenceNum)
                {
                    var nameSetRO = AfterEntityComponentStore->NameStoreAccess.GetEntityWithNameSetRO();

                    foreach (var entity in nameSetRO)
                    {
                        if (TryGetEntityGuid(AfterEntityComponentStore, entity, out var entityGuid))
                        {
                            if (!entitiesLookup.Contains(entityGuid))
                            {
                                AfterEntityComponentStore->GetName(entity, out namesPtr[nameIndex++]);
                                entityGuidsPtr[guidIndex++] = entityGuid;
                            }
                        }
                    }
                }
#endif

                // Destroyed entities will always show up in the entityGuid set so we can grab the rest of those names.
                // They will not exist in the after world so use the before world.
                for (var i = 0; i < DestroyedEntities.Length; i++)
                {
                    var beforeArchetype =
                        BeforeEntityComponentStore->GetArchetype(DestroyedEntities[i].BeforeEntityInChunk.Chunk);
                    var beforeEntity = ChunkDataUtility.GetEntityFromEntityInChunk(beforeArchetype, DestroyedEntities[i].BeforeEntityInChunk);
                    BeforeEntityComponentStore->GetName(beforeEntity, out namesPtr[nameIndex++]);
                }

                *NameChangeCount = guidIndex;
                entitiesLookup.Dispose();
            }
        }
#endif

        /// <summary>
        /// This method returns all entity names for the given array of entityGuid components.
        /// </summary>
        /// <remarks>
        /// This method relies on the source buffers the entityGuids was built from. While this could technically be done
        /// while building the entityGuid set, it's a bit more isolated this way so we can remove it easily in the future.
        /// </remarks>
        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "!DOTS_DISABLE_DEBUG_NAMES")]
        internal static NameChangeSet GetEntityNames(
            NativeList<CreatedEntity> createdEntities,
            NativeList<DestroyedEntity> destroyedEntities,
            NativeList<NameModifiedEntity> nameModifiedEntities,
            EntityManager afterEntityManager,
            EntityManager beforeEntityManager,
            AllocatorManager.AllocatorHandle allocator)
        {
            var length = createdEntities.Length + destroyedEntities.Length + nameModifiedEntities.Length;

#if !DOTS_DISABLE_DEBUG_NAMES

#if ENTITY_STORE_V1
            var nameChangeBitsByEntity = afterEntityManager.GetCheckedEntityDataAccess()->EntityComponentStore->NameChangeBitsByEntity;
            length += nameChangeBitsByEntity.CountBits(0, nameChangeBitsByEntity.Length);
#else
            length += afterEntityManager.GetCheckedEntityDataAccess()->EntityComponentStore->NameStoreAccess.CountEntitiesWithNamesSet();
#endif

#endif

            // No entity name changes
            if (length == 0)
            {
                return new NameChangeSet(0, allocator);
            }

            var namesChanges = new NameChangeSet(length, allocator);
            int nameChangeCount = 0;

#if !DOTS_DISABLE_DEBUG_NAMES
            new GetEntityNamesJob
            {
                EntityGuidTypeIndex = TypeManager.GetTypeIndex<EntityGuid>(),
                NameChanges = namesChanges,
                NameChangeCount = &nameChangeCount,
                CreatedEntities = createdEntities,
                DestroyedEntities = destroyedEntities,
                NameModifiedEntities = nameModifiedEntities,
#if ENTITY_STORE_V1
                NameChangeBitsByEntity = nameChangeBitsByEntity,
#else
                NameStoreAccess = afterEntityManager.GetCheckedEntityDataAccess()->EntityComponentStore->NameStoreAccess,
#endif
                AfterEntityComponentStore = afterEntityManager.GetCheckedEntityDataAccess()->EntityComponentStore,
                BeforeEntityComponentStore = beforeEntityManager.GetCheckedEntityDataAccess()->EntityComponentStore
            }.Run();
#endif
            namesChanges.NameChangedCount = nameChangeCount;

            return namesChanges;
        }

    class ManagedObjectPatches :
            PropertyVisitor,
            IVisitPropertyAdapter<Entity>,
            IVisitPropertyAdapter<BlobAssetReferenceData>
        {
            readonly EntityComponentStore* m_EntityComponentStore;

            PackedComponent m_Component;
            NativeList<EntityReferenceChange> m_EntityReferencePatches;
            NativeList<BlobAssetReferenceChange> m_BlobAssetReferencePatches;
            int m_EntityReferencePatchId;
            int m_BlobAssetReferencePatchId;

            public ManagedObjectPatches(EntityComponentStore* entityComponentStore)
            {
                m_EntityComponentStore = entityComponentStore;
                AddAdapter(this);
            }

            public void ExtractPatches(
                ref object value,
                PackedComponent component,
                NativeList<EntityReferenceChange> entityReferencePatches,
                NativeList<BlobAssetReferenceChange> blobAssetReferencePatches)
            {
                m_Component = component;
                m_EntityReferencePatches = entityReferencePatches;
                m_BlobAssetReferencePatches = blobAssetReferencePatches;
                m_EntityReferencePatchId = 0;
                m_BlobAssetReferencePatchId = 0;

                PropertyContainer.TryAccept(this, ref value, out _);
            }

            protected override void VisitProperty<TContainer, TValue>(Property<TContainer, TValue> property, ref TContainer container, ref TValue value)
            {
                if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(TValue)))
                    return;

                base.VisitProperty(property, ref container, ref value);
            }

            void IVisitPropertyAdapter<Entity>.Visit<TContainer>(in VisitContext<TContainer, Entity> context, ref TContainer container, ref Entity value)
            {
                if (!m_EntityReferencePatches.IsCreated) return;

                var entityGuid = default(EntityGuid);

                if (m_EntityComponentStore->HasComponent(value, TypeManager.GetTypeIndex<EntityGuid>()))
                    entityGuid = *(EntityGuid*)m_EntityComponentStore->GetComponentDataWithTypeRO(value, TypeManager.GetTypeIndex<EntityGuid>());

                value = new Entity { Index = m_EntityReferencePatchId, Version = -1 };

                m_EntityReferencePatches.Add(new EntityReferenceChange
                {
                    Component = m_Component,
                    Offset = m_EntityReferencePatchId++,
                    Value = entityGuid
                });
            }

            void IVisitPropertyAdapter<BlobAssetReferenceData>.Visit<TContainer>(in VisitContext<TContainer, BlobAssetReferenceData> context, ref TContainer container, ref BlobAssetReferenceData value)
            {
                if (!m_BlobAssetReferencePatches.IsCreated) return;

                var hash = default(ulong);

                if (value.m_Ptr != null)
                    hash = value.Header->Hash;

                value.m_Align8Union = m_BlobAssetReferencePatchId;

                m_BlobAssetReferencePatches.Add(new BlobAssetReferenceChange
                {
                    Component = m_Component,
                    Offset = m_BlobAssetReferencePatchId++,
                    Value = hash
                });
            }
        }
    }
}
