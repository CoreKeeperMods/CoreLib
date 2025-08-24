using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Components
{
    /// <summary>
    /// The PoolSettings class defines configuration parameters for an object pool in the pooling system.
    /// It provides options to specify initial and maximum pool size for optimizing object reuse and performance.
    /// This component should be attached to prefabs that will be managed by the object pooling system.
    /// </summary>
    public class PoolSettings : MonoBehaviour
    {
        /// <summary>
        /// Represents the initial size of the object pool for a given prefab in the pool system.
        /// This value determines the number of objects pre-instantiated and available in the pool at startup.
        /// Adjusting this value allows for customization of performance and memory usage based on anticipated object usage.
        /// </summary>
        public int initialSize = 16;

        /// <summary>
        /// Specifies the maximum size of the pool for a given prefab in the pool system.
        /// This value determines the upper limit on how many objects can be dynamically pooled at a time.
        /// Setting this value less than or equal to the initial size ensures a fixed pool size.
        /// </summary>
        public int maxSize = 64;
    }
}