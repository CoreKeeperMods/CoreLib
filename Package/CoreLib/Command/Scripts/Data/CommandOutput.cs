using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Command.Data
{
    /// Represents the output of a command execution within the system.
    /// <remarks>
    /// The <c>CommandOutput</c> struct encapsulates the result or feedback generated after
    /// a command is executed. It provides information about the command's result, such as
    /// success, failure, warnings, or other relevant details.
    /// This structure supports various constructors and implicit conversions for ease of use.
    /// </remarks>
    public struct CommandOutput
    {
        /// Represents the feedback or message content for a command's output within the system.
        /// <remarks>
        /// The <c>feedback</c> variable contains the textual content or description associated
        /// with the output of a command. It is commonly used to convey information, errors,
        /// warnings, or other messages pertaining to the execution or result of a command.
        /// </remarks>
        public string feedback;

        /// Represents the status of a command or message within the system.
        /// <remarks>
        /// The <c>status</c> variable indicates the current state of a command or message.
        /// It is typically used to classify the level of importance or type of output,
        /// such as informational, warning, error, and so on.
        /// The allowed values are defined in the <see cref="CommandStatus"/> enumeration.
        /// </remarks>
        public CommandStatus status;

        /// Represents the output of a command, including feedback and status.
        public CommandOutput(string feedback)
        {
            this.feedback = feedback;
            status = CommandStatus.Info;
        }

        /// Represents the output of a command, including feedback and its status.
        [Obsolete("Please use CommandStatus variation")]
        public CommandOutput(string feedback, Color color)
        {
            this.feedback = feedback;
            status = ConvertToStatus(color);
        }

        /// Represents the output of a command, including feedback and a status detailing the result.
        public CommandOutput(string feedback, CommandStatus status)
        {
            this.feedback = feedback;
            this.status = status;
        }

        /// Converts a color to its corresponding command status value.
        /// <param name="color">The color to convert, which represents different command statuses.</param>
        /// <returns>A <see cref="CommandStatus"/> that corresponds to the given color.</returns>
        private static CommandStatus ConvertToStatus(Color color)
        {
            if (color == Color.red)
                return CommandStatus.Error;
            if (color == Color.white)
                return CommandStatus.None;
            if (color == Color.blue)
                return CommandStatus.Hint;
            if (color == Color.yellow)
                return CommandStatus.Warning;

            return CommandStatus.Info;
        }

        /// Default feedback means success
        public static implicit operator CommandOutput(string d) => new CommandOutput(d);
    }
}