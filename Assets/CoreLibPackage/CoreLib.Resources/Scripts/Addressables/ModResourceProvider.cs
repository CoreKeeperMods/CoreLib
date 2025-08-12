using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace CoreLib.ModResources
{
    /// <summary>
    /// A resource provider for loading mod-based assets within a Unity Addressables system.
    /// </summary>
    /// <remarks>
    /// This provider is designed to handle assets loaded through a custom mod-based resource
    /// locator. It supports various asset types such as GameObjects, ScriptableObjects,
    /// Materials, Sprites, and AudioClips. The provider processes asset loading requests and
    /// completes the handle with the loaded asset or an appropriate response.
    /// </remarks>
    /// <inheritDoc />
    public class ModResourceProvider : ResourceProviderBase
    {
        /// <summary>
        /// Provides a resource to be loaded based on the specified provide handle.
        /// </summary>
        /// <param name="provideHandle">The handle containing the information about the resource to be provided, including its location and type.</param>
        public override void Provide(ProvideHandle provideHandle)
        {
            // Get the url from the IResourceLocation built by the ResourceLocator
            string path = provideHandle.Location.InternalId;
            var type = provideHandle.Location.ResourceType;

            var asset = ResourcesModule.LoadAsset(path, type);

            // Just in case addressables need T to be correct
            switch (asset)
            {
                case GameObject go:
                    provideHandle.Complete(go, go != null, null);
                    break;
                case ScriptableObject so:
                    provideHandle.Complete(so, so != null, null);
                    break;
                case Material mat:
                    provideHandle.Complete(mat, mat != null, null);
                    break;
                case Sprite sprite:
                    provideHandle.Complete(sprite, sprite != null, null);
                    break;
                case AudioClip audio:
                    provideHandle.Complete(audio, audio != null, null);
                    break;
                default:
                    provideHandle.Complete(asset, asset != null, null);
                    break;
            }
        }
        
        
    }
}