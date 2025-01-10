using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities.Serialization;
using EntityOffsetInfo = Unity.Entities.TypeManager.EntityOffsetInfo;

namespace Unity.Entities
{
    /// <summary>
    /// Utility class to remap Entity IDs.
    /// </summary>
    public static unsafe class EntityRemapUtility
    {
        /// <summary>
        /// Structure mapping a target entity to an entity in the current world.
        /// </summary>
        public struct EntityRemapInfo
        {
            /// <summary>
            /// The version of the source Entity.
            /// </summary>
            public int SourceVersion;
            /// <summary>
            /// The target Entity ID after the remapping.
            /// </summary>
            public Entity Target;
        }

        /// <summary>
        /// Gets the array of Entity targets from an array of <see cref="EntityRemapInfo"/>.
        /// </summary>
        /// <param name="output">The output array, containing the target Entity IDs.</param>
        /// <param name="remapping">The source array of <see cref="EntityRemapInfo"/> structs.</param>
        public static void GetTargets(out NativeArray<Entity> output, NativeArray<EntityRemapInfo> remapping)
        {
            NativeArray<Entity> temp = new NativeArray<Entity>(remapping.Length, Allocator.TempJob);
            var outputs = 0;
            for (var i = 0; i < remapping.Length; ++i)
                if (remapping[i].Target != Entity.Null)
                    temp[outputs++] = remapping[i].Target;
            output = new NativeArray<Entity>(outputs, Allocator.Persistent);
            UnsafeUtility.MemCpy(output.GetUnsafePtr(), temp.GetUnsafePtr(), sizeof(Entity) * outputs);
            temp.Dispose();
        }

        /// <summary>
        /// Adds a new <see cref="EntityRemapInfo"/> element to a remapping array.
        /// </summary>
        /// <param name="remapping">The array of remapped elements.</param>
        /// <param name="source">The source Entity.</param>
        /// <param name="target">The target Entity.</param>
        public static void AddEntityRemapping(ref NativeArray<EntityRemapInfo> remapping, Entity source, Entity target)
        {
            remapping[source.Index] = new EntityRemapInfo { SourceVersion = source.Version, Target = target };
        }

        /// <summary>
        /// Remaps a source Entity using the <see cref="EntityRemapInfo"/> array.
        /// </summary>
        /// <param name="remapping">The array of <see cref="EntityRemapInfo"/> used to perform the remapping.</param>
        /// <param name="source">The source Entity to remap.</param>
        /// <returns>Returns the remapped Entity ID if it is valid in the current world, otherwise returns Entity.Null.</returns>
        public static Entity RemapEntity(ref NativeArray<EntityRemapInfo> remapping, Entity source)
        {
            return RemapEntity((EntityRemapInfo*)remapping.GetUnsafeReadOnlyPtr(), source);
        }

        /// <summary>
        /// Remaps an entity using the <see cref="EntityRemapInfo"/> array.
        /// </summary>
        /// <param name="remapping">The array of <see cref="EntityRemapInfo"/> used to perform the remapping.</param>
        /// <param name="source">The source Entity to remap.</param>
        /// <returns>Returns the remapped Entity ID if it is valid in the current world, otherwise returns Entity.Null.</returns>
        public static Entity RemapEntity(EntityRemapInfo* remapping, Entity source)
        {
            if (source.Version == remapping[source.Index].SourceVersion)
                return remapping[source.Index].Target;
            else
            {
                // When moving whole worlds, we do not allow any references that aren't in the new world
                // to avoid any kind of accidental references
                return Entity.Null;
            }
        }

        /// <summary>
        /// Remaps the internal Entity references when instantiating a prefab.
        /// </summary>
        /// <param name="remapSrc">The source array of entity references.</param>
        /// <param name="remapDst">The array of target entities.</param>
        /// <param name="remappingCount">The size of the source and target arrays.</param>
        /// <param name="source">The source entity to remap.</param>
        /// <returns>If the source Entity is found in the <paramref name="remapSrc"/> array, it is remapped.
        /// Otherwise, returns the <paramref name="source"/> Entity.</returns>
        public static Entity RemapEntityForPrefab(Entity* remapSrc, Entity* remapDst, int remappingCount, Entity source)
        {
            // When instantiating prefabs,
            // internal references are remapped.
            for (int i = 0; i != remappingCount; i++)
            {
                if (source == remapSrc[i])
                    return remapDst[i];
            }
            // And external references are kept.
            return source;
        }

        /// <summary>
        /// Contains the information for applying a patch on a chunk.
        /// </summary>
        public struct EntityPatchInfo
        {
            /// <summary>
            /// The offset within the chunk where the patch is applied.
            /// </summary>
            public int Offset;
            /// <summary>
            /// The stride between adjacent entities that need patching.
            /// </summary>
            public int Stride;
        }

        /// <summary>
        /// Contains the information for applying a patch on a DynamicBuffer in a chunk.
        /// </summary>
        public struct BufferEntityPatchInfo
        {
            /// <summary>Offset within chunk where first buffer header can be found.</summary>
            public int BufferOffset;
            /// <summary>Stride between adjacent buffers that need patching.</summary>
            public int BufferStride;
            /// <summary>Offset (from base pointer of array) where the first entity can be found.</summary>
            public int ElementOffset;
            /// <summary>Stride between adjacent buffer elements.</summary>
            public int ElementStride;
        }

        /// <summary>
        /// Calculates the field offsets.
        /// </summary>
        /// <param name="type">The inspected type.</param>
        /// <param name="hasEntityRefs">True if the type has any fields of type <see cref="Entity"/>, otherwise false.</param>
        /// <param name="hasBlobRefs">True if the type has any fields of type <see cref="BlobAssetReferenceData"/>, otherwise false.</param>
        /// <param name="hasWeakAssetRefs">True if the type has fields of type <see cref="UntypedWeakReferenceId"/>, otherwise false.</param>
        /// <param name="entityOffsets">The offsets of the fields of type <see cref="Entity"/>.</param>
        /// <param name="blobOffsets">The offsets of the fields of type <see cref="BlobAssetReferenceData"/>.</param>
        /// <param name="weakAssetRefOffsets">The offsets of the fields of type <see cref="UntypedWeakReferenceId"/>.</param>
        /// <param name="cache">Cache to accelerate type inspection codepaths when calling this function multiple times.</param>
        public static void CalculateFieldOffsetsUnmanaged(Type type,
            out bool hasEntityRefs,
            out bool hasBlobRefs,
            out bool hasWeakAssetRefs,
            ref NativeList<EntityOffsetInfo> entityOffsets,
            ref NativeList<EntityOffsetInfo> blobOffsets,
            ref NativeList<EntityOffsetInfo> weakAssetRefOffsets,
            ref NativeList<EntityOffsetInfo> unityObjectRefOffsets,
            HashSet<Type> cache = null)
        {
            if(cache == null)
                cache = new HashSet<Type>();

            int entityOffsetsCount = entityOffsets.Length;
            int blobOffsetsCount = blobOffsets.Length;
            int weakAssetRefCount = weakAssetRefOffsets.Length;
            CalculateOffsetsRecurse(ref entityOffsets, ref blobOffsets, ref weakAssetRefOffsets, ref unityObjectRefOffsets, type, 0, cache);

            hasEntityRefs = entityOffsets.Length != entityOffsetsCount;
            hasBlobRefs = blobOffsets.Length != blobOffsetsCount;
            hasWeakAssetRefs = weakAssetRefOffsets.Length != weakAssetRefCount;
        }

        static bool CalculateOffsetsRecurse(ref NativeList<EntityOffsetInfo> entityOffsets, ref NativeList<EntityOffsetInfo> blobOffsets, ref NativeList<EntityOffsetInfo> weakAssetRefOffsets, ref NativeList<EntityOffsetInfo> unityObjectRefOffsets, Type type, int baseOffset, HashSet<Type> noOffsetTypes)
        {
            if (noOffsetTypes.Contains(type))
                return false;

            if (type == typeof(Entity))
            {
                entityOffsets.Add(new EntityOffsetInfo { Offset = baseOffset });
                return true;
            }
            else if (type == typeof(BlobAssetReferenceData))
            {
                blobOffsets.Add(new EntityOffsetInfo { Offset = baseOffset });
                return true;
            }
            else if (type == typeof(UntypedWeakReferenceId))
            {
                weakAssetRefOffsets.Add(new EntityOffsetInfo { Offset = baseOffset });
                return true;
            }
            else if (type == typeof(UntypedUnityObjectRef))
            {
                unityObjectRefOffsets.Add(new EntityOffsetInfo { Offset = baseOffset });
                return true;
            }

            bool foundOffset = false;
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (field.FieldType.IsValueType && !field.FieldType.IsPrimitive)
                    foundOffset |= CalculateOffsetsRecurse(ref entityOffsets, ref blobOffsets, ref weakAssetRefOffsets, ref unityObjectRefOffsets, field.FieldType, baseOffset + UnsafeUtility.GetFieldOffset(field), noOffsetTypes);
            }

            if (!foundOffset)
                noOffsetTypes.Add(type);

            return foundOffset;
        }

        /// <summary>
        /// Specifies if a System.Type has any <see cref="Entity"/> or <see cref="BlobAssetReferenceData"/> references in its hierarchy.
        /// </summary>
        /// <remarks>
        /// This enum is returned by <see cref="EntityRemapUtility.HasEntityReferencesManaged"/>
        /// which recursively traverses a System.Type and its fields to find entity or blob asset references.
        ///
        /// In some cases Unity cannot find all the Entity/Blob references within a type.
        /// For example, if the type is polymorphic and non-sealed, or if the type hierarchy is deep, making it too expensive
        /// to be worth inspecting it exhaustively. In this cases, the value <see cref="HasRefResult.MayHaveRef"/> specifies that
        /// although no actual reference was found,
        /// the type cannot be treated as a type which definitely does not have any references during serialization.
        /// </remarks>
        public enum HasRefResult
        {
            /// <summary>The System.Type does not have any References within the entire hierarchy.</summary>
            NoRef = 0,
            /// <summary>
            /// The System.Type might have References.
            /// </summary>
            /// <remarks>
            /// Cases where we can't say with certainty if the type contains any references include
            /// if there is a polymorphic non-sealed type, or if the type hierarchy is deeper than the maximum specified recursion depth.
            ///
            /// This value can be handled while validating the data for serialization.
            /// </remarks>
            MayHaveRef = 1,
            /// <summary>The System.Type has a reference that was directly seen by the function.</summary>
            HasRef = 2
        }

        /// <summary>
        /// Reports whether an Entity blob has any <see cref="Entity"/> or <see cref="BlobAssetReferenceData"/> references.
        /// </summary>
        public struct EntityBlobRefResult
        {
            /// <summary>
            /// Specifies if there are any <see cref="Entity"/> references.
            /// </summary>
            public HasRefResult HasEntityRef;
            /// <summary>
            /// Specifies if there are any <see cref="BlobAssetReferenceData"/> references.
            /// </summary>
            public HasRefResult HasBlobRef;

            /// <summary>
            /// Initializes and returns an instance of EntityBlobRefResult.
            /// </summary>
            /// <param name="hasEntityRef">Specifies if there are any <see cref="Entity"/> references.</param>
            /// <param name="hasBlobRef">Specifies if there are any <see cref="BlobAssetReferenceData"/> references.</param>
            public EntityBlobRefResult(HasRefResult hasEntityRef, HasRefResult hasBlobRef)
            {
                this.HasEntityRef = hasEntityRef;
                this.HasBlobRef = hasBlobRef;
            }
        }

        /// <summary>
        /// Checks if a type has any <see cref="Entity"/> or <see cref="BlobAssetReferenceData"/> references.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="hasEntityReferences">Specifies if the type has any <see cref="Entity"/> references.</param>
        /// <param name="hasBlobReferences">Specifies if the type has any <see cref="BlobAssetReferenceData"/> references.</param>
        /// <param name="cache">Map of type to <see cref="EntityBlobRefResult"/> used to accelerate the type recursion.</param>
        /// <param name="maxDepth">The maximum depth for the recursion.</param>
        public static void HasEntityReferencesManaged(Type type, out HasRefResult hasEntityReferences, out HasRefResult hasBlobReferences, Dictionary<Type,EntityBlobRefResult> cache = null, int maxDepth = 128)
        {
            hasEntityReferences = HasRefResult.NoRef;
            hasBlobReferences = HasRefResult.NoRef;

            if (cache == null)
                cache = new Dictionary<Type, EntityBlobRefResult>();

            ProcessEntityOrBlobReferencesRecursiveManaged(type, ref hasEntityReferences, ref hasBlobReferences, 0, ref cache, maxDepth);
        }


        static void ProcessEntityOrBlobReferencesRecursiveManaged(Type type, ref HasRefResult hasEntityReferences, ref HasRefResult hasBlobReferences, int depth, ref Dictionary<Type,EntityBlobRefResult> cache, int maxDepth = 10)
        {

            // Avoid deep / infinite recursion
            if (depth > maxDepth)
            {
                hasEntityReferences = HasRefResult.MayHaveRef;
                hasBlobReferences = HasRefResult.MayHaveRef;

                return;
            }

            if (cache.ContainsKey(type))
            {
                var result = cache[type];
                hasEntityReferences = result.HasEntityRef;
                hasBlobReferences = result.HasBlobRef;

                return;
            }


            HasRefResult localHasEntityRefs = HasRefResult.NoRef;
            HasRefResult localHasBlobRefs = HasRefResult.NoRef;

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (localHasEntityRefs > 0 && localHasBlobRefs > 0)
                {
                    break;
                }

                var fieldType = field.FieldType;

                // Get underlying type for Array or List
                if (fieldType.IsArray)
                    fieldType = fieldType.GetElementType();
                else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                    fieldType = fieldType.GetGenericArguments()[0];

                if (fieldType.IsPrimitive)
                {

                }
                else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
                {

                }
                else if (fieldType == typeof(Entity))
                {
                    localHasEntityRefs = HasRefResult.HasRef;
                }
                else if (type == typeof(BlobAssetReferenceData))
                {
                    localHasBlobRefs = HasRefResult.HasRef;
                }
                else if (fieldType.IsValueType || fieldType.IsSealed)
                {
                    HasRefResult recursiveHasEntityRefs = HasRefResult.NoRef;
                    HasRefResult recursiveHasBlobRefs = HasRefResult.NoRef;

                    ProcessEntityOrBlobReferencesRecursiveManaged(fieldType, ref recursiveHasEntityRefs,
                        ref recursiveHasBlobRefs, depth + 1, ref cache, maxDepth);

                    localHasEntityRefs = localHasEntityRefs > recursiveHasEntityRefs ? localHasEntityRefs : recursiveHasEntityRefs;
                    localHasBlobRefs = localHasBlobRefs > recursiveHasBlobRefs ? localHasBlobRefs : recursiveHasBlobRefs;
                }
                // It is not possible to determine if there are entity references in a polymorphic non-sealed class type
                else
                {
                    localHasEntityRefs = HasRefResult.MayHaveRef;
                    localHasBlobRefs = HasRefResult.MayHaveRef;
                }
            }

            if(!cache.ContainsKey(type))
                cache[type] = new EntityBlobRefResult(localHasEntityRefs, localHasBlobRefs);

            //Definitely seen a reference takes precedence over maybe, which is more cautious than being sure of no refs
            hasEntityReferences = hasEntityReferences > localHasEntityRefs ?  hasEntityReferences : localHasEntityRefs;
            hasBlobReferences = hasBlobReferences > localHasBlobRefs ?  hasBlobReferences : localHasBlobRefs;

        }

        /// <summary>
        /// Adds <see cref="EntityPatchInfo"/> elements for each of the input offsets.
        /// </summary>
        /// <param name="patches">The patch array.</param>
        /// <param name="offsets">The offset array.</param>
        /// <param name="offsetCount">The number of offsets in the array.</param>
        /// <param name="baseOffset">The base offset of the patch.</param>
        /// <param name="stride">The stride of the patch.</param>
        /// <returns>Returns a pointer to the next free slot in the <paramref name="patches"/> array.</returns>
        public static EntityPatchInfo* AppendEntityPatches(EntityPatchInfo* patches, EntityOffsetInfo* offsets, int offsetCount, int baseOffset, int stride)
        {
            if (offsets == null)
                return patches;

            for (int i = 0; i < offsetCount; i++)
                patches[i] = new EntityPatchInfo { Offset = baseOffset + offsets[i].Offset, Stride = stride };
            return patches + offsetCount;
        }

        /// <summary>
        /// Adds <see cref="BufferEntityPatchInfo"/> elements for each of the input offsets.
        /// </summary>
        /// <param name="patches">The patch array,</param>
        /// <param name="offsets">The offset array.</param>
        /// <param name="offsetCount">The number of offsets in the array.</param>
        /// <param name="bufferBaseOffset">The offset within chunk where first buffer header can be found.</param>
        /// <param name="bufferStride">The stride between adjacent buffers that need patching.</param>
        /// <param name="elementStride">The stride between adjacent buffer elements.</param>
        /// <returns>Returns a pointer to the next free slot in the <paramref name="patches"/> array.</returns>
        public static BufferEntityPatchInfo* AppendBufferEntityPatches(BufferEntityPatchInfo* patches, EntityOffsetInfo* offsets, int offsetCount, int bufferBaseOffset, int bufferStride, int elementStride)
        {
            if (offsets == null)
                return patches;

            for (int i = 0; i < offsetCount; i++)
            {
                patches[i] = new BufferEntityPatchInfo
                {
                    BufferOffset = bufferBaseOffset,
                    BufferStride = bufferStride,
                    ElementOffset = offsets[i].Offset,
                    ElementStride = elementStride,
                };
            }

            return patches + offsetCount;
        }

        /// <summary>
        /// Applies a set of entity patches.
        /// </summary>
        /// <param name="scalarPatches">The scalar patches to apply.</param>
        /// <param name="scalarPatchCount">The number of scalar patches.</param>
        /// <param name="bufferPatches">The buffer patches to apply.</param>
        /// <param name="bufferPatchCount">The number of buffer patches.</param>
        /// <param name="chunkBuffer">The chunk buffer, where the patches are applied.</param>
        /// <param name="entityCount">The number of entities in the chunk.</param>
        /// <param name="remapping">The remapping array.</param>
        public static void PatchEntities(EntityPatchInfo* scalarPatches, int scalarPatchCount,
            BufferEntityPatchInfo* bufferPatches, int bufferPatchCount,
            byte* chunkBuffer, int entityCount, ref NativeArray<EntityRemapInfo> remapping)
        {
            // Patch scalars (single components) with entity references.
            for (int p = 0; p < scalarPatchCount; p++)
            {
                byte* entityData = chunkBuffer + scalarPatches[p].Offset;
                for (int i = 0; i != entityCount; i++)
                {
                    Entity* entity = (Entity*)entityData;
                    *entity = RemapEntity(ref remapping, *entity);
                    entityData += scalarPatches[p].Stride;
                }
            }

            // Patch buffers that contain entity references
            for (int p = 0; p < bufferPatchCount; ++p)
            {
                byte* bufferData = chunkBuffer + bufferPatches[p].BufferOffset;

                for (int i = 0; i != entityCount; ++i)
                {
                    BufferHeader* header = (BufferHeader*)bufferData;

                    byte* elemsBase = BufferHeader.GetElementPointer(header) + bufferPatches[p].ElementOffset;
                    int elemCount = header->Length;

                    for (int k = 0; k != elemCount; ++k)
                    {
                        Entity* entityPtr = (Entity*)elemsBase;
                        *entityPtr = RemapEntity(ref remapping, *entityPtr);
                        elemsBase += bufferPatches[p].ElementStride;
                    }

                    bufferData += bufferPatches[p].BufferStride;
                }
            }
        }

        /// <summary>
        /// Applies a set of patches, remapping the internal prefab entities.
        /// </summary>
        /// <param name="scalarPatches">The scalar patches to apply.</param>
        /// <param name="scalarPatchCount">The number of scalar patches.</param>
        /// <param name="bufferPatches">The buffer patches to apply.</param>
        /// <param name="bufferPatchCount">The number of buffer patches.</param>
        /// <param name="chunkBuffer">The chunk buffer, where the patches are applied.</param>
        /// <param name="indexInChunk">Theo index in chunk of the source entity to remap.</param>
        /// <param name="entityCount">The number of entities in chunk.</param>
        /// <param name="remapSrc">The remap source array.</param>
        /// <param name="remapDst">The remap target array.</param>
        /// <param name="remappingCount">The size of the remap arrays.</param>
        public static void PatchEntitiesForPrefab(EntityPatchInfo* scalarPatches, int scalarPatchCount,
            BufferEntityPatchInfo* bufferPatches, int bufferPatchCount,
            byte* chunkBuffer, int indexInChunk, int entityCount, Entity* remapSrc, Entity* remapDst, int remappingCount)
        {
            // Patch scalars (single components) with entity references.
            for (int p = 0; p < scalarPatchCount; p++)
            {
                byte* entityData = chunkBuffer + scalarPatches[p].Offset;
                for (int e = 0; e != entityCount; e++)
                {
                    Entity* entity = (Entity*)(entityData + scalarPatches[p].Stride * (e + indexInChunk));
                    *entity = RemapEntityForPrefab(remapSrc, remapDst + e * remappingCount, remappingCount, *entity);
                }
            }

            // Patch buffers that contain entity references
            for (int p = 0; p < bufferPatchCount; ++p)
            {
                byte* bufferData = chunkBuffer + bufferPatches[p].BufferOffset;

                for (int e = 0; e != entityCount; e++)
                {
                    BufferHeader* header = (BufferHeader*)(bufferData + bufferPatches[p].BufferStride * (e + indexInChunk));

                    byte* elemsBase = BufferHeader.GetElementPointer(header) + bufferPatches[p].ElementOffset;
                    int elemCount = header->Length;

                    for (int k = 0; k != elemCount; ++k)
                    {
                        Entity* entityPtr = (Entity*)elemsBase;
                        *entityPtr = RemapEntityForPrefab(remapSrc, remapDst + e * remappingCount, remappingCount, *entityPtr);
                        elemsBase += bufferPatches[p].ElementStride;
                    }
                }
            }
        }
    }
}
