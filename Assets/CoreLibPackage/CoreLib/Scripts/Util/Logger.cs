using UnityEngine;

namespace CoreLib.Util
{
    public class Logger
    {
        private readonly string tag;

        public Logger(string tag)
        {
            this.tag = $"[{tag}]";
        }

        public void LogDebug(string text)
        {
            UnityEngine.Debug.unityLogger.Log(LogType.Log, tag, text);
        }
        
        public void LogInfo(string text)
        {
            UnityEngine.Debug.unityLogger.Log(LogType.Log, tag, text);
        }
        
        public void LogWarning(string text)
        {
            UnityEngine.Debug.unityLogger.Log(LogType.Warning, tag, text);
        }
        
        public void LogError(string text)
        {
            UnityEngine.Debug.unityLogger.Log(LogType.Error, tag, text);
        }
    }
}