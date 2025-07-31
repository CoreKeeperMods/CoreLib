using PlayerEquipment;
using Unity.Entities;

// ReSharper disable SuspiciousTypeConversion.Global

namespace CoreLib.Equipment.System
{
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

                    foreach (var slotPair in EquipmentModule.slots)
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