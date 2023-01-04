using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime.Runtime;
using Mono.Cecil;
using FieldInfo = Il2CppSystem.Reflection.FieldInfo;
using Object = Il2CppSystem.Object;


// ReSharper disable All
#pragma warning disable 8603
#pragma warning disable 8625
#pragma warning disable 8604
#pragma warning disable 8602
#pragma warning disable 8620
#pragma warning disable 8605

namespace CoreLib;

public static class Reflection {

    internal const Il2CppSystem.Reflection.BindingFlags all = Il2CppSystem.Reflection.BindingFlags.Instance | Il2CppSystem.Reflection.BindingFlags.Static |
                                                              Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic |
                                                              Il2CppSystem.Reflection.BindingFlags.GetField | Il2CppSystem.Reflection.BindingFlags.SetField |
                                                              Il2CppSystem.Reflection.BindingFlags.GetProperty |
                                                              Il2CppSystem.Reflection.BindingFlags.SetProperty;


    public static void TryInvokeAction(this Object target, string methodName)
    {
        Il2CppSystem.Reflection.MethodInfo method = target?.GetIl2CppType()?.GetMethod(methodName, Reflection.all);
        if (method != null)
        {
            method.Invoke(target, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
        }
    }
    
    public static Object TryInvokeFunc(this Object target, string methodName)
    {
        if (target == null) return null;
        
        Il2CppSystem.Reflection.MethodInfo method = target.GetIl2CppType().GetMethod(methodName, Reflection.all);
        if (method != null)
        {
            return method.Invoke(target, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
        }

        return null;
    }
    
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
        var nestedTypes = type.GetNestedTypes(AccessTools.all);
        foreach (Type nestedType in nestedTypes) {
            var fieldInfo = nestedType.GetField(fieldName, AccessTools.all);
            if (fieldInfo != null) {
                return fieldInfo;
            }
        }
        return null;
    }

    public static System.Reflection.MethodInfo GetNestedMethod(Type type, string methodName) {
        var nestedTypes = type.GetNestedTypes(AccessTools.all);
        foreach (Type nestedType in nestedTypes) {
            var methodInfo = nestedType.GetMethod(methodName, AccessTools.all);
            if (methodInfo != null) {
                return methodInfo;
            }
        }
        return null;
    }

    public static MethodInfo GetGenericMethod(Type type, string name, Type[] parameters) {
        var classMethods = type.GetMethods(AccessTools.all);
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
                m_getDelegate = typeof(Marshal).GetMethod("GetDelegateForFunctionPointerInternal", AccessTools.all);
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
        
        for (int i = 0; i < paramTypes.Length; i++)
        {
            if (paramTypes[i].IsAssignableTo(typeof(Il2CppObjectBase)))
            {
                args[i] = IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase) args[i]);
                paramTypes[i] = typeof(IntPtr);
            }
        }
            
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

    public static FieldInfo GetFieldOfType<T>(this Il2CppSystem.Type type)
    {
        return type
            .GetFields(all)
            .First(info =>
            {
                /*Il2CppReferenceArray<Il2CppSystem.Type> args = info.FieldType.GetGenericArguments();
                if (args != null && args.Count > 0)
                {
                    Il2CppSystem.Type genericType = args.Single();
                    return genericType.Equals(Il2CppType.Of<T>());
                }*/

                return info.FieldType.Equals(Il2CppType.Of<T>());
            });
    }
}