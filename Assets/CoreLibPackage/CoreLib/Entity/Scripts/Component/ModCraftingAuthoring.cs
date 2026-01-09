using System.Collections.Generic;
using System.Linq;
using CoreLib.Util.Extension;
using Pug.Conversion;
using Pug.UnityExtensions;
using PugMod;
using UnityEngine;
using NaughtyAttributes;
using Unity.Mathematics;

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

        public bool allInventoryIsForSingleCraft;

        /// <summary>
        /// Represents a collection of objects or items that can be crafted by the associated crafting component or building.
        /// Each entry in the list specifies the object ID and the quantity that can be crafted.
        /// </summary>
        [HideIf("craftingType", CraftingType.Extract)] [ArrayElementTitle("objectID, amount"), Tooltip("Objects/Items this Building can craft")]
        public List<InventoryItemAuthoring.CraftingObject> canCraftObjects = new();

        /// <summary>
        /// A list of building IDs from which crafted objects should be included for this building's crafting capabilities.
        /// </summary>
        [HideIf("craftingType", CraftingType.Extract)]
        [PickStringFromEnum(typeof(ObjectID)), Tooltip("Buildings listed below will have their items added to the crafted objects in this Building")]
        public List<string> includeCraftedObjectsFromBuildings = new();


        [ShowIf("craftingType", CraftingType.Extract)]
        public ObjectCategoryTag extractableType;

        [ShowIf("craftingType", CraftingType.Extract)]
        public Vector2 minMaxRandomDefaultExtractedOutputAmount;

        public Vector2 minMaxRandomDefaultCraftingTime;

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

            AddComponentData(new CraftingVisualCD
            {
                showLoopEffectOnOutputSlot = craftingAuthoring.showLoopEffectOnOutputSlot
            });

            int num = -1;
            if (TryGetActiveComponent(craftingAuthoring, out InventoryAuthoring _))
                num = AddToBuffer(new ContainedObjectsBuffer());
            AddComponentData(new CraftingCD
            {
                craftingType = craftingAuthoring.craftingType,
                outputSlotIndex = num,
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
                }
                else if (building.GameObject.TryGetComponent(out ModCraftingAuthoring craftingAuthoring2))
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
                objectID = (ObjectID)ObjectIndex,
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
                }
                else if (building.GameObject.TryGetComponent(out ModCraftingAuthoring craftingAuthoring2))
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

        private void SetupCrafting(ModCraftingAuthoring craftingAuthoring)
        {
            int num = -1;
            AddComponentData(new CraftingVisualCD
            {
                showLoopEffectOnOutputSlot = craftingAuthoring.showLoopEffectOnOutputSlot
            });
            if (craftingAuthoring.craftingType == CraftingType.Extract)
            {
                AddComponentData(new ExtractorCD
                {
                    extractableType = craftingAuthoring.extractableType,
                    defaultExtractionTime = craftingAuthoring.minMaxRandomDefaultCraftingTime.x,
                    defaultMinMaxRandomExtractedOutputAmount = craftingAuthoring.minMaxRandomDefaultExtractedOutputAmount
                });
            }
            else if (craftingAuthoring.craftingType == CraftingType.Incinerate)
            {
                AddComponentData(new IncineratorCD
                {
                    defaultIncinerationTime = craftingAuthoring.minMaxRandomDefaultCraftingTime.x
                });
            }
            else if (craftingAuthoring.craftingType == CraftingType.Fishing)
            {
                AddComponentData(new FishingCD
                {
                    minMaxRandomDefaultCraftingTime = craftingAuthoring.minMaxRandomDefaultCraftingTime
                });
            }
            else if (craftingAuthoring.craftingType == CraftingType.CritterCatching)
            {
                AddComponentData(new CritterCatchingCD
                {
                    minMaxRandomDefaultCraftingTime = craftingAuthoring.minMaxRandomDefaultCraftingTime
                });
            }
            else
            {
                this.SetupCanCraftBuffer(craftingAuthoring);
                InventoryAuthoring inventoryAuthoring;
                if (TryGetActiveComponent(craftingAuthoring, out inventoryAuthoring))
                {
                    num = AddToBuffer(default(ContainedObjectsBuffer));
                }
            }

            AddComponentData(new CraftingCD
            {
                craftingType = craftingAuthoring.craftingType,
                outputSlotIndex = num
            });
        }

        private void SetupCanCraftBuffer(ModCraftingAuthoring craftingAuthoring)
        {
            EnsureHasBuffer<CanCraftObjectsBuffer>();
            _cachedPrerequisites.Clear();
            _cachedCanCraftObjects.Clear();

            foreach (var craftableObject in craftingAuthoring.canCraftObjects)
            {
                AddRecipe(craftableObject);
            }

            if (craftingAuthoring.includeCraftedObjectsFromBuildings is { Count: > 0 })
            {
                foreach (var building in craftingAuthoring.includeCraftedObjectsFromBuildings
                             .Select(data => PugDatabase.entityMonobehaviours.FirstOrDefault(x => x.ObjectInfo.objectID == API.Authoring.GetObjectID(data)))
                             .Where(building => building != null))
                {
                    if (building.GameObject.TryGetComponent(out CraftingAuthoring craftingAuthoring1))
                    {
                        foreach (var canCraftObject in craftingAuthoring1.canCraftObjects)
                        {
                            AddRecipe(canCraftObject);
                        }
                    }
                    else if (building.GameObject.TryGetComponent(out ModCraftingAuthoring craftingAuthoring2))
                    {
                        foreach (var canCraftObject in craftingAuthoring2.canCraftObjects)
                        {
                            AddRecipe(canCraftObject);
                        }
                    }
                }

                EnsureHasBuffer<IncludedCraftingBuildingsBuffer>();
                AddToBuffer(new IncludedCraftingBuildingsBuffer
                {
                    objectID = (ObjectID)ObjectIndex,
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
                    }
                    else if (building.GameObject.TryGetComponent(out ModCraftingAuthoring craftingAuthoring2))
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

            foreach (CanCraftObjectsBuffer canCraftObjectsBuffer in _cachedCanCraftObjects)
            {
                AddToBuffer(canCraftObjectsBuffer);
            }

            if (NeedsPrerequisitesCheck(_cachedPrerequisites))
            {
                EnsureHasComponent<CraftingSlotsNeedPrerequisitesCheckCD>();
                SetPropertyList("Crafting/unfilteredRecipes", _cachedCanCraftObjects.ToArray());
                SetPropertyList("Crafting/recipePrerequisites", _cachedPrerequisites.ToArray());
            }
        }

        private static bool NeedsPrerequisitesCheck(List<CraftingPrerequisites> prerequisites)
        {
            foreach (CraftingPrerequisites craftingPrerequisites in prerequisites)
            {
                if (craftingPrerequisites.HasAny())
                {
                    return true;
                }
            }

            return false;
        }

        private void AddRecipe(InventoryItemAuthoring.CraftingObject recipe)
        {
            AddRecipe(new CraftingAuthoring.CraftableObject
            {
                objectID = API.Authoring.GetObjectID(recipe.objectName),
                amount = Mathf.Max(1, recipe.amount),
                entityAmountToConsume = 0,
                allowCraftingNone = false,
                craftingTime = 0,
                hasPrerequisites = false
            });
        }


        private void AddRecipe(CraftingAuthoring.CraftableObject recipe)
        {
            _cachedCanCraftObjects.Add(new CanCraftObjectsBuffer
            {
                objectID = recipe.objectID,
                amount = math.max(1, recipe.amount),
                entityAmountToConsume = (recipe.craftingConsumesEntityAmount ? recipe.entityAmountToConsume : 0),
                allowCraftingNone = recipe.allowCraftingNone,
                craftingTimeOverride = recipe.craftingTime
            });
            CraftingPrerequisites craftingPrerequisites = default(CraftingPrerequisites);
            if (recipe.hasPrerequisites)
            {
                DataBlockRef<ContentBundleDataBlock> dataBlockRef;
                OptionalValue<DataBlockAddress> optionalValue = (recipe.prerequisites.contentBundlePresent.TryGetValue(out dataBlockRef)
                    ? new OptionalValue<DataBlockAddress>(dataBlockRef.address)
                    : default(OptionalValue<DataBlockAddress>));
                DataBlockRef<ContentBundleDataBlock> dataBlockRef2;
                OptionalValue<DataBlockAddress> optionalValue2 = (recipe.prerequisites.contentBundleAbsent.TryGetValue(out dataBlockRef2)
                    ? new OptionalValue<DataBlockAddress>(dataBlockRef2.address)
                    : default(OptionalValue<DataBlockAddress>));
                craftingPrerequisites = new CraftingPrerequisites
                {
                    ContentBundlePresent = optionalValue,
                    ContentBundleAbsent = optionalValue2,
                    BirdBossKilled = recipe.prerequisites.birdBossKilled,
                    OctopusBossKilled = recipe.prerequisites.octopusBossKilled,
                    ScarabBossKilled = recipe.prerequisites.scarabBossKilled
                };
            }

            _cachedPrerequisites.Add(craftingPrerequisites);
        }


        private List<CraftingPrerequisites> _cachedPrerequisites = new List<CraftingPrerequisites>();

        private List<CanCraftObjectsBuffer> _cachedCanCraftObjects = new List<CanCraftObjectsBuffer>();
    }
}