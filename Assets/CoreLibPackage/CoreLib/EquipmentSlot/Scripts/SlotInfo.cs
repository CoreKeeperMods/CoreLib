using System;
using CoreLib.Submodule.EquipmentSlot.Interface;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.EquipmentSlot
{
    /// Represents the information associated with an equipment slot.
    /// <remarks>
    /// This class stores metadata and logic relating to a specific equipment slot within the system.
    /// It is primarily used to associate the slot with its module logic, type, and object type.
    /// </remarks>
    public class SlotInfo
    {
        /// Represents the GameObject associated with a specific equipment slot.
        /// This variable holds the reference to the GameObject that serves as the visual or functional
        /// representation of the slot in the game environment.
        public GameObject slot;

        /// A variable that represents the implementation of custom logic for equipment slots.
        /// It holds an instance of an object that implements the IEquipmentLogic interface, which
        /// allows specialized behavior or functionality to be defined and associated with specific
        /// equipment slots.
        /// This logic can be utilized in various systems or methods to handle operations such as
        /// placement, updates, or validations related to a specific equipment slot.
        /// Assigned via the `RegisterEquipmentSlot` method in the EquipmentSlotModule, this variable
        /// is configured when creating or registering equipment slots during application initialization
        /// or setup.
        /// Usage of this logic includes scenarios such as:
        /// - Custom placement validations through IPlacementLogic.
        /// - Data lookups or computations during system updates.
        /// The flexibility of this variable enables modular and extendable handling of equipment
        /// slot-specific functionality.
        public IEquipmentLogic logic;

        /// Represents the type associated with a specific equipment slot.
        /// This property defines the type of the slot, which is used to determine compatibility
        /// and behavior for various equipment attached to this slot.
        public Type slotType;

        /// Represents the specific type of an object within the equipment system.
        /// This variable is used to categorize objects and associate them with equipment slots or related logic.
        /// It facilitates operations such as determining slot compatibility or mapping functionality to a specific object type.
        public ObjectType objectType;
    }
}