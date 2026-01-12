using PlayerEquipment;
using Unity.Entities;

// ReSharper disable SuspiciousTypeConversion.Global

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.EquipmentSlot.System
{
    /// The ModEquipmentChangeSystem class handles the modification and assignment
    /// of equipment slots based on the type of equipped objects in the simulation.
    /// 
    /// This system executes within the context of the
    /// <c>EquipmentBeforeUpdateSystemGroup</c>, and it processes entities
    /// containing <c>EquippedObjectCD</c> and <c>EquipmentSlotCD</c> components.
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(EquipmentBeforeUpdateSystemGroup))]
    [UpdateAfter(typeof(SelectedEquipmentChangeSystem))]
    [DisableAutoCreation]
    public partial class ModEquipmentChangeSystem : PugSimulationSystemBase
    {

        protected override void OnCreate()
        {
            RequireForUpdate<PugDatabase.DatabaseBankCD>();
            RequireForUpdate<WorldInfoCD>();
            NeedDatabase();
        }
        
        protected override void OnUpdate()
        {
            var databaseLocal = database;

            Entities.ForEach((
                    ref EquippedObjectCD equippedObject,
                    ref EquipmentSlotCD equipmentSlot
                ) =>
                {
                    var item = equippedObject.containedObject;
                    ref var entityObjectInfo = ref PugDatabase.GetEntityObjectInfo(item.objectID, databaseLocal, item.variation);

                    var objectType = entityObjectInfo.objectType;

                    var objectTypeNum = (int)objectType;
                    if (objectTypeNum < short.MaxValue) return;

                    foreach (var slotPair in EquipmentSlotModule.Slots)
                    {
                        if (slotPair.Value.objectType == objectType)
                        {
                            equipmentSlot.slotType = slotPair.Key;
                            return;
                        }
                    }
                })
                .WithAll<Simulate>()
                .WithoutBurst()
                .Schedule();


            base.OnUpdate();
        }
    }
}