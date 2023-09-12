using System;

namespace CoreLib.Submodules.JsonLoader
{
    public struct JsonContext
    {
        public string loadPath;
        
        public JsonContext(string loadPath)
        {
            this.loadPath = loadPath;
        }
    }
    
    public struct ContextHandle : IDisposable
    {
        public JsonContext oldContext;
        public ContextHandle(JsonContext newContext)
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