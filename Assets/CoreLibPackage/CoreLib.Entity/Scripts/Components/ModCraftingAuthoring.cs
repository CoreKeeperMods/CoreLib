using System.Collections.Generic;
using System.Linq;
using CoreLib.Util.Extensions;
using Pug.UnityExtensions;
using PugConversion;
using PugMod;
using Unity.Mathematics;
using UnityEngine;

namespace CoreLib.Submodules.ModEntity.Components
{
    [DisallowMultipleComponent]
    public class ModCraftingAuthoring : MonoBehaviour
    {
        public CraftingType craftingType;
        public bool showLoopEffectOnOutputSlot;

        [ArrayElementTitle("objectID, amount")]
        public List<InventoryItemAuthoring.CraftingObject> canCraftObjects;

        public List<string> includeCraftedObjectsFromBuildings;
        
        private void OnValidate()
        {
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

    public class ModCraftingConverter : SingleAuthoringComponentConverter<ModCraftingAuthoring>
    {
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
                    EntityModule.moddedEntities.TryGetValue(otherId, out var otherObjects);
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
                        EntityModule.moddedEntities.TryGetValue(otherId, out var otherObjects);
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