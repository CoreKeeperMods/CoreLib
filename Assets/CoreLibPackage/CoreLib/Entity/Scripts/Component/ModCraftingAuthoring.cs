using System.Collections.Generic;
using System.Linq;
using CoreLib.Util.Extension;
using Pug.UnityExtensions;
using PugConversion;
using PugMod;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Component
{
    /// <summary>
    /// Provides an authoring component for defining crafting capabilities within a modded entity.
    /// Responsible for configuring crafting behaviors, including crafting types, crafted objects,
    /// and integration with other buildings that contribute to crafting output.
    /// </summary>
    [DisallowMultipleComponent]
    public class ModCraftingAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Specifies the type of crafting associated with an object or item.
        /// </summary>
        [Tooltip("The type of crafting that this item/object does:" +
                 "\nSimple: Make items (up to 18)" +
                 "\nProcess Resources: Make an item turn into another item" +
                 "\nBoss Statue: Activate using an item. Make Items (up to 3)" +
                 "\nCooking: Use 2 items to create a new item" +
                 "\nCattle: Object creates the item on it's own automatically")]
        public CraftingType craftingType;

        /// <summary>
        /// Determines whether a visual effect is displayed on the output slot while processing an item.
        /// </summary>
        [Tooltip("When processing an item, shows an effect on the output slot")]
        public bool showLoopEffectOnOutputSlot;

        /// <summary>
        /// Represents a collection of objects or items that can be crafted by the associated crafting component or building.
        /// Each entry in the list specifies the object ID and the quantity that can be crafted.
        /// </summary>
        [ArrayElementTitle("objectID, amount"), Tooltip("Objects/Items this Building can craft")]
        public List<InventoryItemAuthoring.CraftingObject> canCraftObjects = new();

        /// <summary>
        /// A list of building IDs from which crafted objects should be included for this building's crafting capabilities.
        /// </summary>
        [PickStringFromEnum(typeof (ObjectID)), Tooltip("Buildings listed below will have their items added to the crafted objects in this Building")]
        public List<string> includeCraftedObjectsFromBuildings = new();

        /// <summary>
        /// Called automatically by Unity when the state of the component changes in the Inspector.
        /// Ensures that the crafting objects list does not contain any entries with a quantity of zero or less.
        /// Any such entries are corrected to have a default quantity of one.
        /// </summary>
        private void OnValidate()
        {
            if (canCraftObjects == null) return;
            for (int index = 0; index < canCraftObjects.Count; ++index)
            {
                if (canCraftObjects[index].amount <= 0)
                    canCraftObjects[index] = new InventoryItemAuthoring.CraftingObject
                    {
                        objectName = canCraftObjects[index].objectName,
                        amount = 1
                    };
            }
        }
    }

    /// <summary>
    /// Defines a converter for the ModCraftingAuthoring component, responsible for converting authoring-time
    /// data into runtime configuration for crafting functionality in the ModEntity system.
    /// </summary>
    public class ModCraftingConverter : SingleAuthoringComponentConverter<ModCraftingAuthoring>
    {
        /// <summary>
        /// Converts a ModCraftingAuthoring component into its runtime representation.
        /// This includes adding relevant components and buffers to the associated entity
        /// for crafting, handling crafting types, and processing crafting configurations.
        /// </summary>
        /// <param name="craftingAuthoring">The ModCraftingAuthoring component to convert to runtime data.</param>
        protected override void Convert(ModCraftingAuthoring craftingAuthoring)
        {
            if (craftingAuthoring.GetEntityObjectID() == ObjectID.None) return;
            AddComponentData(new PugTimerUserCD
            {
                triggerType = typeof (CraftingTimerTriggerCD)
            });
            EnsureHasComponent<PugTimerRefCD>();
            int num = -1;
            if (TryGetActiveComponent(craftingAuthoring, out InventoryAuthoring _))
                num = AddToBuffer(new ContainedObjectsBuffer());
            AddComponentData(new CraftingCD
            {
                currentlyCraftingIndex = -1,
                craftingType = craftingAuthoring.craftingType,
                outputSlotIndex = num,
                showLoopEffectOnOutputSlot = craftingAuthoring.showLoopEffectOnOutputSlot
            });
            CraftingCD.IsProcessAutoCrafter(craftingAuthoring.craftingType);
            EnsureHasBuffer<CanCraftObjectsBuffer>();
            foreach (var canCraftObject in craftingAuthoring.canCraftObjects)
                AddToBuffer(new CanCraftObjectsBuffer
                {
                    objectID = API.Authoring.GetObjectID(canCraftObject.objectName),
                    amount = Mathf.Max(1, canCraftObject.amount),
                    entityAmountToConsume = 0,
                    allowCraftingNone = false,
                    craftingTimeOverride = 0
                });
            if (craftingAuthoring.includeCraftedObjectsFromBuildings == null) return;
            foreach (var building in craftingAuthoring.includeCraftedObjectsFromBuildings
                         .Select(data => PugDatabase.entityMonobehaviours.FirstOrDefault(x => x.ObjectInfo.objectID == API.Authoring.GetObjectID(data)))
                         .Where(building => building != null))
            {
                if (building.GameObject.TryGetComponent(out CraftingAuthoring craftingAuthoring1))
                {
                    foreach (var canCraftObject in craftingAuthoring1.canCraftObjects)
                    {
                        AddToBuffer(new CanCraftObjectsBuffer
                        {
                            objectID = canCraftObject.objectID,
                            amount = Mathf.Max(1, canCraftObject.amount),
                            entityAmountToConsume = 0,
                            allowCraftingNone = false,
                            craftingTimeOverride = 0
                        });
                    }
                } else if (building.GameObject.TryGetComponent(out ModCraftingAuthoring craftingAuthoring2))
                {
                    foreach (var canCraftObject in craftingAuthoring2.canCraftObjects)
                    {
                        AddToBuffer(new CanCraftObjectsBuffer
                        {
                            objectID = API.Authoring.GetObjectID(canCraftObject.objectName),
                            amount = Mathf.Max(1, canCraftObject.amount),
                            entityAmountToConsume = 0,
                            allowCraftingNone = false,
                            craftingTimeOverride = 0
                        });
                    }
                }
            }
            if (craftingAuthoring.includeCraftedObjectsFromBuildings.Count <= 0) return;
            EnsureHasBuffer<IncludedCraftingBuildingsBuffer>();
            AddToBuffer(new IncludedCraftingBuildingsBuffer
            {
                objectID = (ObjectID) ObjectIndex,
                amountOfCraftingOptions = craftingAuthoring.canCraftObjects.Count
            });
            foreach (var building in craftingAuthoring.includeCraftedObjectsFromBuildings
                         .Select(data => PugDatabase.entityMonobehaviours.FirstOrDefault(x => x.ObjectInfo.objectID == API.Authoring.GetObjectID(data)))
                         .Where(building => building != null))
            {
                if (building.GameObject.TryGetComponent(out CraftingAuthoring craftingAuthoring1))
                {
                    ObjectID objectId = craftingAuthoring1.GetEntityObjectID();
                    if (objectId == ObjectID.None) continue;
                    AddToBuffer(new IncludedCraftingBuildingsBuffer
                    {
                        objectID = objectId,
                        amountOfCraftingOptions = craftingAuthoring1.canCraftObjects.Count
                    });
                } else if (building.GameObject.TryGetComponent(out ModCraftingAuthoring craftingAuthoring2))
                {
                    ObjectID objectId = craftingAuthoring2.GetEntityObjectID();
                    if (objectId == ObjectID.None) continue;
                    AddToBuffer(new IncludedCraftingBuildingsBuffer
                    {
                        objectID = objectId,
                        amountOfCraftingOptions = craftingAuthoring2.canCraftObjects.Count
                    });
                }
            }
        }
    }
}