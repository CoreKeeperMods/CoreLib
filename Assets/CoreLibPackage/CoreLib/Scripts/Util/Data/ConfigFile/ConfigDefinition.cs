using System;
using System.Linq;
//All code in this folder is from BepInEx library and is licensed under LGPL-2.1 license.

// ReSharper disable once CheckNamespace
namespace CoreLib.Data.Configuration
{
    ///     Section and key of a setting. Used as a unique key for identification within a
    ///     <see cref="T:CoreLib.Data.Configuration.ConfigFile" />.
    ///     The same definition can be used in multiple config files, it will point to different settings then.
    /// <inheritdoc />
    public class ConfigDefinition : IEquatable<ConfigDefinition>
    {
        private static readonly char[] InvalidConfigChars = { '=', '\n', '\t', '\\', '"', '\'', '[', ']' };

        ///     Create a new definition. Definitions with same section and key are equal.
        /// <param name="section">Group of the setting, case-sensitive.</param>
        /// <param name="key">Name of the setting, case-sensitive.</param>
        public ConfigDefinition(string section, string key)
        {
            CheckInvalidConfigChars(section, nameof(section));
            CheckInvalidConfigChars(key, nameof(key));
            Key = key;
            Section = section;
        }

        ///     Group of the setting. All settings within a config file are grouped by this.
        public string Section { get; }

        ///     Name of the setting.
        public string Key { get; }

        ///     Check if the definitions are the same.
        /// <inheritdoc />
        public bool Equals(ConfigDefinition other)
        {
            if (other == null) return false;
            return string.Equals(Key, other.Key)
                   && string.Equals(Section, other.Section);
        }

        private static void CheckInvalidConfigChars(string val, string name)
        {
            if (val == null) throw new ArgumentNullException(name);
            if (val != val.Trim())
                throw new ArgumentException("Cannot use whitespace characters at start or end of section and key names",
                    name);
            if (val.Any(c => InvalidConfigChars.Contains(c)))
                throw new
                    ArgumentException(@"Cannot use any of the following characters in section and key names: = \n \t \ "" ' [ ]",
                        name);
        }

        ///     Check if the definitions are the same.
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;

            return Equals(obj as ConfigDefinition);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Key != null ? Key.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (Section != null ? Section.GetHashCode() : 0);
                return hashCode;
            }
        }

        ///     Check if the definitions are the same.
        public static bool operator ==(ConfigDefinition left, ConfigDefinition right) => Equals(left, right);

        ///     Check if the definitions are the same.
        public static bool operator !=(ConfigDefinition left, ConfigDefinition right) => !Equals(left, right);

        /// <inheritdoc />
        public override string ToString() => Section + "." + Key;
    }
}
