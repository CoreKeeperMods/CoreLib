// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: ObjectDataExtensions.cs
// Author: Minepatcher, Limoka
// Created: 2025-11-07
// Description: Provides extension methods for retrieving Core Keeper object metadata,
//              including ObjectID and variation data from Unity components.
// ========================================================

using PugMod;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Util.Extension
{
    /// <summary>
    /// Provides extension methods for retrieving metadata associated with in-game objects and entities.
    /// </summary>
    /// <remarks>
    /// These extensions simplify access to <see cref="EntityMonoBehaviourData"/> and <see cref="ObjectAuthoring"/>
    /// information attached to Unity objects, providing an easy way to get object IDs and variation indices.
    /// </remarks>
    /// <seealso cref="EntityMonoBehaviourData"/>
    /// <seealso cref="ObjectAuthoring"/>
    public static class ObjectDataExtensions
    {
        #region Entity Variation Retrieval

        /// <summary>
        /// Retrieves the entity variation index for a given <see cref="MonoBehaviour"/>.
        /// </summary>
        /// <param name="monoBehaviour">The MonoBehaviour whose associated entity variation should be retrieved.</param>
        /// <returns>
        /// The variation value of the associated entity. Returns <c>0</c> if no variation is found.
        /// </returns>
        /// <remarks>
        /// This method internally delegates to <see cref="GetEntityVariation(GameObject)"/> for consistency.
        /// </remarks>
        /// <seealso cref="GetEntityVariation(GameObject)"/>
        public static int GetEntityVariation(this MonoBehaviour monoBehaviour)
        {
            return GetEntityVariation(monoBehaviour.gameObject);
        }

        /// <summary>
        /// Retrieves the entity variation index for a given <see cref="GameObject"/>.
        /// </summary>
        /// <param name="gameObject">The GameObject whose variation index should be retrieved.</param>
        /// <returns>
        /// The entity variation index, or <c>0</c> if the object does not contain relevant component data.
        /// </returns>
        /// <remarks>
        /// Checks for the presence of an <see cref="EntityMonoBehaviourData"/> component first.
        /// If none is found, attempts to resolve via <see cref="ObjectAuthoring"/>.
        /// </remarks>
        /// <seealso cref="EntityMonoBehaviourData"/>
        /// <seealso cref="ObjectAuthoring"/>
        public static int GetEntityVariation(this GameObject gameObject) =>
            gameObject.TryGetComponent(out EntityMonoBehaviourData entityData)
                ? entityData.objectInfo.variation
                : gameObject.TryGetComponent(out ObjectAuthoring authoring)
                    ? authoring.variation
                    : 0;

        #endregion

        #region Entity ObjectID Retrieval

        /// <summary>
        /// Retrieves the <see cref="ObjectID"/> associated with the given <see cref="MonoBehaviour"/>.
        /// </summary>
        /// <param name="monoBehaviour">The MonoBehaviour from which to retrieve the ObjectID.</param>
        /// <returns>
        /// The associated <see cref="ObjectID"/>. Returns <see cref="ObjectID.None"/> if unavailable.
        /// </returns>
        /// <remarks>
        /// This method delegates to <see cref="GetEntityObjectID(GameObject)"/> to perform component-based lookup.
        /// </remarks>
        /// <seealso cref="GetEntityObjectID(GameObject)"/>
        /// <seealso cref="ObjectID"/>
        public static ObjectID GetEntityObjectID(this MonoBehaviour monoBehaviour)
        {
            return GetEntityObjectID(monoBehaviour.gameObject);
        }

        /// <summary>
        /// Retrieves the <see cref="ObjectID"/> associated with a given <see cref="GameObject"/>.
        /// </summary>
        /// <param name="gameObject">The GameObject to query for object identification data.</param>
        /// <returns>
        /// The corresponding <see cref="ObjectID"/> if available; otherwise, <see cref="ObjectID.None"/>.
        /// </returns>
        /// <remarks>
        /// This method first checks for <see cref="EntityMonoBehaviourData"/> to retrieve the
        /// stored <see cref="ObjectInfo"/> structure. If unavailable, it falls back to an
        /// <see cref="ObjectAuthoring"/> lookup through <see cref="ModAPIAuthoring.GetObjectID(string)"/>.
        /// </remarks>
        /// <seealso cref="EntityMonoBehaviourData"/>
        /// <seealso cref="ObjectAuthoring"/>
        /// <seealso cref="ModAPIAuthoring.GetObjectID(string)"/>
        public static ObjectID GetEntityObjectID(this GameObject gameObject) =>
            gameObject.TryGetComponent(out EntityMonoBehaviourData entityData)
                ? entityData.objectInfo.objectID
                : gameObject.TryGetComponent(out ObjectAuthoring authoring)
                    ? API.Authoring.GetObjectID(authoring.objectName)
                    : ObjectID.None;

        #endregion
    }
}