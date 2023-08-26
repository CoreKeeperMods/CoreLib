using System;
using CoreLib.Submodules.ChatCommands.Communication;
using UnityEngine;

namespace CoreLib.Submodules.ChatCommands
{
    public struct CommandOutput
    {
        public string feedback;
        public CommandStatus status;

        /// <summary>
        /// Normal feedback (Success)
        /// </summary>
        public CommandOutput(string feedback)
        {
            this.feedback = feedback;
            status = CommandStatus.Info;
        }

        /// <summary>
        /// Feedback with custom color
        /// </summary>
        [Obsolete("Please use CommandStatus variation")]
        public CommandOutput(string feedback, Color color)
        {
            this.feedback = feedback;
            status = ConvertToStatus(color);
        }

        /// <summary>
        /// Feedback with status
        /// </summary>
        public CommandOutput(string feedback, CommandStatus status)
        {
            this.feedback = feedback;
            this.status = status;
        }

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