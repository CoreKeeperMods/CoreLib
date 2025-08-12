using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity.Components
{
    /// <summary>
    /// A base class for handling custom data authoring and modifications in Unity.
    /// Facilitates the application of custom data or related modifications to a target MonoBehaviour instance.
    /// </summary>
    public class ModCDAuthoringBase : MonoBehaviour
    {
        /// <summary>
        /// Applies custom data or modifications to the provided MonoBehaviour instance.
        /// </summary>
        /// <param name="data">The MonoBehaviour instance to which the custom data or modifications will be applied.</param>
        /// <returns>
        /// Returns a boolean indicating whether the application process was successful.
        /// </returns>
        public virtual bool Apply(MonoBehaviour data)
        {
            return default(bool);
        }
    }
}