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
    /// <summary>
    /// Marks a GameObject as capable of being pooled.
    /// </summary>
    public class SupportsPooling : MonoBehaviour {
        /// <summary>
        /// The initial size of the pool for this GameObject.
        /// </summary>
        [Tooltip("The initial size of the pool for this GameObject.")]
        public int initialSize = 16;
        /// <summary>
        /// The maximum number of free objects in the pool for this GameObject.
        /// </summary>
        [Tooltip("The maximum number of free objects in the pool for this GameObject.")]
        public int maxFreeSize = 16;
        /// <summary>
        /// The maximum size of the pool for this GameObject.
        /// </summary>
        [Tooltip("The maximum size of the pool for this GameObject.")]
        public int maxSize = 1024;

        /// <summary>
        /// Returns a PoolablePrefab representation of this GameObject.
        /// </summary>
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