using System.Text.Json;

namespace CoreLib.Submodules.JsonLoader.Readers
{
    public interface IJsonReader
    {
        public void ApplyPre(JsonElement jObject, FileContext context);
        public void ApplyPost(JsonElement jObject, FileContext context);
    }
}