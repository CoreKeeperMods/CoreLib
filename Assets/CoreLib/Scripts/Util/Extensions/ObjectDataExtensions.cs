using UnityEngine;

namespace CoreLib.Scripts.Util.Extensions
{
    public static class ObjectDataExtensions
    {
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

        public static ObjectID GetEntityObjectID(this MonoBehaviour monoBehaviour)
        {
            return GetEntityObjectID(monoBehaviour.gameObject);
        }

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
                return (ObjectID)objectAuthoring.objectID;
            }

            return 0;
        }
        
    }
}