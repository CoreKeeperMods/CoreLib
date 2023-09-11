using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using FieldInfo = System.Reflection.FieldInfo;

// ReSharper disable All
#pragma warning disable 8603
#pragma warning disable 8625
#pragma warning disable 8604
#pragma warning disable 8602
#pragma warning disable 8620
#pragma warning disable 8605

namespace CoreLib
{
    public static class Reflection
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

        public static FieldInfo[] GetFieldsOfType<T>(this Type type)
        {
            return type
                .GetFields(AccessTools.all)
                .Where(info => { return info.FieldType.Equals(typeof(T)); }).ToArray();
        }

        public static bool HasAttribute<T>(MemberInfo type) where T : Attribute
        {
            return type.GetCustomAttribute<T>() != null;
        }

        public static string GetSignature(this MethodInfo member)
        {
            if (member == null)
                return "null";
            StringBuilder stringBuilder = new StringBuilder();
            
            stringBuilder.Append(member.ReturnType.FullDescription());
            stringBuilder.Append(" ");
            
            string str = ((IEnumerable<ParameterInfo>) member.GetParameters()).Join<ParameterInfo>((Func<ParameterInfo, string>) (p => p.ParameterType.FullDescription() + " " + p.Name));
            stringBuilder.Append("(");
            stringBuilder.Append(str);
            stringBuilder.Append(")");
            return stringBuilder.ToString();
        }
        
        public static int RegisterAttributeFunction<TAttr, TDel>(Type type, Func<TDel, TAttr, bool> handler)
            where TAttr : Attribute
            where TDel : Delegate
        {
            int modifiersCount = 0;

            IEnumerable<MethodInfo> methods = type
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic)
                .Where(Reflection.HasAttribute<TAttr>);
        
            foreach (MethodInfo method in methods)
            {
                TDel methodDelegate = (TDel)Delegate.CreateDelegate(typeof(TDel), method, false);
                if (methodDelegate == null)
                {
                    MethodInfo delegateInvoke = typeof(TDel).GetMethod("Invoke");
                    CoreLibMod.Log.LogError(
                        $"Failed to add modify method '{method.FullDescription()}', because method signature is incorrect. Should be {delegateInvoke.GetSignature()}!");
                    continue;
                }

                var attributes = method.GetCustomAttributes<TAttr>();

                foreach (TAttr attribute in attributes)
                {
                    if (handler(methodDelegate, attribute))
                        modifiersCount++;
                }
            }

            return modifiersCount;
        }
    }
}