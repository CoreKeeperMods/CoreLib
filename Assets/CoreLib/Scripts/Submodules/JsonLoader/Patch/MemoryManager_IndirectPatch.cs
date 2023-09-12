namespace CoreLib.Submodules.JsonLoader.Patch
{
    public class MemoryManager_IndirectPatch
    {
        internal static void PerformPostLoad()
        {
            if (!JsonLoaderModule.Instance.Loaded) return;
            
            JsonLoaderModule.PostApply();
        }
    }
}