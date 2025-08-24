// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.EquipmentSlot
{
    /// <summary>
    /// Represents a customizable equipment slot interface for defining unique behavior and interaction logic.
    /// Implement this interface to create specialized functionality for custom equipment slots.
    /// </summary>
    public interface IModEquipmentSlot
    {
        /// <summary>
        /// Returns the ObjectType associated with this Equipment Slot.
        /// </summary>
        ObjectType GetSlotObjectType();

        /// <summary>
        /// Updates the visual representation of the equipment slot.
        /// </summary>
        /// <param name="controller">The player controller used to manage the slot visuals.</param>
        void UpdateSlotVisuals(PlayerController controller);
    }
}