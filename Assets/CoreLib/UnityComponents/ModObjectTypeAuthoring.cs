using System;
using CoreLib.Submodules.Equipment;
using Unity.Collections;
using UnityEngine;

namespace CoreLib.Components
{
    public class ModObjectTypeAuthoring : ModCDAuthoringBase
    {
        public string objectTypeId;
        public override bool Apply(MonoBehaviour data)
        {
            ObjectType objectType = EquipmentSlotModule.GetObjectType(objectTypeId);
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