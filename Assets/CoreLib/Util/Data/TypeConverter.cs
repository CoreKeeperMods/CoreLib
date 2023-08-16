using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor.PackageManager;

namespace CoreLib
{
    /// <summary>
    ///     A serializer/deserializer combo for some type(s). Used by the config system.
    /// </summary>
    public class TypeConverter
    {
        /// <summary>
        ///     Used to serialize the type into a (hopefully) human-readable string.
        ///     Object is the instance to serialize, Type is the object's type.
        /// </summary>
        public Func<object, Type, string> ConvertToString { get; set; }

        /// <summary>
        ///     Used to deserialize the type from a string.
        ///     String is the data to deserialize, Type is the object's type, should return instance to an object of Type.
        /// </summary>
        public Func<string, Type, object> ConvertToObject { get; set; }
    }
    
    public static class TomlTypeConverter
    {
        private static Dictionary<Type, TypeConverter> TypeConverters { get; } = new()
        {
            [typeof(string)] = new TypeConverter
            {
                ConvertToString = (obj, _) => Escape((string)obj),
                ConvertToObject = (str, _) =>
                {
                    // Check if the string is a file path with unescaped \ path separators (e.g. D:\test and not D:\\test)
                    if (Regex.IsMatch(str, @"^""?\w:\\(?!\\)(?!.+\\\\)"))
                        return str;
                    return Unescape(str);
                }
            },
            [typeof(bool)] = new TypeConverter
            {
                ConvertToString = (obj, _) => obj.ToString().ToLowerInvariant(),
                ConvertToObject = (str, _) => bool.Parse(str)
            },
            [typeof(byte)] = new TypeConverter
            {
                ConvertToString = (obj, _) => obj.ToString(),
                ConvertToObject = (str, _) => byte.Parse(str)
            },

            //integral types

            [typeof(sbyte)] = new TypeConverter
            {
                ConvertToString = (obj, _) => obj.ToString(),
                ConvertToObject = (str, _) => sbyte.Parse(str)
            },
            [typeof(byte)] = new TypeConverter
            {
                ConvertToString = (obj, _) => obj.ToString(),
                ConvertToObject = (str, _) => byte.Parse(str)
            },
            [typeof(short)] = new TypeConverter
            {
                ConvertToString = (obj, _) => obj.ToString(),
                ConvertToObject = (str, _) => short.Parse(str)
            },
            [typeof(ushort)] = new TypeConverter
            {
                ConvertToString = (obj, _) => obj.ToString(),
                ConvertToObject = (str, _) => ushort.Parse(str)
            },
            [typeof(int)] = new TypeConverter
            {
                ConvertToString = (obj, _) => obj.ToString(),
                ConvertToObject = (str, _) => int.Parse(str)
            },
            [typeof(uint)] = new TypeConverter
            {
                ConvertToString = (obj, _) => obj.ToString(),
                ConvertToObject = (str, _) => uint.Parse(str)
            },
            [typeof(long)] = new TypeConverter
            {
                ConvertToString = (obj, _) => obj.ToString(),
                ConvertToObject = (str, _) => long.Parse(str)
            },
            [typeof(ulong)] = new TypeConverter
            {
                ConvertToString = (obj, _) => obj.ToString(),
                ConvertToObject = (str, _) => ulong.Parse(str)
            },

            //floating point types

            [typeof(float)] = new TypeConverter
            {
                ConvertToString = (obj, _) => ((float)obj).ToString(NumberFormatInfo.InvariantInfo),
                ConvertToObject = (str, _) => float.Parse(str, NumberFormatInfo.InvariantInfo)
            },
            [typeof(double)] = new TypeConverter
            {
                ConvertToString = (obj, _) => ((double)obj).ToString(NumberFormatInfo.InvariantInfo),
                ConvertToObject = (str, _) => double.Parse(str, NumberFormatInfo.InvariantInfo)
            },
            [typeof(decimal)] = new TypeConverter
            {
                ConvertToString = (obj, _) => ((decimal)obj).ToString(NumberFormatInfo.InvariantInfo),
                ConvertToObject = (str, _) => decimal.Parse(str, NumberFormatInfo.InvariantInfo)
            },

            //enums are special

            [typeof(Enum)] = new TypeConverter
            {
                ConvertToString = (obj, _) => obj.ToString(),
                ConvertToObject = (str, type) => Enum.Parse(type, str, true)
            }
        };

        /// <summary>
        ///     Convert object of a given type to a string using available converters.
        /// </summary>
        public static string ConvertToString(object value, Type valueType)
        {
            var conv = GetConverter(valueType);
            if (conv == null)
                throw new InvalidOperationException($"Cannot convert from type {valueType}");

            return conv.ConvertToString(value, valueType);
        }

        /// <summary>
        ///     Convert string to an object of a given type using available converters.
        /// </summary>
        public static T ConvertToValue<T>(string value) => (T)ConvertToValue(value, typeof(T));

        /// <summary>
        ///     Convert string to an object of a given type using available converters.
        /// </summary>
        public static object ConvertToValue(string value, Type valueType)
        {
            var conv = GetConverter(valueType);
            if (conv == null)
                throw new InvalidOperationException($"Cannot convert to type {valueType.Name}");

            return conv.ConvertToObject(value, valueType);
        }

        /// <summary>
        ///     Get a converter for a given type if there is any.
        /// </summary>
        public static TypeConverter GetConverter(Type valueType)
        {
            if (valueType == null)
                throw new ArgumentNullException(nameof(valueType));

            if (valueType.IsEnum)
                return TypeConverters[typeof(Enum)];

            TypeConverters.TryGetValue(valueType, out var result);

            return result;
        }

        /// <summary>
        ///     Check if a given type can be converted to and from string.
        /// </summary>
        public static bool CanConvert(Type type) => GetConverter(type) != null;

        /// <summary>
        ///     Give a list of types with registered converters.
        /// </summary>
        public static IEnumerable<Type> GetSupportedTypes() => TypeConverters.Keys;

        private static string Escape(this string txt)
        {
            if (string.IsNullOrEmpty(txt)) return string.Empty;

            var stringBuilder = new StringBuilder(txt.Length + 2);
            foreach (var c in txt)
                switch (c)
                {
                    case '\0':
                        stringBuilder.Append(@"\0");
                        break;
                    case '\a':
                        stringBuilder.Append(@"\a");
                        break;
                    case '\b':
                        stringBuilder.Append(@"\b");
                        break;
                    case '\t':
                        stringBuilder.Append(@"\t");
                        break;
                    case '\n':
                        stringBuilder.Append(@"\n");
                        break;
                    case '\v':
                        stringBuilder.Append(@"\v");
                        break;
                    case '\f':
                        stringBuilder.Append(@"\f");
                        break;
                    case '\r':
                        stringBuilder.Append(@"\r");
                        break;
                    case '\'':
                        stringBuilder.Append(@"\'");
                        break;
                    case '\\':
                        stringBuilder.Append(@"\");
                        break;
                    case '\"':
                        stringBuilder.Append(@"\""");
                        break;
                    default:
                        stringBuilder.Append(c);
                        break;
                }

            return stringBuilder.ToString();
        }

        private static string Unescape(this string txt)
        {
            if (string.IsNullOrEmpty(txt))
                return txt;
            var stringBuilder = new StringBuilder(txt.Length);
            for (var i = 0; i < txt.Length;)
            {
                var num = txt.IndexOf('\\', i);
                if (num < 0 || num == txt.Length - 1)
                    num = txt.Length;
                stringBuilder.Append(txt, i, num - i);
                if (num >= txt.Length)
                    break;
                var c = txt[num + 1];
                switch (c)
                {
                    case '0':
                        stringBuilder.Append('\0');
                        break;
                    case 'a':
                        stringBuilder.Append('\a');
                        break;
                    case 'b':
                        stringBuilder.Append('\b');
                        break;
                    case 't':
                        stringBuilder.Append('\t');
                        break;
                    case 'n':
                        stringBuilder.Append('\n');
                        break;
                    case 'v':
                        stringBuilder.Append('\v');
                        break;
                    case 'f':
                        stringBuilder.Append('\f');
                        break;
                    case 'r':
                        stringBuilder.Append('\r');
                        break;
                    case '\'':
                        stringBuilder.Append('\'');
                        break;
                    case '\"':
                        stringBuilder.Append('\"');
                        break;
                    case '\\':
                        stringBuilder.Append('\\');
                        break;
                    default:
                        stringBuilder.Append('\\').Append(c);
                        break;
                }

                i = num + 2;
            }

            return stringBuilder.ToString();
        }
    }
}