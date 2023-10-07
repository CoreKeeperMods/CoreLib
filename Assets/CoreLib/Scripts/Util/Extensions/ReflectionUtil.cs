using System;
using System.Linq;
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
            var method = obj.GetType()
                .GetMembersChecked()
                .FirstOrDefault(info => info.GetNameChecked().Equals(methodName));
            if (method == null)
                throw new MissingFieldException(obj.GetType().GetNameChecked(), methodName);

            API.Reflection.Invoke(method, obj, args);;
        }

        public static T Invoke<T>(this object obj, string methodName, object[] args)
        {
            var method = obj.GetType()
                .GetMembersChecked()
                .FirstOrDefault(info => info.GetNameChecked().Equals(methodName));
            if (method == null)
                throw new MissingFieldException(obj.GetType().GetNameChecked(), methodName);

            return (T)API.Reflection.Invoke(method, obj, args);
        }

        public static T GetValue<T>(this object obj, string fieldName)
        {
            var field = obj.GetType()
                .GetMembersChecked()
                .FirstOrDefault(info => info.GetNameChecked().Equals(fieldName));
            if (field == null)
                throw new MissingFieldException(obj.GetType().GetNameChecked(), fieldName);

            return (T)API.Reflection.GetValue(field, obj);
        }
        
        public static T GetValue<T>(this Type type, string fieldName)
        {
            var field = 
                type.GetMembersChecked()
                .FirstOrDefault(info => info.GetNameChecked().Equals(fieldName));
            if (field == null)
                throw new MissingFieldException(type.GetNameChecked(), fieldName);

            return (T)API.Reflection.GetValue(field, null);
        }
        
        public static void SetValue<T>(this object obj, string fieldName, T value)
        {
            var field = obj.GetType()
                .GetMembersChecked()
                .FirstOrDefault(info => info.GetNameChecked().Equals(fieldName));
            if (field == null)
                throw new MissingFieldException(obj.GetType().GetNameChecked(), fieldName);

            API.Reflection.SetValue(field, obj, value);
        }
        
        public static void SetValue<T>(this Type type, string fieldName, T value)
        {
            var field = 
                type.GetMembersChecked()
                    .FirstOrDefault(info => info.GetNameChecked().Equals(fieldName));
            if (field == null)
                throw new MissingFieldException(type.GetNameChecked(), fieldName);

            API.Reflection.SetValue(field, null, value);
        }

        public static bool IsAssignableTo(this Type type, Type otherType)
        {
            return otherType.IsAssignableFrom(type);
        }
/*
        public static FieldInfo[] GetFieldsOfType<T>(this Type type)
        {
            return type
                .GetFields(AccessTools.all)
                .Where(info => { return info.FieldType.Equals(typeof(T)); }).ToArray();
        }*/
    }
}