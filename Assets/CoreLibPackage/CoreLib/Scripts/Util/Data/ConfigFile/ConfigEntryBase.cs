﻿using System;
using System.Linq;
using System.Text;
using PugMod;

//All code in this folder is from BepInEx library and is licensed under LGPL-2.1 license.

namespace CoreLib.Data.Configuration
{
    /// <summary>
    ///     Provides access to a single setting inside of a <see cref="Configuration.ConfigFile" />.
    /// </summary>
    /// <typeparam name="T">Type of the setting.</typeparam>
    public sealed class ConfigEntry<T> : ConfigEntryBase
    {
        private T _typedValue;

        internal ConfigEntry(ConfigFile configFile,
            ConfigDefinition definition,
            T defaultValue,
            ConfigDescription configDescription) : base(configFile, definition, typeof(T),
            defaultValue, configDescription)
        {
            configFile.SettingChanged += (sender, args) =>
            {
                if (args.ChangedSetting == this) SettingChanged?.Invoke(sender, args);
            };
        }

        /// <summary>
        ///     Value of this setting.
        /// </summary>
        public T Value
        {
            get => _typedValue;
            set
            {
                value = ClampValue(value);
                if (Equals(_typedValue, value))
                    return;

                _typedValue = value;
                OnSettingChanged(this);
            }
        }

        /// <inheritdoc />
        public override object BoxedValue
        {
            get => Value;
            set => Value = (T) value;
        }

        /// <summary>
        ///     Fired when the setting is changed. Does not detect changes made outside from this object.
        /// </summary>
        public event EventHandler SettingChanged;
    }

    /// <summary>
    ///     Container for a single setting of a <see cref="Configuration.ConfigFile" />.
    ///     Each config entry is linked to one config file.
    /// </summary>
    public abstract class ConfigEntryBase
    {
        /// <summary>
        ///     Types of defaultValue and definition.AcceptableValues have to be the same as settingType.
        /// </summary>
        internal protected ConfigEntryBase(ConfigFile configFile,
            ConfigDefinition definition,
            Type settingType,
            object defaultValue,
            ConfigDescription configDescription)
        {
            ConfigFile = configFile ?? throw new ArgumentNullException(nameof(configFile));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            SettingType = settingType ?? throw new ArgumentNullException(nameof(settingType));

            Description = configDescription ?? ConfigDescription.Empty;
            if (Description.AcceptableValues != null &&
                !SettingType.IsAssignableFrom(Description.AcceptableValues.ValueType))
                throw new
                    ArgumentException("configDescription.AcceptableValues is for a different type than the type of this setting");

            DefaultValue = defaultValue;

            // Free type check and automatically calls ClampValue in case AcceptableValues were provided
            BoxedValue = defaultValue;
        }

        /// <summary>
        ///     Config file this entry is a part of.
        /// </summary>
        public ConfigFile ConfigFile { get; }

        /// <summary>
        ///     Category and name of this setting. Used as a unique key for identification within a
        ///     <see cref="Configuration.ConfigFile" />.
        /// </summary>
        public ConfigDefinition Definition { get; }

        /// <summary>
        ///     Description / metadata of this setting.
        /// </summary>
        public ConfigDescription Description { get; }

        /// <summary>
        ///     Type of the <see cref="BoxedValue" /> that this setting holds.
        /// </summary>
        public Type SettingType { get; }

        /// <summary>
        ///     Default value of this setting (set only if the setting was not changed before).
        /// </summary>
        public object DefaultValue { get; }

        /// <summary>
        ///     Get or set the value of the setting.
        /// </summary>
        public abstract object BoxedValue { get; set; }

        /// <summary>
        ///     Get the serialized representation of the value.
        /// </summary>
        public string GetSerializedValue() => TomlTypeConverter.ConvertToString(BoxedValue, SettingType);

        /// <summary>
        ///     Set the value by using its serialized form.
        /// </summary>
        public void SetSerializedValue(string value)
        {
            try
            {
                var newValue = TomlTypeConverter.ConvertToValue(value, SettingType);
                BoxedValue = newValue;
            }
            catch (Exception e)
            {
                CoreLibMod.Log.LogWarning($"Config value of setting \"{Definition}\" could not be parsed and will be ignored. Reason: {e.Message}; Value: {value}");
            }
        }

        /// <summary>
        ///     If necessary, clamp the value to acceptable value range. T has to be equal to settingType.
        /// </summary>
        protected T ClampValue<T>(T value)
        {
            if (Description.AcceptableValues != null)
                return (T) Description.AcceptableValues.Clamp(value);
            return value;
        }

        /// <summary>
        ///     Trigger setting changed event.
        /// </summary>
        protected void OnSettingChanged(object sender) => ConfigFile.OnSettingChanged(sender, this);

        /// <summary>
        ///     Write a description of this setting using all available metadata.
        /// </summary>
        public void WriteDescription(StringBuilder stringBuilder)
        {
            if (!string.IsNullOrEmpty(Description.Description))
                stringBuilder.AppendLine($"## {Description.Description.Replace("\n", "\n## ")}");

            stringBuilder.AppendLine($"# Setting type: {SettingType.GetNameChecked()}");

            stringBuilder.AppendLine($"# Default value: {TomlTypeConverter.ConvertToString(DefaultValue, SettingType)}");

            if (Description.AcceptableValues != null)
            {
                stringBuilder.AppendLine(Description.AcceptableValues.ToDescriptionString());
            }
            else if (SettingType.IsEnum)
            {
                stringBuilder.AppendLine($"# Acceptable values: {string.Join(", ", Enum.GetNames(SettingType))}");

                if (SettingType.GetCustomAttributesChecked().Any(attribute => attribute.GetType() == typeof(FlagsAttribute)))
                    stringBuilder.AppendLine("# Multiple values can be set at the same time by separating them with , (e.g. Debug, Warning)");
            }
        }
    }
}
