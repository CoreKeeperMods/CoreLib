using System.Text.Json.Nodes;

namespace CoreLib.Submodules.JsonLoader.Readers
{
    public interface IJsonReader
    {
        public void ApplyPre(JsonNode jObject);
        public void ApplyPost(JsonNode jObject);
    }
}