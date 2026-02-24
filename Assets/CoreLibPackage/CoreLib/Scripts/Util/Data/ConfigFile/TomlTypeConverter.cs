using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PugMod;

//All code in this folder is from BepInEx library and is licensed under LGPL-2.1 license.

// ReSharper disable once CheckNamespace
namespace CoreLib.Data.Configuration
{
    ///     Serializer/deserializer used by the config system.
    public static class TomlTypeConverter
    {
        // Don't put anything from UnityEngine here, or it will break preloader, use LazyTomlConverterLoader instead
        private static Dictionary<Type, TypeConverter> TypeConverters { get; } = new()
        {
            [typeof(string)] = new TypeConverter
            {
                ConvertToString = (obj, _) => Escape((string) obj),
                ConvertToObject = (str, _) => Regex.IsMatch(str, @"^""?\w:\\(?!\\)(?!.+\\\\)") ? str : Unescape(str)
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
                ConvertToString = (obj, _) => ((float) obj).ToString(NumberFormatInfo.InvariantInfo),
                ConvertToObject = (str, _) => float.Parse(str, NumberFormatInfo.InvariantInfo)
            },
            [typeof(double)] = new TypeConverter
            {
                ConvertToString = (obj, _) => ((double) obj).ToString(NumberFormatInfo.InvariantInfo),
                ConvertToObject = (str, _) => double.Parse(str, NumberFormatInfo.InvariantInfo)
            },
            [typeof(decimal)] = new TypeConverter
            {
                ConvertToString = (obj, _) => ((decimal) obj).ToString(NumberFormatInfo.InvariantInfo),
                ConvertToObject = (str, _) => decimal.Parse(str, NumberFormatInfo.InvariantInfo)
            },

            //enums are special

            [typeof(Enum)] = new TypeConverter
            {
                ConvertToString = (obj, _) => obj.ToString(),
                ConvertToObject = (str, type) => Enum.Parse(type, str, true)
            }
        };

        ///     Convert object of a given type to a string using available converters.
        public static string ConvertToString(object value, Type valueType)
        {
            var conv = GetConverter(valueType);
            return conv == null ? throw new InvalidOperationException($"Cannot convert from type {valueType}") : conv.ConvertToString(value, valueType);
        }

        ///     Convert string to an object of a given type using available converters.
        public static T ConvertToValue<T>(string value) => (T) ConvertToValue(value, typeof(T));

        ///     Convert string to an object of a given type using available converters.
        public static object ConvertToValue(string value, Type valueType)
        {
            var conv = GetConverter(valueType);
            return conv == null ? throw new InvalidOperationException($"Cannot convert to type {valueType.GetNameChecked()}") : conv.ConvertToObject(value, valueType);
        }

        ///     Get a converter for a given type if there is any.
        public static TypeConverter GetConverter(Type valueType)
        {
            if (valueType == null)
                throw new ArgumentNullException(nameof(valueType));

            if (valueType.IsEnum)
                return TypeConverters[typeof(Enum)];

            TypeConverters.TryGetValue(valueType, out var result);

            return result;
        }

        ///     Add a new type converter for a given type.
        ///     If a different converter is already added, this call is ignored and false is returned.
        public static bool AddConverter(Type type, TypeConverter converter)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            if (CanConvert(type))
            {
                CoreLibMod.log.LogWarning("Tried to add a TomlConverter when one already exists for type " + type.FullName);
                return false;
            }

            TypeConverters.Add(type, converter);
            return true;
        }

        ///     Check if a given type can be converted to and from string.
        public static bool CanConvert(Type type) => GetConverter(type) != null;

        ///     Give a list of types with registered converters.
        public static IEnumerable<Type> GetSupportedTypes() => TypeConverters.Keys;

        private static string Escape(this string txt)
        {
            if (string.IsNullOrEmpty(txt)) return string.Empty;

            var stringBuilder = new StringBuilder(txt.Length + 2);
            foreach (char c in txt)
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
            for (int i = 0; i < txt.Length;)
            {
                int num = txt.IndexOf('\\', i);
                if (num < 0 || num == txt.Length - 1)
                    num = txt.Length;
                stringBuilder.Append(txt, i, num - i);
                if (num >= txt.Length)
                    break;
                char c = txt[num + 1];
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
