using CoreLib.Equipment.Component;
using HarmonyLib;
using UnityEngine;

namespace CoreLib.Equipment.Patches
{
    // ReSharper disable once InconsistentNaming
    public static class ObjectAuthoringConverter_Patch
    {
        [HarmonyPatch(typeof(ObjectConverter), nameof(ObjectConverter.Convert))]
        [HarmonyPrefix]
        public static void ObjectAuthoringConvert_Patch(ObjectAuthoring authoring)
        {
            if (authoring.TryGetComponent(out ModObjectTypeAuthoring component))
            {
                ObjectType objectType = EquipmentModule.GetObjectType(component.objectTypeId);
                authoring.objectType = objectType;
            }
        }
        
        [HarmonyPatch(typeof(EntityMonoBehaviourDataConverter), nameof(EntityMonoBehaviourDataConverter.Convert))]
        [HarmonyPrefix]
        public static void EntityMonoBehaviourDataConvert_Patch(EntityMonoBehaviourData authoring)
        {
            if (!authoring.TryGetComponent(out ModObjectTypeAuthoring component)) return;
            
            ObjectType objectType = EquipmentModule.GetObjectType(component.objectTypeId);
            authoring.objectInfo.objectType = objectType;
        }

        [HarmonyPatch(typeof(CooldownConverter), nameof(CooldownConverter.Convert))]
        [HarmonyPrefix]
        public static void CooldownConverterConvert_Patch(CooldownAuthoring authoring)
        {
            EntityMonoBehaviourData dataAuthoring = authoring.GetComponent<EntityMonoBehaviourData>();
            ObjectAuthoring objectAuthoring = authoring.GetComponent<ObjectAuthoring>();

            if (dataAuthoring != null)
            {
                EntityMonoBehaviourDataConvert_Patch(dataAuthoring);
            }

            if (objectAuthoring != null)
            {
                ObjectAuthoringConvert_Patch(objectAuthoring);
            }
        }
    }
}