using Unity.Collections;

namespace Unity.Entities.Internal
{
    /// <summary>
    /// This exists only for internal use and is intended to be only used by source-generated code.
    /// DO NOT USE in user code (this API will change).
    /// </summary>
    public struct InternalGatherEntitiesResult
    {
        public int StartingOffset;
        public int EntityCount;
        public unsafe Entity* EntityBuffer;
        public NativeArray<Entity> EntityArray;
    }
}
