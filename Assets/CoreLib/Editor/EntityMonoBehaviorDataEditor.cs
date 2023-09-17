using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CoreLib.Editor
{
    [CustomEditor(typeof(EntityMonoBehaviourData))]
    public class EntityMonoBehaviorDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Migrate To Object Authoring"))
            {
                var entityData = (EntityMonoBehaviourData)target;
                Convert(entityData);
            }
        }

        private void Convert(EntityMonoBehaviourData entityData)
        {
            var objectAuthoring = entityData.GameObject.GetComponent<ObjectAuthoring>();
            if (objectAuthoring != null)
            {
                Debug.LogWarning("Cannot convert! Prefab already contains ObjectAuthoring!");
                return;
            }

            objectAuthoring = entityData.GameObject.AddComponent<ObjectAuthoring>();
            var itemAuthoring = entityData.GameObject.AddComponent<InventoryItemAuthoring>();

            objectAuthoring.objectID = (int)entityData.objectInfo.objectID;
            objectAuthoring.initialAmount = entityData.objectInfo.initialAmount;
            objectAuthoring.variation = entityData.objectInfo.variation;
            objectAuthoring.variationIsDynamic = entityData.objectInfo.variationIsDynamic;
            objectAuthoring.variationToToggleTo = entityData.objectInfo.variationToToggleTo;
            objectAuthoring.objectType = entityData.objectInfo.objectType;
            objectAuthoring.tags = entityData.objectInfo.tags.ToList();
            objectAuthoring.rarity = entityData.objectInfo.rarity;

            if (entityData.objectInfo.prefabInfos.Count > 0 &&
                entityData.objectInfo.prefabInfos[0].prefab != null)
            {
                objectAuthoring.graphicalPrefab = entityData.objectInfo.prefabInfos[0].prefab.gameObject;
            }

            objectAuthoring.additionalSprites = entityData.objectInfo.additionalSprites.ToList();

            itemAuthoring.onlyExistsInSeason = (int)entityData.objectInfo.onlyExistsInSeason;
            itemAuthoring.sellValue = entityData.objectInfo.sellValue;
            itemAuthoring.buyValueMultiplier = entityData.objectInfo.buyValueMultiplier;
            itemAuthoring.icon = entityData.objectInfo.icon;
            itemAuthoring.iconOffset = entityData.objectInfo.iconOffset;
            itemAuthoring.smallIcon = entityData.objectInfo.smallIcon;
            itemAuthoring.isStackable = entityData.objectInfo.isStackable;
            itemAuthoring.craftingSettings = entityData.objectInfo.craftingSettings;
            itemAuthoring.requiredObjectsToCraft = entityData.objectInfo.requiredObjectsToCraft
                .Select(item => new InventoryItemAuthoring.CraftingObject()
                {
                    objectID = (int)item.objectID,
                    amount = item.amount
                }).ToList();
            itemAuthoring.craftingTime = entityData.objectInfo.craftingTime;

            var placeableObject = entityData.GameObject.GetComponent<PlaceableObjectAuthoring>();

            if (placeableObject != null)
            {
                placeableObject.prefabTileSize = entityData.objectInfo.prefabTileSize;
                placeableObject.prefabCornerOffset = entityData.objectInfo.prefabCornerOffset;
                placeableObject.centerIsAtEntityPosition = entityData.objectInfo.centerIsAtEntityPosition;
            }

            MoveToTop(itemAuthoring);
            MoveToTop(objectAuthoring);
        }

        private static void MoveToTop(Component newComponent)
        {
            for (int i = 0; i < 20; i++)
            {
                bool result = UnityEditorInternal.ComponentUtility.MoveComponentUp(newComponent);
                if (!result) break;
            }
        }
    }
}