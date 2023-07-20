using UnityEngine;

namespace CoreLib
{
    public static class Logger
    {
        const string Tag = "[Core Lib]";

        public static void LogDebug(string text)
        {
            
            Debug.unityLogger.Log(LogType.Log, Tag, text);
        }
        
        public static void LogInfo(string text)
        {
            Debug.unityLogger.Log(LogType.Log, Tag, text);
        }
        
        public static void LogWarning(string text)
        {
            Debug.unityLogger.Log(LogType.Warning, Tag, text);
        }
        
        public static void LogError(string text)
        {
            Debug.unityLogger.Log(LogType.Error, Tag, text);
        }
    }
}