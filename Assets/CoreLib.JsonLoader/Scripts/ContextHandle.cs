using System;
using PugMod;

namespace CoreLib.JsonLoader
{
    public struct JsonContext
    {
        public LoadedMod mod;
        public string loadPath;
        
        public JsonContext(LoadedMod mod, string loadPath)
        {
            this.mod = mod;
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