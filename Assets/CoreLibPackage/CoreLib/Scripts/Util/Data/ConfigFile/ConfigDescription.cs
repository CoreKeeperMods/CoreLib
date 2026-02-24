using System;
//All code in this folder is from BepInEx library and is licensed under LGPL-2.1 license.

// ReSharper disable once CheckNamespace
namespace CoreLib.Data.Configuration
{
    ///     Metadata of a <see cref="ConfigEntryBase" />.
    public class ConfigDescription
    {
        ///     Create a new description.
        /// <param name="description">Text describing the function of the setting and any notes or warnings.</param>
        /// <param name="acceptableValues">
        ///     Range of values that this setting can take. The setting's value will be automatically
        ///     clamped.
        /// </param>
        /// <param name="tags">Objects that can be used by user-made classes to add functionality.</param>
        public ConfigDescription(string description, AcceptableValueBase acceptableValues = null, params object[] tags)
        {
            AcceptableValues = acceptableValues;
            Tags = tags;
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }

        ///     Text describing the function of the setting and any notes or warnings.
        public string Description { get; }

        ///     Range of acceptable values for a setting.
        public AcceptableValueBase AcceptableValues { get; }

        ///     Objects that can be used by user-made classes to add functionality.
        public object[] Tags { get; }

        ///     An empty description.
        public static ConfigDescription Empty { get; } = new("");
    }
}
