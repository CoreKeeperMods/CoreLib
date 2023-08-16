using System;
using HarmonyLib;

namespace CoreLib.Extensions
{
    public static class Extensions
    {
        public static void InvokeVoid(this object obj, string methodName, object[] args)
        {
            var method = obj.GetType().GetMethod(methodName, AccessTools.all);
            if (method == null)
                throw new MissingMethodException(obj.GetType().Name, methodName);
            method.Invoke(obj, args);
        }
        
        public static T Invoke<T>(this object obj, string methodName, object[] args)
        {
            var method = obj.GetType().GetMethod(methodName, AccessTools.all);
            if (method == null)
                throw new MissingMethodException(obj.GetType().Name, methodName);
            return (T)method.Invoke(obj, args);
        }
        
        public static T GetProperty<T>(this Type obj, string propertyName)
        {
            var property = obj.GetProperty(propertyName, AccessTools.all);
            if (property == null)
                throw new MissingMemberException(obj.Name, propertyName);

            return (T)property.GetValue(null);
        }
        
        public static T GetProperty<T>(this object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName, AccessTools.all);
            if (property == null)
                throw new MissingMemberException(obj.GetType().Name, propertyName);

            return (T)property.GetValue(obj);
        }
        
        public static T GetField<T>(this object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, AccessTools.all);
            if (field == null)
                throw new MissingFieldException(obj.GetType().Name, fieldName);

            return (T)field.GetValue(obj);
        }

        public static void SetField<T>(this object obj, string fieldName, T value)
        {
            var field = obj.GetType().GetField(fieldName, AccessTools.all);
            if (field == null)
                throw new MissingFieldException(obj.GetType().Name, fieldName);

            field.SetValue(obj, value);
        }

        public static bool IsAssignableTo(this Type type, Type otherType)
        {
            return otherType.IsAssignableFrom(type);
        }
    }
}