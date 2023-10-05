using PugMod;

namespace CoreLib.JsonLoader
{
    public struct FileReference
    {
        public LoadedMod mod;
        public string filePath;
        public string contextPath;
        
        public string targetId;

        public FileReference(LoadedMod mod, string filePath, string contextPath)
        {
            this.mod = mod;
            this.filePath = filePath;
            this.contextPath = contextPath;
            targetId = "";
        }
        
        public FileReference(LoadedMod mod, string filePath, string contextPath, string targetId)
        {
            this.mod = mod;
            this.filePath = filePath;
            this.contextPath = contextPath;
            this.targetId = targetId;
        }
    }
}