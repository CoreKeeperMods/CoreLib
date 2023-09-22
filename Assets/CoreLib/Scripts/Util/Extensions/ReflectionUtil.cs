using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using PugMod;

// ReSharper disable All
#pragma warning disable 8603
#pragma warning disable 8625
#pragma warning disable 8604
#pragma warning disable 8602
#pragma warning disable 8620
#pragma warning disable 8605

namespace CoreLib.Util.Extensions
{
    public static class ReflectionUtil
    {
        public static void InvokeVoid(this object obj, string methodName, object[] args)
        {
            var method = obj.GetType().GetMethod(methodName, AccessTools.all);
            if (method == null)
                throw new MissingMethodException(obj.GetType().GetNameChecked(), methodName);
            API.Reflection.Invoke(method, obj, args);
        }

        public static T Invoke<T>(this object obj, string methodName, object[] args)
        {
            var method = obj.GetType().GetMethod(methodName, AccessTools.all);
            if (method == null)
                throw new MissingMethodException(obj.GetType().GetNameChecked(), methodName);
            return (T)API.Reflection.Invoke(method, obj, args);
        }

        public static T GetProperty<T>(this Type obj, string propertyName)
        {
            var property = obj.GetProperty(propertyName, AccessTools.all);
            if (property == null)
                throw new MissingMemberException(obj.GetNameChecked(), propertyName);
            
            return (T)API.Reflection.GetValue(property, null);
        }

        public static T GetProperty<T>(this object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName, AccessTools.all);
            if (property == null)
                throw new MissingMemberException(obj.GetType().GetNameChecked(), propertyName);

            return (T)API.Reflection.GetValue(property, obj);
        }

        public static T GetField<T>(this object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, AccessTools.all);
            if (field == null)
                throw new MissingFieldException(obj.GetType().GetNameChecked(), fieldName);

            return (T)API.Reflection.GetValue(field, obj);
        }

        public static void SetField<T>(this object obj, string fieldName, T value)
        {
            var field = obj.GetType().GetField(fieldName, AccessTools.all);
            if (field == null)
                throw new MissingFieldException(obj.GetType().GetNameChecked(), fieldName);

            API.Reflection.SetValue(field, obj, value);
        }

        public static bool IsAssignableTo(this Type type, Type otherType)
        {
            return otherType.IsAssignableFrom(type);
        }

        public static FieldInfo[] GetFieldsOfType<T>(this Type type)
        {
            return type
                .GetFields(AccessTools.all)
                .Where(info => { return info.FieldType.Equals(typeof(T)); }).ToArray();
        }
    }
}