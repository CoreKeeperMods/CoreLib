using PlayerEquipment;
using Unity.Entities;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.EquipmentSlot.Interface
{
    /// Interface defining logic for equipment slots and their behavior in different scenarios.
    public interface IEquipmentLogic
    {
        /// Can the equipment slot be used while the player is in a sitting state?
        public bool CanUseWhileSitting { get; }

        /// Can the equipment be used while the player is on a boat?
        public bool CanUseWhileOnBoat { get; }

        public bool CanResize => false;

        /// Create component lookups necessary for slot logic in <see cref="Update"/> here
        public void CreateLookups(ref SystemState state);

        /// Updates the state of the equipment slot based on the provided inputs.
        /// <param name="equipmentAspect">player equipment state.</param>
        /// <param name="sharedData">Shared equipment system data</param>
        /// <param name="lookupData">Equipment system component lookups</param>
        /// <param name="interactHeld">Is player holding down main interact button</param>
        /// <param name="secondInteractHeld">Is player holding down secondary interact button</param>
        /// <param name="hasItemInMouse">Does player currently have an item in mouse</param>
        /// <returns>Return true here to consume player input</returns>
        public bool Update(
            EquipmentUpdateAspect equipmentAspect,
            EquipmentUpdateSharedData sharedData,
            LookupEquipmentUpdateData lookupData,
            bool interactHeld,
            bool secondInteractHeld,
            bool hasItemInMouse
        );
    }
}