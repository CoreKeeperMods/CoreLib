using PlayerEquipment;
using Unity.Mathematics;
// ReSharper disable PossiblyImpureMethodCallOnReadonlyVariable

namespace CoreLib.Submodule.EquipmentSlot
{
    public static class EquipmentSlotUtils
    {
        public static void ChangeSize(in EquipmentUpdateAspect equipmentUpdateAspect, PugDatabase.DatabaseBankCD databaseBank)
        {
            var objectID = equipmentUpdateAspect.equippedObjectCD.ValueRO.containedObject.objectData.objectID;
            ref var entityObjectInfo = ref PugDatabase.GetEntityObjectInfo(objectID, databaseBank.databaseBankBlob);
            ChangeSize(in equipmentUpdateAspect, entityObjectInfo.prefabTileSize);
        }
        
        private static void ChangeSize(in EquipmentUpdateAspect equipmentUpdateAspect, int2 prefabTileSize)
        {
            if (prefabTileSize.x <= 1) return;

            var slotType = equipmentUpdateAspect.equipmentSlotCD.ValueRO.slotType;
            if (!EquipmentSlotModule.GetSlotInfoFor(slotType, out var slotInfo)) return;
            if (slotInfo.resizeIndex == -1) return;
            
            ref var elementForEquipment = ref equipmentUpdateAspect.placementSizeByEquipmentTypeBuffer.ElementAt(slotInfo.resizeIndex);
            
            int num = math.min(elementForEquipment.sizeVariationToPlace, prefabTileSize.x - 1);
            elementForEquipment.sizeVariationToPlace = (byte)((num + 1) % prefabTileSize.x);
        }
    }
}