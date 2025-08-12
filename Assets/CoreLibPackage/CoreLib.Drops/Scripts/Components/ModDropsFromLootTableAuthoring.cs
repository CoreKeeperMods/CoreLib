using PugConversion;
using UnityEngine;

namespace CoreLib.Drops.Components
{
    /// <summary>
    /// The <c>ModDropsFromLootTableAuthoring</c> class is a MonoBehaviour representing
    /// an authoring component that links a game object to a specific loot table ID.
    /// </summary>
    /// <remarks>
    /// This component serves as a bridge between Unity Editor configurations and the runtime ECS world,
    /// allowing for drops to be defined from a loot table. It interacts with <c>DropTablesModule</c>
    /// to retrieve and manage the corresponding loot table ID.
    /// </remarks>
    public class ModDropsFromLootTableAuthoring : MonoBehaviour
    {
        /// <summary>
        /// A unique identifier representing a specific loot table.
        /// </summary>
        /// <remarks>
        /// This identifier is used to link a component or entity to a loot table configuration.
        /// The loot table defines the set of items or resources that can be generated or dropped
        /// based on predefined rules. The identifier is used internally to retrieve the corresponding
        /// loot table data from the system.
        /// </remarks>
        public string lootTableId;
    }

    /// A converter responsible for transforming ModDropsFromLootTableAuthoring authoring components
    /// into corresponding runtime component data used in the entity component system (ECS).
    /// This converter retrieves the loot table ID from the authoring component and assigns it to the
    /// runtime component `DropsLootFromLootTableCD`. The loot table ID is resolved using the
    /// DropTablesModule.
    public class ModDropsFromLootTableAuthoringConverter : SingleAuthoringComponentConverter<ModDropsFromLootTableAuthoring>
    {
        /// Converts the ModDropsFromLootTableAuthoring component to its runtime equivalent by creating and adding
        /// a DropsLootFromLootTableCD component with the corresponding data from the authoring instance.
        /// <param name="authoring">The ModDropsFromLootTableAuthoring instance to convert.</param>
        protected override void Convert(ModDropsFromLootTableAuthoring authoring)
        {
            AddComponentData(new DropsLootFromLootTableCD
            {
                lootTableID = DropTablesModule.GetLootTableID(authoring.lootTableId)
            });
        }
    }
}