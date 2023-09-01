using System;
using System.Collections.Generic;
using System.Linq;
using PugMod;
// ReSharper disable PossibleInvalidCastExceptionInForeachLoop

namespace CoreLib
{
    public class JsonConfigFile
    {
        private readonly string ConfigFilePath;
        private readonly string Mod;

        [Serializable]
        public struct EntryData
        {
            public string key;
            public string value;
        }

        [Serializable]
        public class FileData
        {
            public List<EntryData> entries = new List<EntryData>();
        }

        public Dictionary<string, ConfigEntryBase> Entries { get; } = new();
        public Dictionary<string, string> OrphanedEntries { get; } = new();

        public JsonConfigFile(string mod, string configPath, bool saveOnInit)
        {
            if (configPath == null) throw new ArgumentNullException(nameof(configPath));
            ConfigFilePath = configPath;
            Mod = mod;

            if (saveOnInit && !Reload()) Save();
        }

        public void Save()
        {
            FileData fileData = new FileData();
            foreach (var kv in Entries)
            {
                fileData.entries.Add(new EntryData()
                {
                    key = kv.Key,
                    value = kv.Value.GetSerializedValue()
                });
            }
            
            foreach (var kv in OrphanedEntries)
            {
                fileData.entries.Add(new EntryData()
                {
                    key = kv.Key,
                    value = kv.Value
                });
            }

            if (API.Config == null)
            {
                CoreLibMod.Log.LogWarning("Tried to save too early!");
                return;
            }
            API.Config.Set(Mod, ConfigFilePath, "json", fileData);
        }

        public bool Reload()
        {
            if (API.Config == null)
            {
                CoreLibMod.Log.LogWarning("Tried to load too early!");
                return false;
            }
            
            bool success = API.Config.TryGet(Mod, ConfigFilePath, "json", out FileData fileData);
            if (success)
            {
                foreach (EntryData entry in fileData.entries)
                {
                    OrphanedEntries.Add(entry.key, entry.value);
                }
            }

            return success;
        }

        /// <summary>
        ///     Create a new setting. The setting is saved to drive and loaded automatically.
        ///     Each definition can be used to add only one setting, trying to add a second setting will throw an exception.
        /// </summary>
        public ConfigEntry<T> Bind<T>(string key, T defaultValue)
        {
            if (!TomlTypeConverter.CanConvert(typeof(T)))
                throw new
                    ArgumentException(
                        $"Type {typeof(T)} is not supported by the config system. Supported types: {string.Join(", ", TomlTypeConverter.GetSupportedTypes().Select(x => x.Name).ToArray())}");


            if (Entries.TryGetValue(key, out var rawEntry))
                return (ConfigEntry<T>)rawEntry;

            var entry = new ConfigEntry<T>(this, key, defaultValue);

            Entries[key] = entry;

            if (OrphanedEntries.TryGetValue(key, out var homelessValue))
            {
                entry.SetSerializedValue(homelessValue);
                OrphanedEntries.Remove(key);
            }
            
            Save();

            return entry;
        }

        public event EventHandler ConfigReloaded;

        public sealed class SettingChangedEventArgs : EventArgs
        {
            /// <inheritdoc />
            public SettingChangedEventArgs(ConfigEntryBase changedSetting)
            {
                ChangedSetting = changedSetting;
            }

            /// <summary>
            ///     Setting that was changed
            /// </summary>
            public ConfigEntryBase ChangedSetting { get; }
        }
        
        /// <summary>
        ///     Fired when one of the settings is changed.
        /// </summary>
        public event EventHandler<SettingChangedEventArgs> SettingChanged;

        internal void OnSettingChanged(object sender, ConfigEntryBase changedEntryBase)
        {
            if (changedEntryBase == null) throw new ArgumentNullException(nameof(changedEntryBase));
            
            Save();

            var settingChanged = SettingChanged;
            if (settingChanged == null) return;

            var args = new SettingChangedEventArgs(changedEntryBase);
            foreach (EventHandler<SettingChangedEventArgs> callback in settingChanged.GetInvocationList())
                try
                {
                    callback(sender, args);
                }
                catch (Exception e)
                {
                    CoreLibMod.Log.LogError(e.ToString());
                }
        }

        private void OnConfigReloaded()
        {
            var configReloaded = ConfigReloaded;
            if (configReloaded == null) return;

            foreach (EventHandler callback in configReloaded.GetInvocationList())
                try
                {
                    callback(this, EventArgs.Empty);
                }
                catch (Exception e)
                {
                    CoreLibMod.Log.LogError(e.ToString());
                }
        }
    }
}