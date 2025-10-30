using UnityEngine;
using static UnityEngine.Debug;

// ReSharper disable once CheckNamespace
namespace CoreLib.Util
{
    /// Provides logging functionality with categorization support for Debug, Info, Warning, and Error messages.
    public class Logger
    {
        /// Represents a string-based identifier used to categorize and tag log messages.
        private readonly string _tag;

        /// <summary>
        /// Logger Constructor, uses a tag name to identify and categorize logs.
        /// </summary>
        /// <param name="tag">The name of the mod you wish to log</param>
        public Logger(string tag) => _tag = $"[{tag}]";

        /// Logs an Informational Message to the console.
        /// <param name="message">The Informational message to be logged.</param>
        public void LogInfo(string message) => unityLogger.Log(LogType.Log, _tag, message);

        /// Logs a Warning Message to the console.
        /// <param name="message">The Warning message to be logged.</param>
        public void LogWarning(string message) => unityLogger.Log(LogType.Warning, _tag, message);

        /// Logs an Error Message to the console.
        /// <param name="message">The Error message to be logged.</param>
        public void LogError(string message) => unityLogger.Log(LogType.Error, _tag, message);
    }
}