// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: SupportsPooling.cs
// Author: Minepatcher, Limoka, Moorowl
// Created: 2025-11-19
// Description: Marks a GameObject as capable of being pooled.
// ========================================================

/* Edited from Moorowl's Paintable Double Chest https://mod.io/g/corekeeper/m/doublechest#description */
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Component {
    /// Marks a GameObject as capable of being pooled.
    public class SupportsPooling : MonoBehaviour {
        /// The initial size of the pool for this GameObject.
        [Tooltip("The initial size of the pool for this GameObject.")]
        public int initialSize = 16;
        /// The maximum number of free objects in the pool for this GameObject.
        [Tooltip("The maximum number of free objects in the pool for this GameObject.")]
        public int maxFreeSize = 16;
        /// The maximum size of the pool for this GameObject.
        [Tooltip("The maximum size of the pool for this GameObject.")]
        public int maxSize = 1024;

        /// Returns a PoolablePrefab representation of this GameObject.
        /// <returns>A PoolablePrefab representation of this GameObject.</returns>
        public PoolablePrefabBank.PoolablePrefab GetPoolablePrefab() {
            return new PoolablePrefabBank.PoolablePrefab {
                prefab = gameObject,
                initialSize = initialSize,
                maxFreeSize = maxFreeSize,
                maxSize = maxSize
            };
        }
    }
}