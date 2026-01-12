using PlayerEquipment;
using Unity.Collections;
using Unity.Mathematics;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.EquipmentSlot.Interface
{
    /// Defines logic for determining if an object can be placed at a specified position within the game world.
    /// This interface is typically used in conjunction with placement systems that involve checking game rules,
    /// entity constraints, and environmental conditions for valid object placement.
    public interface IPlacementLogic
    {
        /// Determines if an object can be placed at the specified position based on placement logic.
        /// <param name="placementPrefab">The entity prefab of the object being placed.</param>
        /// <param name="posToPlaceAt">The position where the object is intended to be placed, expressed as an integer 3D coordinate.</param>
        /// <param name="width">The width of the object to be placed.</param>
        /// <param name="height">The height of the object to be placed.</param>
        /// <param name="tilesChecked">A collection of tiles that have been checked for validation during placement.</param>
        /// <param name="diggableEntityAndInfos">
        /// A reference to a list where information about related diggable entities and their placement data can be stored or modified.
        /// </param>
        /// <param name="equipmentUpdateAspect">The aspect data used for updating the equipment in the placement logic.</param>
        /// <param name="equipmentUpdateSharedData">The shared data related to equipment updates used during placement.</param>
        /// <param name="equipmentUpdateLookupData">The lookup data required for equipment updates during the placement process.</param>
        /// <returns>
        /// An integer value indicating the outcome of the placement operation:
        /// 0 for a successful placement, or an error code if placement is not possible.
        /// </returns>
        int CanPlaceObjectAtPosition(
            Unity.Entities.Entity placementPrefab,
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