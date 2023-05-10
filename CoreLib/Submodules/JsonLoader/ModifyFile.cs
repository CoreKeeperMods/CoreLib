namespace CoreLib.Submodules.JsonLoader
{
    public struct ModifyFile
    {
        public string filePath;
        public string targetId;

        public ModifyFile(string filePath, string targetId)
        {
            this.filePath = filePath;
            this.targetId = targetId;
        }
    }
}