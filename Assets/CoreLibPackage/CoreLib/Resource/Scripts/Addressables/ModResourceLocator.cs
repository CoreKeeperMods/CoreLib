using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Resource
{
    /// <summary>
    /// Implements a resource locator for mod-based assets, enabling the
    /// ability to locate resources addressed with a "mod:" protocol.
    /// </summary>
    /// <remarks>
    /// This class is designed to integrate with Unity's Addressables system,
    /// allowing modded assets to be handled seamlessly alongside standard resources.
    /// </remarks>
    public class ModResourceLocator : IResourceLocator
    {
        /// <summary>
        /// Represents the protocol identifier used to denote resources that are associated with mods in the system.
        /// This constant is used as a prefix in resource keys to signify that the resource should be resolved
        /// using the custom mod resource locator logic.
        /// </summary>
        internal const string PROTOCOL = "mod:";

        /// <summary>
        /// Represents the namespace identifier used internally within the ModResourceLocator
        /// to specify the context or scope of resource providers and locators for the mod resources.
        /// </summary>
        /// <remarks>
        /// The NAMESPACE variable is a constant string defined as "CoreLib.ModResources."
        /// and serves as a namespace marker for resource types and resource location identifiers
        /// managed by the ModResourceLocator and ModResourceProvider.
        /// It is utilized to differentiate resources specific to the CoreLib ModResources system.
        /// </remarks>
        internal const string NAMESPACE = "CoreLib.ModResources.";

        /// <summary>
        /// Attempts to locate the requested resource using the provided key and type,
        /// returning corresponding resource locations if found.
        /// </summary>
        /// <param name="key">
        /// The key or identifier of the resource to locate. Typically a string in the format "mod:<path>".
        /// </param>
        /// <param name="type">
        /// The type of the resource being located.
        /// </param>
        /// <param name="locations">
        /// When this method returns, contains a list of <see cref="IResourceLocation"/> objects
        /// corresponding to the located resource, or null if the resource could not be found.
        /// </param>
        /// <returns>
        /// True if the resource was successfully located, otherwise false.
        /// </returns>
        public bool Locate(object key, Type type, out IList<IResourceLocation> locations)
        {
            if (key is string stringKey && stringKey.StartsWith(PROTOCOL))
            {
                string path = stringKey.Substring(PROTOCOL.Length);
              
                // Build the resource location with the url provider
                locations = new List<IResourceLocation>(1)
                {
                    new ResourceLocationBase(
                        "MOD-" + path,
                        path,
                        NAMESPACE + nameof(ModResourceProvider),
                        type)
                };

                return true;
            }
          
            locations = null;
            return false;
        }

        /// <summary>
        /// Gets the unique identifier for the resource locator.
        /// This identifier is used to distinguish this specific resource locator
        /// within the resource management system, ensuring the correct resources are
        /// identified and resolved.
        /// </summary>
        public string LocatorId => NAMESPACE + nameof(ModResourceLocator);

        /// <summary>
        /// Gets the collection of keys representing the resources that this locator can provide.
        /// This collection is used for resource location lookups and should contain all the keys
        /// that can be resolved by the <see cref="Locate"/> method of the <see cref="ModResourceLocator"/>.
        /// </summary>
        public IEnumerable<object> Keys => Array.Empty<object>();
    }
}