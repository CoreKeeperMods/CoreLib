// ReSharper disable once CheckNamespace
namespace CoreLib.Data.Configuration
{
    public enum ConfigAccessLevel
    {
        /// Can only be viewed in the menu, unchangeable
        ViewOnly = -1,

        /// Changes only take effect on your own client
        Client,

        /// Changes are synced to all clients
        Server,

        /// Admin: Same as <see cref="ConfigAccessLevel.Server"/>, only admin changeable
        Admin,
    }

    /// Necessary information to ensure the normal operation of the General Config Menu
    public class ConfigScope
    {
        public static readonly ConfigScope Empty = new();
        public bool RequireReload;

        /// Determines permissions for changing config entry
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
