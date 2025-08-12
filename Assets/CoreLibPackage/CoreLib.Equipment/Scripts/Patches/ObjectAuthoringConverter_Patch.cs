using CoreLib.Equipment.Component;
using HarmonyLib;

namespace CoreLib.Equipment.Patches
{
    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// The ObjectAuthoringConverter_Patch class contains Harmony patches designed to extend or modify the behavior
    /// of the object conversion process within the authoring system. These patches intercept the Convert methods of
    /// specific conversion types to integrate custom logic for handling modifications related to object types
    /// in the game framework.
    /// </summary>
    public static class ObjectAuthoringConverter_Patch
    {
        /// A patch method for the ObjectConverter's `Convert` method to handle the conversion of
        /// `ObjectAuthoring` by retrieving and setting the appropriate `ObjectType` based on the
        /// `objectTypeId` of the attached `ModObjectTypeAuthoring` component.
        /// This method is intended to be used as a Harmony Prefix.
        /// <param name="authoring">
        /// The `ObjectAuthoring` instance that is being converted. This is the target object to which
        /// the `objectType` will be assigned.
        /// </param>
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

        /// <summary>
        /// Patches the Convert method of EntityMonoBehaviourDataConverter to modify object type data based on the component attached.
        /// </summary>
        /// <param name="authoring">The EntityMonoBehaviourData instance to be patched and modified.</param>
        [HarmonyPatch(typeof(EntityMonoBehaviourDataConverter), nameof(EntityMonoBehaviourDataConverter.Convert))]
        [HarmonyPrefix]
        public static void EntityMonoBehaviourDataConvert_Patch(EntityMonoBehaviourData authoring)
        {
            if (!authoring.TryGetComponent(out ModObjectTypeAuthoring component)) return;
            
            ObjectType objectType = EquipmentModule.GetObjectType(component.objectTypeId);
            authoring.objectInfo.objectType = objectType;
        }

        /// <summary>
        /// Handles the conversion of a CooldownAuthoring instance during the conversion process by using
        /// associated components within the object to update necessary object types.
        /// </summary>
        /// <param name="authoring">The CooldownAuthoring instance being converted.</param>
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