using PugMod;
using UnityEngine;

namespace CoreLib.Util.Extensions
{
    /// <summary>
    /// Provides extension methods for retrieving data associated with Unity objects.
    /// </summary>
    public static class ObjectDataExtensions
    {
        /// Retrieves the variation associated with the specified MonoBehaviour.
        /// <param name="monoBehaviour">
        /// The MonoBehaviour for which the variation is to be retrieved. This parameter must not be null.
        /// </param>
        /// <returns>
        /// The variation associated with the specified MonoBehaviour.
        /// Returns 0 if no variation is found.
        /// </returns>
        public static int GetEntityVariation(this MonoBehaviour monoBehaviour)
        {
            return GetEntityVariation(monoBehaviour.gameObject);
        }

        /// Retrieves the variation associated with the specified MonoBehaviour.
        /// <param name="monoBehaviour">
        /// The MonoBehaviour for which the variation is to be retrieved. This parameter must not be null.
        /// </param>
        /// <returns>
        /// The variation associated with the specified MonoBehaviour.
        /// Returns 0 if no variation is found.
        /// </returns>
        public static int GetEntityVariation(this GameObject gameObject)
        {
            var entityMonoBehaviorData = gameObject.GetComponent<EntityMonoBehaviourData>();
            var objectAuthoring = gameObject.GetComponent<ObjectAuthoring>();
                
            if (entityMonoBehaviorData != null)
            {
                return entityMonoBehaviorData.objectInfo.variation;
            }
            if (objectAuthoring != null)
            {
                return objectAuthoring.variation;
            }

            return 0;
        }

        /// Retrieves the ObjectID associated with the specified MonoBehaviour.
        /// <param name="monoBehaviour">
        /// The MonoBehaviour for which the ObjectID is to be retrieved. This parameter must not be null.
        /// </param>
        /// <returns>
        /// The ObjectID associated with the specified MonoBehaviour.
        /// If no ObjectID is found, a default or invalid ObjectID is returned.
        /// </returns>
        public static ObjectID GetEntityObjectID(this MonoBehaviour monoBehaviour)
        {
            return GetEntityObjectID(monoBehaviour.gameObject);
        }

        /// Retrieves the ObjectID associated with the specified MonoBehaviour.
        /// <param name="monoBehaviour">
        /// The MonoBehaviour for which the ObjectID is to be retrieved. This parameter must not be null.
        /// </param>
        /// <returns>
        /// The ObjectID associated with the specified MonoBehaviour.
        /// If no ObjectID is found, a default or invalid ObjectID is returned.
        /// </returns>
        public static ObjectID GetEntityObjectID(this GameObject gameObject)
        {
            var entityMonoBehaviorData = gameObject.GetComponent<EntityMonoBehaviourData>();
            var objectAuthoring = gameObject.GetComponent<ObjectAuthoring>();
                
            if (entityMonoBehaviorData != null)
            {
                return entityMonoBehaviorData.objectInfo.objectID;
            }
            if (objectAuthoring != null)
            {
                return API.Authoring.GetObjectID(objectAuthoring.objectName);
            }

            return 0;
        }
        
        
        
    }
}