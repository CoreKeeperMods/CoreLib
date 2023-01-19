using System;

namespace CoreLib.Submodules.JsonLoader
{
    public struct ContextHandle : IDisposable
    {
        public string oldContext;
        public ContextHandle(string newContext)
        {
            oldContext = JsonLoaderModule.context;
            JsonLoaderModule.context = newContext;
        }

        public void Dispose()
        {
            JsonLoaderModule.context = oldContext;
        }
    }
}