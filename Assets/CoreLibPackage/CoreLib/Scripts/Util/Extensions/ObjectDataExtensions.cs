using PugMod;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Util.Extensions
{
    /// Provides extension methods for retrieving data associated with Unity objects.
    public static class ObjectDataExtensions
    {
        /// <returns>The variation associated with the specified MonoBehaviour. Returns 0 if no variation is found.</returns>
        public static int GetEntityVariation(this MonoBehaviour monoBehaviour)
        {
            return GetEntityVariation(monoBehaviour.gameObject);
        }
        
        /// <returns>The variation associated with the specified GameObject. Returns 0 if no variation is found.</returns>
        public static int GetEntityVariation(this GameObject gameObject)
        {
            if(gameObject.TryGetComponent(out EntityMonoBehaviourData entityMonoBehaviorData)) return entityMonoBehaviorData.objectInfo.variation;
            
            return gameObject.TryGetComponent(out ObjectAuthoring objectAuthoring) ? objectAuthoring.variation : 0;
        }
        
        /// <returns>The ObjectID associated with the specified MonoBehaviour. If no ObjectID is found, <see cref="ObjectID.None"/> is returned.</returns>
        public static ObjectID GetEntityObjectID(this MonoBehaviour monoBehaviour)
        {
            return GetEntityObjectID(monoBehaviour.gameObject);
        }

        /// <returns>The ObjectID associated with the specified GameObject. If no ObjectID is found, <see cref="ObjectID.None"/> is returned.</returns>
        public static ObjectID GetEntityObjectID(this GameObject gameObject)
        {
            if(gameObject.TryGetComponent(out EntityMonoBehaviourData entityMonoBehaviorData)) return entityMonoBehaviorData.objectInfo.objectID;

            return gameObject.TryGetComponent(out ObjectAuthoring objectAuthoring) ? API.Authoring.GetObjectID(objectAuthoring.objectName) : ObjectID.None;
        }
        
        
        
    }
}