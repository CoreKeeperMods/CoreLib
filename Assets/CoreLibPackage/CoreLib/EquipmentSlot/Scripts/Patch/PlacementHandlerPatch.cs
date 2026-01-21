using CoreLib.Submodule.EquipmentSlot.Interface;
using HarmonyLib;
using PlayerEquipment;
using Unity.Collections;
using Unity.Mathematics;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.EquipmentSlot.Patch
{
    /// Patch class for modifying and extending the functionality of the PlacementHandler class.
    public static class PlacementHandlerPatch
    {
        /// Determines if an object can be placed at the specified position for a given equipment slot type.
        /// <param name="placementPrefab">The entity prefab to be placed.</param>
        /// <param name="posToPlaceAt">The position where the object is intended to be placed.</param>
        /// <param name="width">The width of the object being placed.</param>
        /// <param name="height">The height of the object being placed.</param>
        /// <param name="tilesChecked">A hash map of tiles that have been checked for placement compatibility.</param>
        /// <param name="diggableEntityAndInfos">A list of entities and related information affected by the placement.</param>
        /// <param name="equipmentUpdateAspect">Aspect containing data necessary for updating equipment.</param>
        /// <param name="equipmentUpdateSharedData">Shared data relevant to equipment updates.</param>
        /// <param name="equipmentUpdateLookupData">Lookup data required for equipment updates.</param>
        /// <param name="__result">A reference to the result indicating if placement is allowed (1 for true, 0 for false).</param>
        /// <returns>Returns a boolean value indicating whether the method can proceed with placement logic (true) or not (false).</returns>
        [HarmonyPatch(typeof(PlacementHandler), nameof(PlacementHandler.CanPlaceObjectAtPositionForSlotType))]
        [HarmonyPrefix]
        public static bool CanPlaceObjectAtPositionForSlotType(
            Unity.Entities.Entity placementPrefab, 
            int3 posToPlaceAt, 
            int width, 
            int height,
            NativeHashMap<int3, bool> tilesChecked, 
            ref NativeList<PlacementHandler.EntityAndInfoFromPlacement> diggableEntityAndInfos, 
            in EquipmentUpdateAspect equipmentUpdateAspect, 
            in EquipmentUpdateSharedData equipmentUpdateSharedData, 
            in LookupEquipmentUpdateData equipmentUpdateLookupData,
            ref int __result
            )
        {
            EquipmentSlotType slotType = equipmentUpdateAspect.equipmentSlotCD.ValueRO.slotType;
            var slotTypeNum = (int)slotType;
            
            if (slotTypeNum < EquipmentSlotModule.MOD_SLOT_TYPE_ID_START) return true;
            if (!EquipmentSlotModule.slots.TryGetValue(slotType, out var slotInfo)) return true;
            
            var logic = slotInfo.logic;
            if (logic is not IPlacementLogic placementLogic) return true;
            
            __result = placementLogic.CanPlaceObjectAtPosition(
                placementPrefab,
                posToPlaceAt,
                width,
                height,
                tilesChecked,
                ref diggableEntityAndInfos,
                in equipmentUpdateAspect,
                in equipmentUpdateSharedData,
                in equipmentUpdateLookupData
            );
            return false;
        } 
    }
}