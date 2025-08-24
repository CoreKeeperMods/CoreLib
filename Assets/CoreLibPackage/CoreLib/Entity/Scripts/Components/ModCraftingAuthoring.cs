using System.Collections.Generic;
using System.Linq;
using Pug.UnityExtensions;
using PugConversion;
using PugMod;
using Unity.Mathematics;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Components
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
        /// <remarks>
        /// The crafting type defines the behavior and functionality of the crafting process.
        /// Examples of crafting types include:
        /// - Simple: Create a limited number of items.
        /// - Process Resources: Transform one item into another.
        /// - Boss Statue: Enable specific interactions and crafting features.
        /// - Cooking: Combine multiple items to produce a new item.
        /// - Cattle: Automatically produce items without external input.
        /// The type determines how input, output, and crafting mechanics are represented in the system.
        /// </remarks>
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
        [ArrayElementTitle("objectID, amount")][Tooltip("Objects/Items this Building can craft")]
        public List<InventoryItemAuthoring.CraftingObject> canCraftObjects;

        /// <summary>
        /// A list of building IDs from which crafted objects should be included for this building's crafting capabilities.
        /// </summary>
        /// <remarks>
        /// This property allows the current building to inherit crafting recipes or objects from other specified buildings.
        /// When populated, the items from the listed buildings will be added to the current building's crafting options.
        /// </remarks>
        [PickStringFromEnum(typeof (ObjectID))] [Tooltip("Buildings listed below will have their items added to the crafted objects in this Building")]
        public List<string> includeCraftedObjectsFromBuildings;

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
        /// <param name="authoring">The ModCraftingAuthoring component to convert to runtime data.</param>
        protected override void Convert(ModCraftingAuthoring authoring)
        {
            AddComponentData(new PugTimerUserCD
            {
                triggerType = typeof(CraftingTimerTriggerCD)
            });
            EnsureHasComponent<PugTimerRefCD>();
            if (authoring.craftingType != CraftingType.Cattle)
            {
                if (!TryGetActiveComponent(authoring, out InventoryAuthoring _))
                {
                    Debug.LogError($"{authoring.gameObject} has non-cattle crafting but no inventory.");
                    return;
                }

                int outputSlotIndex = AddToBuffer(default(ContainedObjectsBuffer));
                AddComponentData(new CraftingCD
                {
                    currentlyCraftingIndex = -1,
                    craftingType = authoring.craftingType,
                    outputSlotIndex = outputSlotIndex,
                    showLoopEffectOnOutputSlot = authoring.showLoopEffectOnOutputSlot
                });
            }
            else
            {
                AddComponentData(new CraftingCD
                {
                    currentlyCraftingIndex = -1,
                    craftingType = authoring.craftingType,
                    showLoopEffectOnOutputSlot = authoring.showLoopEffectOnOutputSlot
                });
            }

            CraftingCD.IsProcessAutoCrafter(authoring.craftingType);
            EnsureHasBuffer<CanCraftObjectsBuffer>();
            foreach (InventoryItemAuthoring.CraftingObject craftableObject in authoring.canCraftObjects)
            {
                CanCraftObjectsBuffer elementData = new CanCraftObjectsBuffer
                {
                    objectID = API.Authoring.GetObjectID(craftableObject.objectName),
                    amount = math.max(1, craftableObject.amount),
                    entityAmountToConsume = 0
                };
                AddToBuffer(elementData);
            }
            
            

            if (authoring.includeCraftedObjectsFromBuildings != null)
            {
                foreach (string otherId in authoring.includeCraftedObjectsFromBuildings)
                {
                    EntityModule.ModdedEntities.TryGetValue(otherId, out var otherObjects);
                    if (otherObjects == null || otherObjects.Count == 0) continue;

                    var otherObject = otherObjects.First();

                    var craftingAuthoring2 = otherObject.GetComponent<CraftingAuthoring>();
                    var modCraftingAuthoring = otherObject.GetComponent<ModCraftingAuthoring>();

                    if (craftingAuthoring2 != null && craftingAuthoring2.canCraftObjects != null)
                    {
                        foreach (CraftingAuthoring.CraftableObject craftableObject2 in craftingAuthoring2.canCraftObjects)
                        {
                            CanCraftObjectsBuffer elementData = new CanCraftObjectsBuffer
                            {
                                objectID = craftableObject2.objectID,
                                amount = math.max(1, craftableObject2.amount),
                                entityAmountToConsume = (craftableObject2.craftingConsumesEntityAmount ? craftableObject2.entityAmountToConsume : 0)
                            };
                            AddToBuffer(elementData);
                        }
                    }
                    if (modCraftingAuthoring != null && modCraftingAuthoring.canCraftObjects != null)
                    {
                        foreach (InventoryItemAuthoring.CraftingObject craftableObject2 in modCraftingAuthoring.canCraftObjects)
                        {
                            CanCraftObjectsBuffer elementData = new CanCraftObjectsBuffer
                            {
                                objectID = API.Authoring.GetObjectID(craftableObject2.objectName),
                                amount = math.max(1, craftableObject2.amount),
                                entityAmountToConsume = 0
                            };
                            AddToBuffer(elementData);
                        }
                    }
                }

                if (authoring.includeCraftedObjectsFromBuildings.Count > 0)
                {
                    EnsureHasBuffer<IncludedCraftingBuildingsBuffer>();
                    AddToBuffer(new IncludedCraftingBuildingsBuffer
                    {
                        objectID = (ObjectID)ObjectIndex,
                        amountOfCraftingOptions = authoring.canCraftObjects.Count
                    });
                    foreach (string otherId in authoring.includeCraftedObjectsFromBuildings)
                    {
                        EntityModule.ModdedEntities.TryGetValue(otherId, out var otherObjects);
                        if (otherObjects == null || otherObjects.Count == 0) continue;

                        AddToBuffer(new IncludedCraftingBuildingsBuffer
                        {
                            objectID = API.Authoring.GetObjectID(otherId),
                            amountOfCraftingOptions = authoring.canCraftObjects.Count
                        });
                    }
                }
            }
        }
    }
}