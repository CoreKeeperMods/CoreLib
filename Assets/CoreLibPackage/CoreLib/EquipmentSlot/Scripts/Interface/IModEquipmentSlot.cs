// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.EquipmentSlot.Interface
{
    /// Represents a customizable equipment slot interface for defining unique behavior and interaction logic.
    /// Implement this interface to create specialized functionality for custom equipment slots.
    public interface IModEquipmentSlot
    {
        /// Returns the ObjectType associated with this Equipment Slot.
        ObjectType GetSlotObjectType();

        /// Updates the visual representation of the equipment slot.
        /// <param name="controller">The player controller used to manage the slot visuals.</param>
        void UpdateSlotVisuals(PlayerController controller);
    }
}