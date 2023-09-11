namespace CoreLib.Submodules.JsonLoader
{
    public class FileContext
    {
        public string file;
        public string filename;
        public FileContext() { }

        public FileContext(string file, string filename)
        {
            this.file = file;
            this.filename = filename;
        }
    }
}