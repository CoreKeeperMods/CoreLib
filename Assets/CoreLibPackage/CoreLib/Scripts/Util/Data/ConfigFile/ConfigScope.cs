namespace CoreLib.Data.Configuration
{
    public enum ConfigAccessLevel
    {
        /// <summary>
        /// Can only be viewed in the menu, unchangeable
        /// </summary>
        ViewOnly = -1,

        /// <summary>
        /// Changes only take effect on your own client
        /// </summary>
        Client,

        /// <summary>
        /// Changes are synced to all clients
        /// </summary>
        Server,

        /// <summary>
        /// Admin: Same as <see cref="ConfigAccessLevel.Server"/>, only admin changeable
        /// </summary>
        Admin,
    }

    /// <summary>
    /// Necessary information to ensure the normal operation of the General Config Menu
    /// </summary>
    public class ConfigScope
    {
        public readonly static ConfigScope Empty = new(ConfigAccessLevel.Server, false);
        public bool RequireReload;

        /// <summary>
        /// Determines permissions for changing config entry
        /// </summary>
        public ConfigAccessLevel AccessLevel;
        public ConfigScope(ConfigAccessLevel accessLevel = ConfigAccessLevel.Server, bool requireReload = false)
        {
            RequireReload = requireReload;
            AccessLevel = accessLevel;
        }
        public bool Changeable()
        {
            var player = Manager.main.player;
            return AccessLevel switch
            {
                ConfigAccessLevel.Client => true,
                ConfigAccessLevel.Server => !player.guestMode,
                ConfigAccessLevel.Admin => !player.guestMode && player.adminPrivileges > 0,
                _ => false,
            };
        }
        public bool ShouldSync => (int)AccessLevel > 0;
    }
}
