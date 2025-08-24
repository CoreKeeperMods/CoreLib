using System;
using ModIO;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Editor
{
    public static class ModIOExtensions
    {
        private static TagCategory[] _allTags = Array.Empty<TagCategory>();
        private static bool _triedToFetch;
        
        public static bool InitializeModIO()
        {
            if (ModIOUnity.IsInitialized()) return true;
            var result = ModIOUnity.InitializeForUser("PugModSDKUser");

            if (result.Succeeded()) return true;
            Debug.Log("Failed to initialize mod.io SDK");
            return false;
        }
        
        public static TagCategory[] FetchTags()
        {
            if (_triedToFetch) return _allTags;
            
            ModIOUnity.GetTagCategories(result =>
            {
                if (result.result.errorCode == 20303 || !result.result.Succeeded())
                {
                    Debug.LogError("failed to fetch categories data");
                    return;
                }

                _allTags = result.value;
            });
            _triedToFetch = true;
            return _allTags;
        }
    }
}