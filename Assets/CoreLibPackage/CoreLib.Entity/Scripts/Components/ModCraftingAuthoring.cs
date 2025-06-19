using System.Collections.Generic;
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
            foreach (var craftableObject in authoring.canCraftObjects)
            {
                AddToBuffer(new CanCraftObjectsBuffer
                {
                    objectID = API.Authoring.GetObjectID(craftableObject.objectName),
                    amount = math.max(1, craftableObject.amount),
                    entityAmountToConsume = 0
                });
            }

            if (authoring.includeCraftedObjectsFromBuildings == null || authoring.includeCraftedObjectsFromBuildings.Count == 0) return;
            EnsureHasBuffer<IncludedCraftingBuildingsBuffer>();
            AddToBuffer(new IncludedCraftingBuildingsBuffer
            {
                objectID = (ObjectID)ObjectIndex,
                amountOfCraftingOptions = authoring.canCraftObjects.Count
            });
            
            foreach (var otherId in authoring.includeCraftedObjectsFromBuildings)
            {
                EntityModule.GetMainEntity(otherId, out var otherObject);
                var otherObject2 = !otherObject ? (EntityMonoBehaviourData) PugDatabase.entityMonobehaviours.Find(mono => 
                    mono.ObjectInfo.objectID == API.Authoring.GetObjectID(otherId)) : null;
                if(otherObject is null && otherObject2 is null) continue;
                var craftingAuthoring2 = otherObject2 ? otherObject2.GetComponent<CraftingAuthoring>() : otherObject?.GetComponent<CraftingAuthoring>();
                var modCraftingAuthoring = otherObject?.GetComponent<ModCraftingAuthoring>();

                if (craftingAuthoring2?.canCraftObjects != null)
                {
                    foreach (var craftableObject2 in craftingAuthoring2.canCraftObjects)
                    {
                        AddToBuffer(new CanCraftObjectsBuffer
                        {
                            objectID = craftableObject2.objectID,
                            amount = math.max(1, craftableObject2.amount),
                            entityAmountToConsume = (craftableObject2.craftingConsumesEntityAmount ? craftableObject2.entityAmountToConsume : 0)
                        });
                    }
                    AddToBuffer(new IncludedCraftingBuildingsBuffer
                    {
                        objectID = API.Authoring.GetObjectID(otherId),
                        amountOfCraftingOptions = craftingAuthoring2.canCraftObjects.Count
                    });
                }
                else if (modCraftingAuthoring?.canCraftObjects != null)
                {
                    foreach (var craftableObject2 in modCraftingAuthoring.canCraftObjects)
                    {
                        AddToBuffer(new CanCraftObjectsBuffer
                        {
                            objectID = API.Authoring.GetObjectID(craftableObject2.objectName),
                            amount = math.max(1, craftableObject2.amount),
                            entityAmountToConsume = 0
                        });
                    }
                    AddToBuffer(new IncludedCraftingBuildingsBuffer
                    {
                        objectID = API.Authoring.GetObjectID(otherId),
                        amountOfCraftingOptions = modCraftingAuthoring.canCraftObjects.Count
                    });
                } else
                {
                    AddToBuffer(new IncludedCraftingBuildingsBuffer
                    {
                        objectID = API.Authoring.GetObjectID(otherId),
                        amountOfCraftingOptions = 0
                    });
                }
            }
        }
    }
}