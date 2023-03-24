using System;
using System.Reflection;

namespace CoreLib.Submodules.JsonLoader
{
    public struct JsonContext
    {
        public string loadPath = "";
        public Assembly callingAssembly = null;

        public JsonContext() { }
        public JsonContext(string loadPath, Assembly callingAssembly)
        {
            this.loadPath = loadPath;
            this.callingAssembly = callingAssembly;
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