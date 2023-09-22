using System;

namespace CoreLib.Submodules.JsonLoader
{
    public interface IFileAccess
    {
        void WriteAllBytes(string path, byte[] bytes);
    }

    public class NoFileAccess : IFileAccess
    {
        public void WriteAllBytes(string path, byte[] bytes)
        {
            throw new NotSupportedException("Unable to write to disk!");
        }
    }
}