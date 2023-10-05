using System;

namespace CoreLib.Commands
{
    [Serializable]
    public class CommandModuleSettings
    {
        public bool displayAdditionalHints = true;
        public bool allowUnknownClientCommands = false;
        public bool enableCommandSecurity = false;
        public bool logAllExecutedCommands = true;
    }
}