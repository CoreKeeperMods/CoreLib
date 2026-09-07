// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.EquipmentSlot.Interface
{
    /// Interface to define modded equipment slots
    public interface IModEquipmentSlot
    {
        /// Returns the ObjectType associated with this Equipment Slot.
        ObjectType GetSlotObjectType();

        /// Updates the visual representation of the equipment slot.
        /// <param name="controller">The player controller used to manage the slot visuals.</param>
        void UpdateSlotVisuals(PlayerController controller);
    }
}