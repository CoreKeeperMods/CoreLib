using System;
using System.Collections.Generic;
using CoreLib.Data.Configuration;

namespace CoreLib.Commands
{
    [Serializable]
    public class CommandModuleSettings
    {
        public ConfigEntry<bool> displayAdditionalHints;
        public ConfigEntry<bool> allowUnknownClientCommands;
        public ConfigEntry<bool> enableCommandSecurity;
        public ConfigEntry<bool> logAllExecutedCommands;

        public Dictionary<string, ConfigEntry<bool>> userAllowedCommands = new Dictionary<string, ConfigEntry<bool>>();
    }
}