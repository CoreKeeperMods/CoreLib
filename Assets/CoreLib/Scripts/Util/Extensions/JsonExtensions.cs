using System;
using System.Text.Json;

namespace CoreLib.Util.Extensions
{
    public static class JsonExtensions
    {
        public static int GetIntPropertyOrDefault(this JsonElement element, string key)
        {
            if (element.TryGetProperty(key, out JsonElement res))
            {
                return res.GetInt32();
            }

            return 0;
        }
        
        public static T Deserialize<T>(this JsonElement element, JsonSerializerOptions options = null)
        {
            var json = element.GetRawText();
            return JsonSerializer.Deserialize<T>(json, options);
        }

        public static T Deserialize<T>(this JsonDocument document, JsonSerializerOptions options = null)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            return document.RootElement.Deserialize<T>(options);
        }

        public static object Deserialize(this JsonElement element, Type type, JsonSerializerOptions options = null)
        {
            var json = element.GetRawText();
            return JsonSerializer.Deserialize(json, type, options);
        }
    }
}