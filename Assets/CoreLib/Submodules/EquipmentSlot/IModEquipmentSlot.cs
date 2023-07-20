namespace CoreLib.Submodules.Equipment
{
    /// <summary>
    /// Define custom Equipment Slot by implementing this interface
    /// Equipment slots define item interaction and usage logic
    /// </summary>
    public interface IModEquipmentSlot
    {
        /// <summary>
        /// Return ObjectType this Equipment Slot is for
        /// </summary>
        ObjectType GetSlotObjectType();

        /// <summary>
        /// Handle custom hand visuals here
        /// </summary>
        void UpdateSlotVisuals(PlayerController controller);
    }
}