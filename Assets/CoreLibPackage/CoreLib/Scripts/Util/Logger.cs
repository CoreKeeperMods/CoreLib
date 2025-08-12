using UnityEngine;

namespace CoreLib.Util
{
    /// <summary>
    /// Provides logging functionality with categorization support for Debug, Info, Warning, and Error messages.
    /// </summary>
    /// <remarks>
    /// The <c>Logger</c> class allows tagging log messages with a specified tag, enabling easier categorization
    /// and filtering of log output in Unity's console. This class is particularly useful for maintaining organized
    /// and context-aware logging in larger applications or systems.
    /// </remarks>
    public class Logger
    {
        /// <summary>
        /// Represents a string-based identifier used to categorize and tag log messages.
        /// </summary>
        /// <remarks>
        /// This variable is prefixed to log messages to indicate their category or context.
        /// It helps in filtering and organizing log output, particularly in larger systems or applications.
        /// </remarks>
        private readonly string tag;

        /// <summary>
        /// Provides logging functionality with different levels of severity, including Debug, Info, Warning, and Error.
        /// </summary>
        /// <remarks>
        /// The Logger class enables log messages to be tagged with a specific identifier, making them easy to categorize and filter.
        /// It's useful for maintaining organized log outputs, particularly in larger applications or game development environments.
        /// </remarks>
        public Logger(string tag)
        {
            this.tag = $"[{tag}]";
        }

        /// <summary>
        /// Logs a debug message tagged with a specific identifier.
        /// </summary>
        /// <param name="text">The debug message to be logged. This message provides detailed information useful for diagnosing specific issues.</param>
        public void LogDebug(string text)
        {
            UnityEngine.Debug.unityLogger.Log(LogType.Log, tag, text);
        }

        /// <summary>
        /// Logs informational messages to the Unity console.
        /// </summary>
        /// <param name="text">The informational message to be logged.</param>
        public void LogInfo(string text)
        {
            UnityEngine.Debug.unityLogger.Log(LogType.Log, tag, text);
        }

        /// <summary>
        /// Logs a warning message to the Unity console with an associated tag.
        /// </summary>
        /// <param name="text">The warning message text to be logged.</param>
        public void LogWarning(string text)
        {
            UnityEngine.Debug.unityLogger.Log(LogType.Warning, tag, text);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="text">The error message to be logged.</param>
        public void LogError(string text)
        {
            UnityEngine.Debug.unityLogger.Log(LogType.Error, tag, text);
        }
    }
}