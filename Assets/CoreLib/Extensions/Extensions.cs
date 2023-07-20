using System;
using HarmonyLib;

namespace CoreLib.Extensions
{
    public static class Extensions
    {
        public static T GetField<T>(this object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, AccessTools.all);
            return (T)field.GetValue(obj);
        }
        
        public static void SetField<T>(this object obj, string fieldName, T value)
        {
            try
            {
                obj.GetType().GetField(fieldName, AccessTools.all).SetValue(obj, value);
            } catch (Exception e)
            {
                Logger.LogWarning(e.ToString());
            }
        }

        public static bool IsAssignableTo(this Type type, Type otherType)
        {
            return otherType.IsAssignableFrom(type);
        }
    }
}