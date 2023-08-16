using UnityEngine;

namespace CoreLib.Submodules.ChatCommands
{
    public struct CommandOutput
    {
        public string feedback;
        public Color color;

        /// <summary>
        /// Normal feedback (Success)
        /// </summary>
        public CommandOutput(string feedback)
        {
            this.feedback = feedback;
            color = Color.green;
        }

        /// <summary>
        /// Feedback with custom color
        /// </summary>
        public CommandOutput(string feedback, Color color)
        {
            this.feedback = feedback;
            this.color = color;
        }
        
        /// <summary>
        /// Default feedback means success
        /// </summary>
        public static implicit operator CommandOutput(string d) => new CommandOutput(d);
    }
}