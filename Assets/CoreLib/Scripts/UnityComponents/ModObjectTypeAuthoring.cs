using CoreLib.Submodules.Equipment;
using UnityEngine;

namespace CoreLib.Components
{
    public class ModObjectTypeAuthoring : ModCDAuthoringBase
    {
        public string objectTypeId;
        public override bool Apply(MonoBehaviour data)
        {
            ObjectType objectType = EquipmentModule.GetObjectType(objectTypeId);
            if (data is EntityMonoBehaviourData monoData)
            {
                monoData.objectInfo.objectType = objectType;
            }else if (data is ObjectAuthoring objectAuthoring)
            {
                objectAuthoring.objectType = objectType;
            }
            return true;
        }
    }
}