using System;
using HarmonyLib;
using PlayerEquipment;
using Unity.Entities;
using Unity.Mathematics;
// ReSharper disable PossiblyImpureMethodCallOnReadonlyVariable

namespace CoreLib.Submodule.EquipmentSlot.Patch
{
    public static class EquipmentSlot_Patch
    {
        [HarmonyPatch(
            typeof(global::EquipmentSlot), 
            "GetTileSizeFromVariation", 
            new[] { typeof(EquipmentSlotCD), typeof(DynamicBuffer<PlacementSizeByEquipmentTypeBuffer>), typeof(int2) }, 
            new[] { ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal }
        )]
        [HarmonyPrefix]
        public static bool OnGetTileSizeFromVariation(
            ref int2 __result, 
            ref EquipmentSlotCD equipmentSlotCD,
            ref DynamicBuffer<PlacementSizeByEquipmentTypeBuffer> placementSizeByEquipmentTypeBuffer, 
            int2 prefabTileSize
        )
        {
            var slotType = equipmentSlotCD.slotType;
            if (!EquipmentSlotModule.GetSlotInfoFor(slotType, out var slotInfo)) return true;
            if (slotInfo.resizeIndex == -1) return false;

            var sizeVariation = placementSizeByEquipmentTypeBuffer.ElementAt(slotInfo.resizeIndex).sizeVariationToPlace;
            
            __result = global::EquipmentSlot.GetTileSizeFromVariation(sizeVariation, prefabTileSize);
            return false;
        }
    }
}