namespace CoreLib.Submodules.Equipment
{
    public interface IModEquipmentSlot
    {
        ObjectType GetSlotObjectType();

        void UpdateSlotVisuals(PlayerController controller);
    }
}