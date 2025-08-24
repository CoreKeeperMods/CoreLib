using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Util
{
    /// <summary>
    /// Provides logging functionality with categorization support for Debug, Info, Warning, and Error messages.
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// Represents a string-based identifier used to categorize and tag log messages.
        /// </summary>
        private readonly string _tag;

        /// <summary>
        /// Logger Constructor, uses a tag name to identify and categorize logs.
        /// </summary>
        /// <param name="tag">The name of the mod you wish to log</param>
        public Logger(string tag)
        {
            _tag = $"[{tag}]";
        }

        /// <summary>
        /// Logs an Informational Message to the console.
        /// </summary>
        /// <param name="message">The Informational message to be logged.</param>
        public void LogInfo(string message)
        {
            Debug.unityLogger.Log(LogType.Log, _tag, message);
        }

        /// <summary>
        /// Logs a Warning Message to the console.
        /// </summary>
        /// <param name="message">The Warning message to be logged.</param>
        public void LogWarning(string message)
        {
            Debug.unityLogger.Log(LogType.Warning, _tag, message);
        }

        /// <summary>
        /// Logs an Error Message to the console.
        /// </summary>
        /// <param name="message">The Error message to be logged.</param>
        public void LogError(string message)
        {
            Debug.unityLogger.Log(LogType.Error, _tag, message);
        }
    }
}