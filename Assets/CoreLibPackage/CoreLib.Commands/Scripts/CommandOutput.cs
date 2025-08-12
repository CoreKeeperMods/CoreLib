using System;
using CoreLib.Commands.Communication;
using UnityEngine;

namespace CoreLib.Commands
{
    /// <summary>
    /// Represents the output of a command execution within the system.
    /// </summary>
    /// <remarks>
    /// The <c>CommandOutput</c> struct encapsulates the result or feedback generated after
    /// a command is executed. It provides information about the command's result, such as
    /// success, failure, warnings, or other relevant details.
    /// This structure supports various constructors and implicit conversions for ease of use.
    /// </remarks>
    public struct CommandOutput
    {
        /// <summary>
        /// Represents the feedback or message content for a command's output within the system.
        /// </summary>
        /// <remarks>
        /// The <c>feedback</c> variable contains the textual content or description associated
        /// with the output of a command. It is commonly used to convey information, errors,
        /// warnings, or other messages pertaining to the execution or result of a command.
        /// </remarks>
        public string feedback;

        /// <summary>
        /// Represents the status of a command or message within the system.
        /// </summary>
        /// <remarks>
        /// The <c>status</c> variable indicates the current state of a command or message.
        /// It is typically used to classify the level of importance or type of output,
        /// such as informational, warning, error, and so on.
        /// The allowed values are defined in the <see cref="CommandStatus"/> enumeration.
        /// </remarks>
        public CommandStatus status;

        /// <summary>
        /// Represents the output of a command, including feedback and status.
        /// </summary>
        public CommandOutput(string feedback)
        {
            this.feedback = feedback;
            status = CommandStatus.Info;
        }

        /// <summary>
        /// Represents the output of a command, including feedback and its status.
        /// </summary>
        [Obsolete("Please use CommandStatus variation")]
        public CommandOutput(string feedback, Color color)
        {
            this.feedback = feedback;
            status = ConvertToStatus(color);
        }

        /// <summary>
        /// Represents the output of a command, including feedback and a status detailing the result.
        /// </summary>
        public CommandOutput(string feedback, CommandStatus status)
        {
            this.feedback = feedback;
            this.status = status;
        }

        /// <summary>
        /// Converts a color to its corresponding command status value.
        /// </summary>
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

        /// <summary>
        /// Default feedback means success
        /// </summary>
        public static implicit operator CommandOutput(string d) => new CommandOutput(d);
    }
}