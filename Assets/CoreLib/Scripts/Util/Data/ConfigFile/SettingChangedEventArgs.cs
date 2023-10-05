using System;
//All code in this folder is from BepInEx library and is licensed under LGPL-2.1 license.

namespace CoreLib.Data.Configuration
{
    /// <summary>
    ///     Arguments for events concerning a change of a setting.
    /// </summary>
    /// <inheritdoc />
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
}
