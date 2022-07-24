using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace CoreLib.Submodules.CustomEntity
{
    /// <summary>
    /// Indicates that loading something has failed
    /// </summary>
    public class LoadException : Exception
    {
        public LoadException(string message) : base(message) { }
    }
    
    /// <summary>
    /// Mod resources definition class. Use this class to load your asset bundles and resolve verta folder paths
    /// </summary>
    public class ResourceData
    {
        public string modId;
        public string modPath;
        public string keyWord;

        public AssetBundle bundle;

        /// <summary>
        /// Create new resource definition
        /// </summary>
        /// <param name="modId">Your mod ID</param>
        /// <param name="keyWord">Unique Keyword used only by your mods</param>
        /// <param name="modPath">Path to mod's main assembly</param>
        public ResourceData(string modId, string keyWord, string modPath)
        {
            this.modId = modId;
            this.modPath = modPath;
            this.keyWord = keyWord;
        }

        /// <summary>
        /// Create new resource definition. Path is inferred from what assembly is calling.
        /// </summary>
        /// <param name="modId">Your mod ID</param>
        /// <param name="keyWord">Unique Keyword used only by your mods</param>
        public ResourceData(string modId, string keyWord)
        {
            this.modId = modId;
            this.modPath = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
            this.keyWord = keyWord;
        }

        /// <summary>
        /// Does this resource definition have a asset bundle loaded
        /// </summary>
        public bool HasAssetBundle()
        {
            return bundle != null;
        }

        /// <summary>
        /// Load asset bundle from mod path.
        /// </summary>
        /// <param name="bundleName">Bundle name</param>
        /// <exception cref="LoadException">Thrown if loading an asset bundle has failed</exception>
        public void LoadAssetBundle(string bundleName)
        {
            bundle = AssetBundle.LoadFromFile($"{modPath}/{bundleName}");
            if (bundle == null)
            {
                throw new LoadException($"Failed to load asset bundle at {modPath}/{bundleName}");
            }
        }
    }
}