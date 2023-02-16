using System.Text.Json;
using System.Text.Json.Serialization;
using Il2CppSystem;
using Type = System.Type;

namespace CoreLib.Submodules.JsonLoader.Converters
{
    public class Il2CppStringConverter : JsonConverter<String>
    {
        public override String Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString();
        }

        public override void Write(Utf8JsonWriter writer, String value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}