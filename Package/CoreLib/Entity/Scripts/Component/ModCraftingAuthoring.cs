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
    /// Provides an authoring component for defining crafting capabilities within a modded entity.
    /// Responsible for configuring crafting behaviors, including crafting types, crafted objects,
    /// and integration with other buildings that contribute to crafting output.
    [DisallowMultipleComponent]
    public class ModCraftingAuthoring : MonoBehaviour
    {
        /// Specifies the type of crafting associated with an object or item.
        [Tooltip("The type of crafting that this item/object does:" +
                 "\nSimple: Make items (up to 18)" +
                 "\nProcess Resources: Make an item turn into another item" +
                 "\nBoss Statue: Activate using an item. Make Items (up to 3)" +
                 "\nCooking: Use 2 items to create a new item" +
                 "\nCattle: Object creates the item on it's own automatically")]
        public CraftingType craftingType;

        /// Determines whether a visual effect is displayed on the output slot while processing an item.
        [Tooltip("When processing an item, shows an effect on the output slot")]
        public bool showLoopEffectOnOutputSlot;

        public bool allInventoryIsForSingleCraft;

        /// Represents a collection of objects or items that can be crafted by the associated crafting component or building.
        /// Each entry in the list specifies the object ID and the quantity that can be crafted.
        [HideIf("craftingType", CraftingType.Extract)]
        [ArrayElementTitle("objectID, amount"), Tooltip("Objects/Items this Building can craft")]
        public List<InventoryItemAuthoring.CraftingObject> canCraftObjects = new();

        /// A list of building IDs from which crafted objects should be included for this building's crafting capabilities.
        [HideIf("craftingType", CraftingType.Extract)]
        [PickStringFromEnum(typeof(ObjectID)),
         Tooltip("Buildings listed below will have their items added to the crafted objects in this Building")]
        public List<string> includeCraftedObjectsFromBuildings = new();


        [ShowIf("craftingType", CraftingType.Extract)]
        public ObjectCategoryTag extractableType;

        [ShowIf("craftingType", CraftingType.Extract)]
        public Vector2 minMaxRandomDefaultExtractedOutputAmount;

        public Vector2 minMaxRandomDefaultCraftingTime;

        /// Called automatically by Unity when the state of the component changes in the Inspector.
        /// Ensures that the crafting objects list does not contain any entries with a quantity of zero or less.
        /// Any such entries are corrected to have a default quantity of one.
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

    /// Defines a converter for the ModCraftingAuthoring component, responsible for converting authoring-time
    /// data into runtime configuration for crafting functionality in the ModEntity system.
    public class ModCraftingConverter : SingleAuthoringComponentConverter<ModCraftingAuthoring>
    {
        /// Converts a ModCraftingAuthoring component into its runtime representation.
        /// This includes adding relevant components and buffers to the associated entity
        /// for crafting, handling crafting types, and processing crafting configurations.
        /// <param name="craftingAuthoring">The ModCraftingAuthoring component to convert to runtime data.</param>
        protected override void Convert(ModCraftingAuthoring craftingAuthoring)
        {
            if (craftingAuthoring.GetEntityObjectID() == ObjectID.None) return;
            int num = -1;
            AddComponentData(new CraftingVisualCD
            {
                showLoopEffectOnOutputSlot = craftingAuthoring.showLoopEffectOnOutputSlot
            });
            switch (craftingAuthoring.craftingType)
            {
                case CraftingType.Extract:
                    AddComponentData(new ExtractorCD
                    {
                        extractableType = craftingAuthoring.extractableType,
                        defaultExtractionTime = craftingAuthoring.minMaxRandomDefaultCraftingTime.x,
                        defaultMinMaxRandomExtractedOutputAmount =
                            craftingAuthoring.minMaxRandomDefaultExtractedOutputAmount
                    });
                    break;
                case CraftingType.Incinerate:
                    AddComponentData(new IncineratorCD
                    {
                        defaultIncinerationTime = craftingAuthoring.minMaxRandomDefaultCraftingTime.x
                    });
                    break;
                case CraftingType.Fishing:
                    AddComponentData(new FishingCD
                    {
                        minMaxRandomDefaultCraftingTime = craftingAuthoring.minMaxRandomDefaultCraftingTime
                    });
                    break;
                case CraftingType.CritterCatching:
                    AddComponentData(new CritterCatchingCD
                    {
                        minMaxRandomDefaultCraftingTime = craftingAuthoring.minMaxRandomDefaultCraftingTime
                    });
                    break;
                default:
                {
                    SetupCanCraftBuffer(craftingAuthoring);
                    if (TryGetActiveComponent(craftingAuthoring,
                            out InventoryAuthoring _))
                        num = AddToBuffer(new ContainedObjectsBuffer());
                    break;
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
            foreach (var canCraftObject in craftingAuthoring.canCraftObjects)
                AddRecipe(canCraftObject);
            if (craftingAuthoring.includeCraftedObjectsFromBuildings != null)
            {
                var buildings = craftingAuthoring.includeCraftedObjectsFromBuildings.Select(data => 
                        PugDatabase.entityMonobehaviours.FirstOrDefault(x => 
                            x.ObjectInfo.objectID == API.Authoring.GetObjectID(data)))
                    .Where(building => building != null).ToList();
                foreach (var objectsFromBuilding in buildings)
                {
                    if (objectsFromBuilding.GameObject.TryGetComponent(out CraftingAuthoring craftingAuthoring1))
                    {
                        foreach (var canCraftObject in craftingAuthoring1.canCraftObjects)
                        {
                            AddRecipe(canCraftObject);
                        }
                    }
                    else if (objectsFromBuilding.GameObject.TryGetComponent(out ModCraftingAuthoring craftingAuthoring2))
                    {
                        foreach (var canCraftObject in craftingAuthoring2.canCraftObjects)
                        {
                            AddRecipe(canCraftObject);
                        }
                    }
                    else
                    {
                        Debug.LogError(craftingAuthoring.name + ": null in includeCraftedObjectsFromBuildings");
                    }
                }

                if (craftingAuthoring.includeCraftedObjectsFromBuildings.Count > 0)
                {
                    EnsureHasBuffer<IncludedCraftingBuildingsBuffer>();
                    AddToBuffer(new IncludedCraftingBuildingsBuffer
                    {
                        objectID = (ObjectID)ObjectIndex,
                        amountOfCraftingOptions = craftingAuthoring.canCraftObjects.Count
                    });
                    foreach (var objectsFromBuilding in buildings)
                    {
                        var objectId = objectsFromBuilding.GameObject.GetEntityObjectID();
                        if (objectId == ObjectID.None)
                        {
                            Debug.LogError($"{craftingAuthoring.name}: Building ObjectID set to None or Not Found.");
                            continue;
                        }
                        if (objectsFromBuilding.GameObject.TryGetComponent(out CraftingAuthoring craftingAuthoring1))
                        {
                            AddToBuffer(new IncludedCraftingBuildingsBuffer
                            {
                                objectID = objectId,
                                amountOfCraftingOptions = craftingAuthoring1.canCraftObjects.Count
                            });
                        }
                        else if (objectsFromBuilding.GameObject.TryGetComponent(out ModCraftingAuthoring craftingAuthoring2))
                        {
                            AddToBuffer(new IncludedCraftingBuildingsBuffer
                            {
                                objectID = objectId,
                                amountOfCraftingOptions = craftingAuthoring2.canCraftObjects.Count
                            });
                        }
                    }
                }
            }

            foreach (var cachedCanCraftObject in _cachedCanCraftObjects)
                AddToBuffer(cachedCanCraftObject);
            if (!NeedsPrerequisitesCheck(_cachedPrerequisites))
                return;
            EnsureHasComponent<CraftingSlotsNeedPrerequisitesCheckCD>();
            SetPropertyList("Crafting/unfilteredRecipes",
                _cachedCanCraftObjects.ToArray());
            SetPropertyList("Crafting/recipePrerequisites",
                _cachedPrerequisites.ToArray());
        }

        private static bool NeedsPrerequisitesCheck(List<CraftingPrerequisites> prerequisites)
        {
            foreach (var prerequisite in prerequisites)
            {
                if (prerequisite.HasAny())
                    return true;
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
                entityAmountToConsume = recipe.craftingConsumesEntityAmount ? recipe.entityAmountToConsume : 0,
                allowCraftingNone = recipe.allowCraftingNone,
                craftingTimeOverride = recipe.craftingTime
            });
            CraftingPrerequisites craftingPrerequisites = new CraftingPrerequisites();
            if (recipe.hasPrerequisites)
            {
                var optionalValue1 = recipe.prerequisites.contentBundlePresent.TryGetValue(out var output1) ? new OptionalValue<DataBlockAddress>(output1.address) : new OptionalValue<DataBlockAddress>();
                var optionalValue2 = recipe.prerequisites.contentBundleAbsent.TryGetValue(out var output2) ? new OptionalValue<DataBlockAddress>(output2.address) : new OptionalValue<DataBlockAddress>();
                craftingPrerequisites = new CraftingPrerequisites
                {
                    ContentBundlePresent = optionalValue1,
                    ContentBundleAbsent = optionalValue2,
                    BirdBossKilled = recipe.prerequisites.birdBossKilled,
                    OctopusBossKilled = recipe.prerequisites.octopusBossKilled,
                    ScarabBossKilled = recipe.prerequisites.scarabBossKilled,
                    HydraBossNatureKilled = recipe.prerequisites.hydraBossNatureKilled,
                    HydraBossSeaKilled = recipe.prerequisites.hydraBossSeaKilled,
                    HydraBossDesertKilled = recipe.prerequisites.hydraBossDesertKilled
                };
            }
            _cachedPrerequisites.Add(craftingPrerequisites);
        }


        private readonly List<CraftingPrerequisites> _cachedPrerequisites = new();

        private readonly List<CanCraftObjectsBuffer> _cachedCanCraftObjects = new();
    }
}