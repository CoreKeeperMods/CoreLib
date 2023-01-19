using System;
using CoreLib.Submodules.CustomEntity;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Unity.Collections;

namespace CoreLib.Components
{
    public class ModObjectTypeAuthoring : ModCDAuthoringBase
    {
        public Il2CppValueField<FixedString64Bytes> objectTypeId;

        public ModObjectTypeAuthoring(IntPtr ptr) : base(ptr) { }

        public override bool Apply(EntityMonoBehaviourData data)
        {
            ObjectType objectType = CustomEntityModule.GetObjectType(objectTypeId.Value.Value);
            data.objectInfo.objectType = objectType;
            return true;
        }
    }
}