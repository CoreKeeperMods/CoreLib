using System.Globalization;
using System.Text.Json;

namespace CoreLib.JsonLoader.Converters
{
    public static partial class ConverterUtil
    {
        public static float ReadArraySingle(ref Utf8JsonReader reader)
        {
            if (!reader.Read()) return 0;
            if (reader.TokenType == JsonTokenType.EndArray) return 0;

            return reader.GetSingle();
        }

        public static void WriteNumberSafe(Utf8JsonWriter writer, float value)
        {
            if (!float.IsInfinity(value) &&
                !float.IsNaN(value))
            {
                writer.WriteNumberValue(value);
            }
            else
            {
                writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}