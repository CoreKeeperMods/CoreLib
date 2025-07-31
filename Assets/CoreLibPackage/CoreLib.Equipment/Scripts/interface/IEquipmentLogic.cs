using PlayerEquipment;
using Unity.Entities;

namespace CoreLib.Equipment
{
    /// <summary>
    /// Interface for implementing equipment slot logic class
    /// </summary>
    public interface IEquipmentLogic
    {
        public bool CanUseWhileSitting { get; }
        public bool CanUseWhileOnBoat { get; }

        public void CreateLookups(ref SystemState state);

        public bool Update(
            EquipmentUpdateAspect equipmentAspect,
            EquipmentUpdateSharedData sharedData,
            LookupEquipmentUpdateData lookupData,
            bool interactHeld,
            bool secondInteractHeld
        );
    }
}