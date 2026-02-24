using PugMod;
using System;
using System.Linq;
using System.Text;

//All code in this folder is from BepInEx library and is licensed under LGPL-2.1 license.

// ReSharper disable once CheckNamespace
namespace CoreLib.Data.Configuration
{
    ///     Provides access to a single setting inside a <see cref="Configuration.ConfigFile" />.
    /// <typeparam name="T">Type of the setting.</typeparam>
    public sealed class ConfigEntry<T> : ConfigEntryBase
    {
        private T _typedValue;

        internal ConfigEntry(ConfigFile configFile,
            ConfigDefinition definition,
            T defaultValue,
            ConfigDescription configDescription,
            ConfigScope scope) : base(configFile, definition, typeof(T),
            defaultValue, configDescription, scope)
        {
            configFile.SettingChanged += (sender, args) =>
            {
                if (args.ChangedSetting == this)
                    SettingChanged?.Invoke(sender, args);
            };
        }

        ///     Value of this setting.
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
            set => Value = (T)value;
        }

        ///     Fired when the setting is changed. Does not detect changes made outside from this object.
        public event EventHandler SettingChanged;
    }

    ///     Container for a single setting of a <see cref="Configuration.ConfigFile" />.
    ///     Each config entry is linked to one config file.
    public abstract class ConfigEntryBase
    {
        ///     Types of defaultValue and definition.AcceptableValues have to be the same as settingType.
        protected internal ConfigEntryBase(ConfigFile configFile,
            ConfigDefinition definition,
            Type settingType,
            object defaultValue,
            ConfigDescription configDescription,
            ConfigScope scope)
        {
            ConfigFile = configFile ?? throw new ArgumentNullException(nameof(configFile));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            SettingType = settingType ?? throw new ArgumentNullException(nameof(settingType));

            Description = configDescription ?? ConfigDescription.Empty;
            if (Description.AcceptableValues != null &&
                !SettingType.IsAssignableFrom(Description.AcceptableValues.ValueType))
                throw new
                    ArgumentException("configDescription.AcceptableValues is for a different type than the type of this setting");
            Scope = scope ?? ConfigScope.Empty;
            DefaultValue = defaultValue;

            // Free type check and automatically calls ClampValue in case AcceptableValues were provided
            // ReSharper disable once VirtualMemberCallInConstructor
            BoxedValue = defaultValue;
        }

        ///     Config file this entry is a part of.
        public ConfigFile ConfigFile { get; }

        ///     Category and name of this setting. Used as a unique key for identification within a
        ///     <see cref="Configuration.ConfigFile" />.
        public ConfigDefinition Definition { get; }

        ///     Description / metadata of this setting.
        public ConfigDescription Description { get; }

        ///     Used by GeneralConfigMenu.
        public ConfigScope Scope { get; }

        ///     Type of the <see cref="BoxedValue" /> that this setting holds.
        public Type SettingType { get; }

        ///     Default value of this setting (set only if the setting was not changed before).
        public object DefaultValue { get; }

        ///     Get or set the value of the setting.
        public abstract object BoxedValue { get; set; }

        ///     Get the serialized representation of the value.
        public string GetSerializedValue() => TomlTypeConverter.ConvertToString(BoxedValue, SettingType);

        ///     Set the value by using its serialized form.
        public void SetSerializedValue(string value)
        {
            try
            {
                var newValue = TomlTypeConverter.ConvertToValue(value, SettingType);
                BoxedValue = newValue;
            }
            catch (Exception e)
            {
                CoreLibMod.log.LogWarning($"Config value of setting \"{Definition}\" could not be parsed and will be ignored. Reason: {e.Message}; Value: {value}");
            }
        }

        ///     If necessary, clamp the value to acceptable value range. T has to be equal to settingType.
        protected T ClampValue<T>(T value)
        {
            if (Description.AcceptableValues != null)
                return (T)Description.AcceptableValues.Clamp(value);
            return value;
        }

        ///     Trigger setting changed event.
        protected void OnSettingChanged(object sender) => ConfigFile.OnSettingChanged(sender, this);

        ///     Write a description of this setting using all available metadata.
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
