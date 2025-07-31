using HarmonyLib;
using PlayerEquipment;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CoreLib.Equipment.Patches
{
    public static class PlacementHandler_Patch
    {
        [HarmonyPatch(typeof(PlacementHandler), nameof(PlacementHandler.CanPlaceObjectAtPositionForSlotType))]
        [HarmonyPrefix]
        public static bool CanPlaceObjectAtPositionForSlotType(
            Entity placementPrefab, 
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
            
            if (slotTypeNum < EquipmentModule.ModSlotTypeIdStart) return true;
            if (!EquipmentModule.slots.TryGetValue(slotType, out var slotInfo)) return true;
            
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