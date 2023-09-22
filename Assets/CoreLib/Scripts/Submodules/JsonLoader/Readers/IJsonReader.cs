using System.Text.Json;

namespace CoreLib.Submodules.JsonLoader.Readers
{
    public interface IJsonReader
    {
        public void ApplyPre(JsonElement jObject, FileReference context);
        public void ApplyPost(JsonElement jObject, FileReference context);
    }
}