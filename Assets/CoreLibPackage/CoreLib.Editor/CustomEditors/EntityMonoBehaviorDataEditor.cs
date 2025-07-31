using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CoreLib.Editor {
    [CustomEditor(typeof(EntityMonoBehaviourData))]
    public class EntityMonoBehaviorDataEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            if (!GUILayout.Button("Migrate To Object Authoring")) return;
            var entityData = (EntityMonoBehaviourData)target;
            Convert(entityData);
            DestroyImmediate(entityData, true);
        }

        public void Convert(EntityMonoBehaviourData entityData) {
            if (entityData.TryGetComponent(out ObjectAuthoring objectAuthoring)) {
                Debug.LogWarning("Cannot convert! Prefab already contains ObjectAuthoring!");
                return;
            }

            objectAuthoring = entityData.gameObject.AddComponent<ObjectAuthoring>();
            var itemAuthoring = entityData.gameObject.AddComponent<InventoryItemAuthoring>();
            var objectInfo = entityData.objectInfo;
            
            objectAuthoring.objectName = objectInfo.objectID.ToString();
            objectAuthoring.initialAmount = objectInfo.initialAmount;
            objectAuthoring.variation = objectInfo.variation;
            objectAuthoring.variationIsDynamic = objectInfo.variationIsDynamic;
            objectAuthoring.variationToToggleTo = objectInfo.variationToToggleTo;
            objectAuthoring.objectType = objectInfo.objectType;
            objectAuthoring.tags = objectInfo.tags;
            objectAuthoring.rarity = objectInfo.rarity;
            objectAuthoring.graphicalPrefab = objectInfo.prefabInfos?[0]?.prefab?.gameObject;
            objectAuthoring.additionalSprites = objectInfo.additionalSprites;

            itemAuthoring.sellValue = objectInfo.sellValue;
            itemAuthoring.buyValueMultiplier = objectInfo.buyValueMultiplier;
            itemAuthoring.icon = objectInfo.icon;
            itemAuthoring.iconOffset = objectInfo.iconOffset;
            itemAuthoring.smallIcon = objectInfo.smallIcon;
            itemAuthoring.isStackable = objectInfo.isStackable;
            itemAuthoring.requiredObjectsToCraft = objectInfo.requiredObjectsToCraft
                .Select(item => new InventoryItemAuthoring.CraftingObject
                    { objectName = item.objectID.ToString(), amount = item.amount }).ToList();
            itemAuthoring.craftingTime = objectInfo.craftingTime;

            if (entityData.TryGetComponent(out PlaceableObjectAuthoring placeableObject)) {
                placeableObject.prefabTileSize = objectInfo.prefabTileSize;
                placeableObject.prefabCornerOffset = objectInfo.prefabCornerOffset;
                placeableObject.centerIsAtEntityPosition = objectInfo.centerIsAtEntityPosition;
            }
            
            MoveToTop(itemAuthoring);
            MoveToTop(objectAuthoring);
            Debug.Log("Successfully converted EntityMonoBehaviourData!");
        }

        public static void MoveToTop(Component newComponent) {
            while (newComponent.GetComponentIndex() != 1) {
                ComponentUtility.MoveComponentUp(newComponent);
            }
        }
    }
}