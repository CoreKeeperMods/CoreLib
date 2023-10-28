using UnityEngine;

namespace CoreLib.Editor
{
    public static class SafeExtensions
    {
        public static bool GetShouldBuildBurst(this ModBuilderSettings settings)
        {
            var field = settings.GetType().GetField("buildBurst");
            if (field == null)
            {
                Debug.LogWarning("WARNING: ModBuilderSettings does not contain 'buildBurst' field! Add it manually for compatibility with SDK Extensions. Assuming 'buildBurst' value is false.");
                return false;
            }

            return (bool)field.GetValue(settings);
        }
        
        public static bool GetShouldBuildForLinux(this ModBuilderSettings settings)
        {
            var field = settings.GetType().GetField("buildLinux");
            if (field == null)
            {
                Debug.LogWarning("WARNING: ModBuilderSettings does not contain 'buildLinux' field! Add it manually for compatibility with SDK Extensions. Assuming 'buildLinux' value is false.");
                return false;
            }

            return (bool)field.GetValue(settings);
        }
    }
}