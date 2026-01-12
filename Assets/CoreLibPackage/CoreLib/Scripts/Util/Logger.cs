// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: Logger.cs
// Author: Minepatcher, Limoka
// Created: 2025-11-07
// Description: Provides categorized logging functionality for the Core Library mod,
//              supporting informational, warning, and error message outputs to the Unity console.
// ========================================================

using UnityEngine;
using static UnityEngine.Debug;

// ReSharper disable once CheckNamespace
namespace CoreLib.Util
{
    /// Provides categorized and consistent logging functionality for the Core Library mod.
    /// Supports multiple log levels such as Information, Warning, and Error.
    /// <remarks>
    /// This class wraps the Unity <see cref="Debug"/> logger to provide standardized
    /// and identifiable output for CoreLib modules and submodules.
    /// Each log message is prefixed with a tag that identifies the originating mod or module.
    /// </remarks>
    /// <seealso cref="CoreLibMod"/>
    /// <seealso cref="UnityEngine.Debug"/>
    public class Logger
    {
        #region Fields

        /// A string-based identifier used to categorize and tag log messages.
        /// <remarks>
        /// This tag helps distinguish messages in the Unity console, making it easier
        /// to filter logs by their originating module or subsystem.
        /// </remarks>
        private readonly string _tag;

        #endregion

        #region Constructor

        /// Initializes a new instance of the <see cref="Logger"/> class with a specific tag name.
        /// <param name="tag">
        /// The name of the mod or system component to associate with all log messages.
        /// This value is automatically enclosed in brackets when displayed in the console.
        /// </param>
        /// <example>
        /// <code>
        /// var log = new Logger("CoreLib");
        /// log.LogInfo("Initialization complete.");
        /// </code>
        /// </example>
        public Logger(string tag)
        {
            _tag = $"[{tag}]";
        }

        #endregion

        #region Logging Methods

        /// Logs an informational message to the Unity console.
        /// <param name="message">The message to be logged at the informational level.</param>
        /// <remarks>
        /// Use this method to report normal operational messages or status updates.
        /// </remarks>
        /// <seealso cref="LogWarning(string)"/>
        /// <seealso cref="LogError(string)"/>
        public void LogInfo(string message)
        {
            unityLogger.Log(LogType.Log, _tag, message);
        }

        /// Logs a warning message to the Unity console.
        /// <param name="message">The message to be logged at the warning level.</param>
        /// <remarks>
        /// Warnings indicate potential issues or unusual states that do not stop execution
        /// but may require user or developer attention.
        /// </remarks>
        /// <seealso cref="LogInfo(string)"/>
        /// <seealso cref="LogError(string)"/>
        public void LogWarning(string message)
        {
            unityLogger.Log(LogType.Warning, _tag, message);
        }

        /// Logs an error message to the Unity console.
        /// <param name="message">The message to be logged at the error level.</param>
        /// <remarks>
        /// Errors indicate failed operations or critical issues that require immediate attention.
        /// </remarks>
        /// <seealso cref="LogInfo(string)"/>
        /// <seealso cref="LogWarning(string)"/>
        public void LogError(string message)
        {
            unityLogger.Log(LogType.Error, _tag, message);
        }

        #endregion
    }
}