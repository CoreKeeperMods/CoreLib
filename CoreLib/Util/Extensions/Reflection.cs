using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using Object = Il2CppSystem.Object;


// ReSharper disable All
#pragma warning disable 8603
#pragma warning disable 8625
#pragma warning disable 8604
#pragma warning disable 8602
#pragma warning disable 8620
#pragma warning disable 8605

// Source code is taken from R2API: https://github.com/risk-of-thunder/R2API/tree/master

namespace CoreLib
{
    public static class Reflection {
    private const BindingFlags AllFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                              BindingFlags.Instance | BindingFlags.DeclaredOnly;
    
        internal static bool IsSubTypeOf(this TypeDefinition typeDefinition, string typeFullName) {
            if (typeDefinition.FullName == typeFullName) {
                return true;
            }

            var typeDefBaseType = typeDefinition.BaseType?.Resolve();
            while (typeDefBaseType != null) {
                if (typeDefBaseType.FullName == typeFullName) {
                    return true;
                }

                typeDefBaseType = typeDefBaseType.BaseType?.Resolve();
            }

            return false;
        }

        public static System.Reflection.FieldInfo GetNestedField(Type type, string fieldName) {
            var nestedTypes = type.GetNestedTypes(AllFlags);
            foreach (Type nestedType in nestedTypes) {
                var fieldInfo = nestedType.GetField(fieldName, AllFlags);
                if (fieldInfo != null) {
                    return fieldInfo;
                }
            }
            return null;
        }

        public static System.Reflection.MethodInfo GetNestedMethod(Type type, string methodName) {
            var nestedTypes = type.GetNestedTypes(AllFlags);
            foreach (Type nestedType in nestedTypes) {
                var methodInfo = nestedType.GetMethod(methodName, AllFlags);
                if (methodInfo != null) {
                    return methodInfo;
                }
            }
            return null;
        }

        public static MethodInfo GetGenericMethod(Type type, string name, Type[] parameters) {
            var classMethods = type.GetMethods(AllFlags);
            foreach (System.Reflection.MethodInfo methodInfo in classMethods) {
                if (methodInfo.Name == name) {
                    System.Reflection.ParameterInfo[] parameterInfos = methodInfo.GetParameters();
                    if (parameterInfos.Length == parameters.Length) {
                        bool parameterMatch = true;
                        for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++) {
                            if (parameterInfos[parameterIndex].ParameterType.Name != parameters[parameterIndex].Name) {
                                parameterMatch = false;
                                break;
                            }
                        }
                        if (parameterMatch) {
                            return methodInfo;
                        }
                    }
                }
            }
            return null;
        }

        private static MethodInfo m_getDelegate;
        private static MethodInfo getDelegate
        {
            get
            {
                if (m_getDelegate == null)
                    m_getDelegate = typeof(Marshal).GetMethod("GetDelegateForFunctionPointerInternal", AllFlags);
                return m_getDelegate;
            }
        }
        
        public unsafe static object CallBase<T, TDelegate>(this Object obj, string name, params object[] args)
            where T : Object
            where TDelegate : Delegate
        {
            Type delegateType = typeof(TDelegate);
            MethodInfo method = delegateType.GetMethod("Invoke");
            Type returnType = method.ReturnType;
            Type[] paramTypes = method.GetParameters().Select(info => info.ParameterType).ToArray();
            string[] parameters = paramTypes.Select(info => info.FullName).ToArray();
            
            IntPtr klass = Il2CppClassPointerStore<T>.NativeClassPtr;
            IntPtr methodPtr = IL2CPP.GetIl2CppMethod(klass, false, name, returnType.FullName, parameters);
            Type il2cppDelegateType = Expression.GetDelegateType(paramTypes.InsertIl2CppDef(typeof(IntPtr), typeof(Il2CppMethodInfo*), returnType));
            
            Delegate funcDelegate = (Delegate)getDelegate.Invoke(null, new object[] { *(IntPtr*)methodPtr, il2cppDelegateType });
            return funcDelegate.DynamicInvoke(args.InsertIl2CppArgs(obj.Pointer, IntPtr.Zero));
        }

        private static T[] InsertIl2CppDef<T>(this T[] array, T first, T method, T returnType)
        {
            T[] newArray = new T[array.Length + 3];
            Array.Copy(array, 0, newArray, 1, array.Length);
            newArray[0] = first;
            newArray[^2] = method;
            newArray[^1] = returnType;
            return newArray;
        }
        
        private static T[] InsertIl2CppArgs<T>(this T[] array, T first, T last)
        {
            T[] newArray = new T[array.Length + 2];
            Array.Copy(array, 0, newArray, 1, array.Length);
            newArray[0] = first;
            newArray[^1] = last;
            return newArray;
        }

        private unsafe delegate void BaseFunc(IntPtr arg1, Il2CppMethodInfo* arg2);
        public unsafe static void CallBase<T>(this Object obj, string name)
        where T : Object
        {
            IntPtr klass = Il2CppClassPointerStore<T>.NativeClassPtr;
            IntPtr methodPtr = IL2CPP.GetIl2CppMethod(klass, false, name, "System.Void", Array.Empty<string>());
            BaseFunc baseFunc = Marshal.GetDelegateForFunctionPointer<BaseFunc>(*(IntPtr*)methodPtr);
            baseFunc?.Invoke(obj.Pointer, (Il2CppMethodInfo*)IntPtr.Zero);
        }
    }
}