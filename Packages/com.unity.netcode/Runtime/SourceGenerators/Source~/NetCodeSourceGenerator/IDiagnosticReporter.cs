using System;
using Microsoft.CodeAnalysis;

namespace Unity.NetCode.Generators
{
    /// <summary>
    /// Generic interface for reporting diagnostic issues
    /// </summary>
    internal interface IDiagnosticReporter
    {
        void LogInfo(string message, Location location);
        void LogInfo(string message,
            [System.Runtime.CompilerServices.CallerFilePath]
            string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber]
            int sourceLineNumber = 0);

        void LogWarning(string message, Location location);
        void LogWarning(string message,
            [System.Runtime.CompilerServices.CallerFilePath]
            string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber]
            int sourceLineNumber = 0);

        void LogError(string message, Location location);
        void LogError(string message,
            [System.Runtime.CompilerServices.CallerFilePath]
            string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber]
            int sourceLineNumber = 0);

        void LogException(Exception e, Location location);
        void LogException(Exception e,
            [System.Runtime.CompilerServices.CallerFilePath]
            string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber]
            int sourceLineNumber = 0);
    }
}
