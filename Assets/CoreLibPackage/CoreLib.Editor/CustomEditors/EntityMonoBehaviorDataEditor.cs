using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CoreLib.Editor {
    
    /// <summary>Custom editor class for managing the EntityMonoBehaviourData component in the Unity Inspector.</summary>
    [CustomEditor(typeof(EntityMonoBehaviourData))]
    public class EntityMonoBehaviourDataEditor : UnityEditor.Editor
    {
        private const string MigrateButtonLabel = "Migrate To Object Authoring";
        private const string AlreadyHasObjectAuthoringWarning = "Cannot convert! Prefab already contains ObjectAuthoring!";
        private const string NullObjectInfoWarning = "Cannot convert! 'objectInfo' is null.";

        /// <summary>Draws the custom inspector GUI for the EntityMonoBehaviourData component.</summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!GUILayout.Button(MigrateButtonLabel)) return;

            var entityData = (EntityMonoBehaviourData)target;
            if (MigrateToAuthoring(entityData))
                Undo.DestroyObjectImmediate(entityData);
        }

        /// <summary>
        /// Migrates an EntityMonoBehaviourData component into ObjectAuthoring and InventoryItemAuthoring components,
        /// transferring relevant data and removing the original EntityMonoBehaviourData component.
        /// </summary>
        /// <param name="entityData">The EntityMonoBehaviourData component to be migrated. This parameter must not be null.</param>
        /// <returns>Returns true if the migration process completes successfully; otherwise, returns false.</returns>
        private static bool MigrateToAuthoring(EntityMonoBehaviourData entityData) {
            if (entityData == null) return false;

            if (entityData.TryGetComponent(out ObjectAuthoring _)) {
                Debug.LogWarning(AlreadyHasObjectAuthoringWarning);
                return false;
            }

            var objectInfo = entityData.objectInfo;
            if (objectInfo == null) {
                Debug.LogWarning(NullObjectInfoWarning);
                return false;
            }

            var go = entityData.gameObject;

            // Create components with Undo support
            var objectAuthoring = Undo.AddComponent<ObjectAuthoring>(go);
            var itemAuthoring = Undo.AddComponent<InventoryItemAuthoring>(go);

            AssignObjectAuthoringFields(objectAuthoring, objectInfo);
            AssignInventoryItemAuthoringFields(itemAuthoring, objectInfo);

            if (entityData.TryGetComponent(out PlaceableObjectAuthoring placeableObject)) {
                AssignPlaceableObjectFields(placeableObject, objectInfo);
                EditorUtility.SetDirty(placeableObject);
            }

            // Keep new components visible at the top
            MoveToTop(itemAuthoring);
            MoveToTop(objectAuthoring);

            EditorUtility.SetDirty(objectAuthoring);
            EditorUtility.SetDirty(itemAuthoring);

            Debug.Log("Successfully converted EntityMonoBehaviourData!");
            return true;
        }

        /// <summary>Assigns values from the ObjectInfo instance to the corresponding fields in the ObjectAuthoring component.</summary>
        /// <param name="objectAuthoring">The target ObjectAuthoring component to which the data will be assigned.</param>
        /// <param name="objectInfo">The source ObjectInfo instance containing the data to be assigned.</param>
        private static void AssignObjectAuthoringFields(ObjectAuthoring objectAuthoring, ObjectInfo objectInfo) {
            Undo.RecordObject(objectAuthoring, "Assign ObjectAuthoring Fields");
            objectAuthoring.objectName = objectInfo.objectID.ToString();
            objectAuthoring.initialAmount = objectInfo.initialAmount;
            objectAuthoring.variation = objectInfo.variation;
            objectAuthoring.variationIsDynamic = objectInfo.variationIsDynamic;
            objectAuthoring.variationToToggleTo = objectInfo.variationToToggleTo;
            objectAuthoring.objectType = objectInfo.objectType;
            objectAuthoring.tags = objectInfo.tags;
            objectAuthoring.rarity = objectInfo.rarity;

            // Safer access for first prefab info (handles null and empty arrays)
            var firstPrefabInfo = objectInfo.prefabInfos?.FirstOrDefault();
            objectAuthoring.graphicalPrefab = firstPrefabInfo?.prefab?.gameObject;

            objectAuthoring.additionalSprites = objectInfo.additionalSprites;
        }

        /// <summary>Assigns the necessary fields from the ObjectInfo to the InventoryItemAuthoring component.</summary>
        /// <param name="itemAuthoring">The InventoryItemAuthoring component to be updated with data.</param>
        /// <param name="objectInfo">The ObjectInfo containing the relevant properties to assign to InventoryItemAuthoring.</param>
        private static void AssignInventoryItemAuthoringFields(InventoryItemAuthoring itemAuthoring, ObjectInfo objectInfo) {
            Undo.RecordObject(itemAuthoring, "Assign InventoryItemAuthoring Fields");
            itemAuthoring.sellValue = objectInfo.sellValue;
            itemAuthoring.buyValueMultiplier = objectInfo.buyValueMultiplier;
            itemAuthoring.icon = objectInfo.icon;
            itemAuthoring.iconOffset = objectInfo.iconOffset;
            itemAuthoring.smallIcon = objectInfo.smallIcon;
            itemAuthoring.isStackable = objectInfo.isStackable;

            itemAuthoring.requiredObjectsToCraft = objectInfo.requiredObjectsToCraft?
                .Select(item => new InventoryItemAuthoring.CraftingObject {
                    objectName = item.objectID.ToString(),
                    amount = item.amount
                })
                .ToList() ?? new List<InventoryItemAuthoring.CraftingObject>();

            itemAuthoring.craftingTime = objectInfo.craftingTime;
        }

        /// <summary>Assigns the fields of a PlaceableObjectAuthoring component from the provided ObjectInfo instance.</summary>
        /// <param name="placeableObject">The PlaceableObjectAuthoring component to update with the field values.</param>
        /// <param name="objectInfo">The ObjectInfo instance containing the data to assign to the PlaceableObjectAuthoring fields.</param>
        private static void AssignPlaceableObjectFields(PlaceableObjectAuthoring placeableObject, ObjectInfo objectInfo) {
            Undo.RecordObject(placeableObject, "Assign PlaceableObjectAuthoring Fields");
            placeableObject.prefabTileSize = objectInfo.prefabTileSize;
            placeableObject.prefabCornerOffset = objectInfo.prefabCornerOffset;
            placeableObject.centerIsAtEntityPosition = objectInfo.centerIsAtEntityPosition;
        }

        /// <summary>Moves the specified component to the top of the Unity Inspector's component list.</summary>
        /// <param name="newComponent">The component to be repositioned at the top of the Inspector component hierarchy.</param>
        private static void MoveToTop(Component newComponent)
        {
            while (newComponent.GetComponentIndex() != 1)
            {
                ComponentUtility.MoveComponentUp(newComponent);
            }
        }
    }
}