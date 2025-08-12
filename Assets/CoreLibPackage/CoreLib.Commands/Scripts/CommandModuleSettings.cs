using System;
using System.Collections.Generic;
using CoreLib.Data.Configuration;

namespace CoreLib.Commands
{
    /// <summary>
    /// Represents the configuration settings for the command module used in the system.
    /// </summary>
    /// <remarks>
    /// This class provides a centralized configuration structure for controlling various
    /// aspects of command execution and behavior, such as security, logging, and user permissions.
    /// </remarks>
    [Serializable]
    public class CommandModuleSettings
    {
        /// <summary>
        /// A configuration setting indicating whether additional hints should be displayed to the user
        /// when errors or issues are encountered.
        /// </summary>
        /// <remarks>
        /// When enabled, the system provides descriptive hints or suggestions to assist the user
        /// in resolving errors. This setting is primarily designed for enhancing user experience
        /// by offering more context during command execution.
        /// </remarks>
        public ConfigEntry<bool> displayAdditionalHints;

        /// <summary>
        /// Determines whether client commands not recognized by the server are allowed to execute.
        /// </summary>
        /// <remarks>
        /// This setting enables or disables the execution of commands that are unrecognized by the server.
        /// When set to true, such commands can still be processed, which may be useful for extending functionality
        /// or for cases where the server does not maintain a strict command validation mechanism.
        /// When set to false, unrecognized client commands will be denied execution, enforcing stricter control and validation.
        /// </remarks>
        public ConfigEntry<bool> allowUnknownClientCommands;

        /// <summary>
        /// Specifies whether the command security system should be enabled.
        /// When enabled, the system checks user permissions and can deny
        /// execution of certain commands based on permissions or predefined restrictions.
        /// </summary>
        public ConfigEntry<bool> enableCommandSecurity;

        /// <summary>
        /// Represents a configuration entry which determines whether all executed commands
        /// should be logged to the console or log file.
        /// </summary>
        /// <remarks>
        /// When enabled, the application will log the details of every command that is executed.
        /// This can assist in debugging or tracking user activity, but it may also result in
        /// higher log verbosity and larger log files.
        /// </remarks>
        public ConfigEntry<bool> logAllExecutedCommands;

        /// <summary>
        /// Represents a dictionary that maps command names (as strings) to their corresponding configuration entries,
        /// determining whether each command is allowed to be executed by users (non-admins).
        /// </summary>
        /// <remarks>
        /// Each key in the dictionary is the name of a command (string), and the value is a <see cref="CoreLib.Data.Configuration.ConfigEntry{bool}"/>
        /// which specifies if the respective command is permitted for execution by users. The boolean value in the configuration entry indicates
        /// whether the command is allowed (true) or not allowed (false).
        /// This variable is utilized by the command registration and permission checking mechanisms to enforce
        /// command execution policies based on user roles and dynamic configuration settings.
        /// </remarks>
        public Dictionary<string, ConfigEntry<bool>> userAllowedCommands = new Dictionary<string, ConfigEntry<bool>>();
    }
}