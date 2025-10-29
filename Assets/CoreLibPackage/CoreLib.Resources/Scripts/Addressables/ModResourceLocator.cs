using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace CoreLib.ModResources
{
    public class ModResourceLocator : IResourceLocator
    {
        internal const string PROTOCOL = "mod:";
        internal const string NAMESPACE = "CoreLib.ModResources.";

        public IEnumerable<IResourceLocation> AllLocations { get; }

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

        public string LocatorId => NAMESPACE + nameof(ModResourceLocator);
      
        public IEnumerable<object> Keys => Array.Empty<object>();
    }
}