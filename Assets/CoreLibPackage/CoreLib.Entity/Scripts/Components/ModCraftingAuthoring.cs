using System.Collections.Generic;
using System.Linq;
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

        [PickStringFromEnum(typeof (ObjectID))]
        public List<string> includeCraftedObjectsFromBuildings;
        
        private void OnValidate()
        {
            for (var index = 0; index < canCraftObjects.Count; ++index)
            {
                if (canCraftObjects[index].amount > 0) continue;
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
            var outputSlotIndex = -1;
            if (authoring.craftingType != CraftingType.Cattle)
            {
                if(!TryGetActiveComponent(authoring, out InventoryAuthoring _))
                {
                    Debug.LogError($"{authoring.gameObject} has non-cattle crafting but no inventory.");
                    return;
                }
                outputSlotIndex = AddToBuffer(new ContainedObjectsBuffer());
            }
            AddComponentData(new CraftingCD()
            {
                currentlyCraftingIndex = -1,
                craftingType = authoring.craftingType,
                outputSlotIndex = outputSlotIndex,
                showLoopEffectOnOutputSlot = authoring.showLoopEffectOnOutputSlot
            });
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
            
            foreach (var building in authoring.includeCraftedObjectsFromBuildings)
            {
                var monoObject = PugDatabase.entityMonobehaviours.Find(mono => 
                    mono.ObjectInfo.objectID == API.Authoring.GetObjectID(building));
                CraftingAuthoring craftingAuthoring = null;
                ModCraftingAuthoring modCraftingAuthoring = null;
                switch (monoObject)
                {
                    case EntityMonoBehaviourData monoAuthoring:
                        craftingAuthoring = monoAuthoring.GetComponent<CraftingAuthoring>();
                        break;
                    case ObjectAuthoring objectAuthoring:
                        modCraftingAuthoring = objectAuthoring.GetComponent<ModCraftingAuthoring>();
                        craftingAuthoring = !modCraftingAuthoring ? objectAuthoring.GetComponent<CraftingAuthoring>() : null;
                        break;
                    case null:
                        continue;
                }

                if (modCraftingAuthoring?.canCraftObjects != null)
                {
                    foreach (var craftableObject in modCraftingAuthoring.canCraftObjects)
                    {
                        AddToBuffer(new CanCraftObjectsBuffer
                        {
                            objectID = API.Authoring.GetObjectID(craftableObject.objectName),
                            amount = math.max(1, craftableObject.amount),
                            entityAmountToConsume = 0
                        });
                    }
                    AddToBuffer(new IncludedCraftingBuildingsBuffer
                    {
                        objectID = API.Authoring.GetObjectID(building),
                        amountOfCraftingOptions = modCraftingAuthoring.canCraftObjects.Count
                    });
                    continue;
                }

                if (craftingAuthoring?.canCraftObjects != null)
                {
                    foreach (var craftableObject in craftingAuthoring.canCraftObjects)
                    {
                        AddToBuffer(new CanCraftObjectsBuffer
                        {
                            objectID = craftableObject.objectID,
                            amount = math.max(1, craftableObject.amount),
                            entityAmountToConsume = (craftableObject.craftingConsumesEntityAmount ? craftableObject.entityAmountToConsume : 0)
                        });
                    }
                    AddToBuffer(new IncludedCraftingBuildingsBuffer
                    {
                        objectID = API.Authoring.GetObjectID(building),
                        amountOfCraftingOptions = craftingAuthoring.canCraftObjects.Count
                    }); 
                }
            }
        }
    }
}