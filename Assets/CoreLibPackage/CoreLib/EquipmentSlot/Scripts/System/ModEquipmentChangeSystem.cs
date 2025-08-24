using PlayerEquipment;
using Unity.Entities;

// ReSharper disable SuspiciousTypeConversion.Global

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.EquipmentSlot.System
{
    /// <summary>
    /// The ModEquipmentChangeSystem class handles the modification and assignment
    /// of equipment slots based on the type of equipped objects in the simulation.
    /// </summary>
    /// <remarks>
    /// This system is relevant for managing equipment changes within the simulation,
    /// specifically modifying the equipment slots based on the properties of the
    /// equipped objects. It is designed to be an integral part of the equipment
    /// handling workflow.
    /// </remarks>
    /// <example>
    /// This system executes within the context of the
    /// <c>EquipmentBeforeUpdateSystemGroup</c>, and it processes entities
    /// containing <c>EquippedObjectCD</c> and <c>EquipmentSlotCD</c> components.
    /// </example>
    /// <remarks>
    /// This class filters entities with specific components and avoids burst
    /// compilation, enabling it to interact with databases and modify equipment
    /// slots accordingly. It requires the presence of <c>PugDatabase.DatabaseBankCD</c>
    /// and <c>WorldInfoCD</c> in the world.
    /// </remarks>
    /// <seealso cref="EquipmentBeforeUpdateSystemGroup"/>
    /// <seealso cref="SelectedEquipmentChangeSystem"/>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(EquipmentBeforeUpdateSystemGroup))]
    [UpdateAfter(typeof(SelectedEquipmentChangeSystem))]
    [DisableAutoCreation]
    public partial class ModEquipmentChangeSystem : PugSimulationSystemBase
    {
        /// <summary>
        /// Called when the system is created. This method is used for initial setup and determines the
        /// necessary components or entities required for the system's execution.
        /// It specifies the required component types using the RequireForUpdate method and performs any
        /// additional initialization steps necessary for the system to function.
        /// </summary>
        protected override void OnCreate()
        {
            RequireForUpdate<PugDatabase.DatabaseBankCD>();
            RequireForUpdate<WorldInfoCD>();
            NeedDatabase();
        }

        /// <summary>
        /// Executes the update logic for the ModEquipmentChangeSystem.
        /// This method processes entities with specific components to determine and update the appropriate equipment slot based on the equipped object's type.
        /// It performs this operation within an Entity ForEach scope and applies the necessary modifications without burst compilation to ensure accurate outcomes.
        /// </summary>
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

                    foreach (var slotPair in EquipmentModule.Slots)
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