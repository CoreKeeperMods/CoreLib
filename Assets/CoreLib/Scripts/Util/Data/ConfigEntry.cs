using System;

namespace CoreLib.Data
{
/// <summary>
///     Provides access to a single setting inside of a <see cref="Configuration.ConfigFile" />.
/// </summary>
/// <typeparam name="T">Type of the setting.</typeparam>
public sealed class ConfigEntry<T> : ConfigEntryBase
{
    private T _typedValue;

    internal ConfigEntry(JsonConfigFile configFile,
                         string key,
                         T defaultValue) : base(configFile, key, typeof(T), defaultValue)
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
    internal protected ConfigEntryBase(JsonConfigFile configFile,
                             string key,
                             Type settingType,
                             object defaultValue)
    {
        ConfigFile = configFile ?? throw new ArgumentNullException(nameof(configFile));
        Key = key ?? throw new ArgumentNullException(nameof(key));
        SettingType = settingType ?? throw new ArgumentNullException(nameof(settingType));

        DefaultValue = defaultValue;

        // Free type check and automatically calls ClampValue in case AcceptableValues were provided
        BoxedValue = defaultValue;
    }

    /// <summary>
    ///     Config file this entry is a part of.
    /// </summary>
    public JsonConfigFile ConfigFile { get; }

    /// <summary>
    ///     Category and name of this setting. Used as a unique key for identification within a
    ///     <see cref="Configuration.ConfigFile" />.
    /// </summary>
    public string Key { get; }

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
            CoreLibMod.Log.LogWarning(
                       $"Config value of setting \"{Key}\" could not be parsed and will be ignored. Reason: {e.Message}; Value: {value}");
        }
    }
    

    /// <summary>
    ///     Trigger setting changed event.
    /// </summary>
    protected void OnSettingChanged(object sender) => ConfigFile.OnSettingChanged(sender, this);
}
}