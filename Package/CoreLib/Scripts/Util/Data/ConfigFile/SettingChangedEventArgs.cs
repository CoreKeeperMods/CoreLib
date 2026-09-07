using System;
//All code in this folder is from BepInEx library and is licensed under LGPL-2.1 license.

// ReSharper disable once CheckNamespace
namespace CoreLib.Data.Configuration
{
    ///     Arguments for events concerning a change of a setting.
    /// <inheritdoc />
    public sealed class SettingChangedEventArgs : EventArgs
    {
        /// <inheritdoc />
        public SettingChangedEventArgs(ConfigEntryBase changedSetting)
        {
            ChangedSetting = changedSetting;
        }

        ///     Setting that was changed
        public ConfigEntryBase ChangedSetting { get; }
    }
}
