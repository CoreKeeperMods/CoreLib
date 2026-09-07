// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: SupportsCoreLib.cs
// Author: Minepatcher, Limoka, 
// Created: 2025-11-21
// Description: Marks a GameObject as a Core Lib entity.
// ========================================================

using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Component {
    /// This component is used to identify the entity as a Core Lib entity.
    public class SupportsCoreLib : MonoBehaviour
    {
        [Tooltip("Bind this entity to the Core Library's Root Workbench.")]
        public bool bindToRootWorkbench;
        
        [Tooltip("The amount of this entity to create when crafting.")]
        public int amount = 1;
        
        public string EntityName => gameObject.GetComponent<ObjectAuthoring>().objectName;
        
        public string ModID => EntityName.Split(':')[0];

        public InventoryItemAuthoring.CraftingObject GetCraftingObject()
        {
            return new InventoryItemAuthoring.CraftingObject
            {
                objectName = EntityName,
                amount = amount
            };
        }
    }
}