using System;
using Unity.Collections;

namespace CoreLib.Components
{
    public class ModObjectTypeAuthoring : ModCDAuthoringBase
    {
        public FixedString64Bytes objectTypeId;
        public bool Apply(EntityMonoBehaviourData data)
        {
            return default(bool);
        }
    }
}