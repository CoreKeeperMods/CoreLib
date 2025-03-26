using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace CoreLib.ModResources
{
    public class ModResourceProvider : ResourceProviderBase
    {
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