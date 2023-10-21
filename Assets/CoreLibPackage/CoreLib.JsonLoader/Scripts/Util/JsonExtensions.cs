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
    }
}