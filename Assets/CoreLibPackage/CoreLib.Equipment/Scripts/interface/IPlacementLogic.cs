using PlayerEquipment;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CoreLib.Equipment
{
    public interface IPlacementLogic
    {
        int CanPlaceObjectAtPosition(
            Entity placementPrefab,
            int3 posToPlaceAt,
            int width,
            int height,
            NativeHashMap<int3, bool> tilesChecked,
            ref NativeList<PlacementHandler.EntityAndInfoFromPlacement> diggableEntityAndInfos, 
            in EquipmentUpdateAspect equipmentUpdateAspect,
            in EquipmentUpdateSharedData equipmentUpdateSharedData,
            in LookupEquipmentUpdateData equipmentUpdateLookupData);
    }
}