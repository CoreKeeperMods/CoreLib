namespace CoreLib.Submodules.JsonLoader
{
    public struct ModifyFile
    {
        public string filePath;

        public string contextPath;
        public string targetId;

        public ModifyFile(string filePath, string contextPath, string targetId)
        {
            this.filePath = filePath;
            this.contextPath = contextPath;
            this.targetId = targetId;
        }
    }
}