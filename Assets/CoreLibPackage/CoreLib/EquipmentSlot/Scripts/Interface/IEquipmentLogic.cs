using PlayerEquipment;
using Unity.Entities;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.EquipmentSlot.Interface
{
    /// <summary>
    /// Interface defining logic for equipment slots and their behavior in different scenarios.
    /// </summary>
    public interface IEquipmentLogic
    {
        /// <summary>
        /// Determines whether the equipment can be used while the player is in a sitting state.
        /// </summary>
        /// <remarks>
        /// This property is part of the IEquipmentLogic interface, and its value is used to check
        /// if an interaction or action involving equipment is allowed when the player is sitting.
        /// </remarks>
        /// <value>
        /// Returns true if the equipment can be used while sitting, otherwise false.
        /// </value>
        public bool CanUseWhileSitting { get; }

        /// <summary>
        /// Determines whether the equipment can be used while the player is on a boat.
        /// </summary>
        public bool CanUseWhileOnBoat { get; }

        /// <summary>
        /// Creates the necessary lookups for the equipment logic. This method
        /// is usually invoked to set up or initialize data structures or processes
        /// to efficiently handle equipment operations during the system's lifecycle.
        /// </summary>
        /// <param name="state">A reference to the current system state, which provides
        /// access to entity-related operations and overall system context.</param>
        public void CreateLookups(ref SystemState state);

        /// Updates the state of the equipment based on the provided inputs.
        /// <param name="equipmentAspect">
        /// An instance of EquipmentUpdateAspect that contains information about the player's equipment state.
        /// </param>
        /// <param name="sharedData">
        /// Shared data that may include global or system-wide information required for the update process.
        /// </param>
        /// <param name="lookupData">
        /// A collection of data used to look up information needed for equipment processing, such as cooldown or type-specific logic.
        /// </param>
        /// <param name="interactHeld">
        /// Indicates whether the primary interaction button is being held down.
        /// </param>
        /// <param name="secondInteractHeld">
        /// Indicates whether the secondary interaction button is being held down.
        /// </param>
        /// <returns>
        /// A boolean value indicating whether the update operation was successful or applicable.
        /// </returns>
        public bool Update(
            EquipmentUpdateAspect equipmentAspect,
            EquipmentUpdateSharedData sharedData,
            LookupEquipmentUpdateData lookupData,
            bool interactHeld,
            bool secondInteractHeld
        );
    }
}