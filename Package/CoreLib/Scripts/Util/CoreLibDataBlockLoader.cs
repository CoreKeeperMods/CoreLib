using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreLib.Util
{
    public class CoreLibDataBlockLoader : IScriptableDataLoader
    {
        // Singleton Instance
        private static readonly Lazy<CoreLibDataBlockLoader> _instance = new(() => new CoreLibDataBlockLoader());
        
        public static CoreLibDataBlockLoader Instance => _instance.Value;

        // Internal data storage
        private readonly List<ScriptableDataBlock> _dataBlocks = new List<ScriptableDataBlock>();

        // Private constructor prevents external instantiation
        private CoreLibDataBlockLoader() { }

        // Interface Properties
        public bool HasDataBlockChanges { get; private set; }
        public bool LoadCompleted { get; private set; }
        public float LoadCompletedPercentage { get; private set; }

        /// <summary>
        /// Adds an item to the internal tracking list.
        /// </summary>
        public void AddDataBlock(ScriptableDataBlock block)
        {
            if (block == null) throw new ArgumentNullException(nameof(block));
        
            CoreLibMod.log.LogInfo("CoreLibBlocks: Added data block " + block.name);
            
            _dataBlocks.Add(block);
            HasDataBlockChanges = true;
        }

        /// <summary>
        /// Returns the internal list instantly via a completed Task.
        /// </summary>
        public Task<IEnumerable<ScriptableDataBlock>> LoadAsync()
        {
            CoreLibMod.log.LogInfo("CoreLibBlocks: LoadAsync()");
            UpdateLoadStateSuccess();
            return Task.FromResult<IEnumerable<ScriptableDataBlock>>(_dataBlocks);
        }

        /// <summary>
        /// Returns the internal list instantly.
        /// </summary>
        public IEnumerable<ScriptableDataBlock> Load()
        {
            CoreLibMod.log.LogInfo("CoreLibBlocks: Load()");
            UpdateLoadStateSuccess();
            return _dataBlocks;
        }

        /// <summary>
        /// Resets the internal list and state properties.
        /// </summary>
        public void Unload()
        {
            CoreLibMod.log.LogInfo("CoreLibBlocks: Unload()");
            _dataBlocks.Clear();
            LoadCompleted = false;
            LoadCompletedPercentage = 0f;
            HasDataBlockChanges = false;
        }

        private void UpdateLoadStateSuccess()
        {
            LoadCompletedPercentage = 1.0f;
            LoadCompleted = true;
            HasDataBlockChanges = false;
        }
    }
}