using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using FieldInfo = System.Reflection.FieldInfo;
using Object = System.Object;


// ReSharper disable All
#pragma warning disable 8603
#pragma warning disable 8625
#pragma warning disable 8604
#pragma warning disable 8602
#pragma warning disable 8620
#pragma warning disable 8605

namespace CoreLib
{
    public static class Reflection {
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

        public static FieldInfo[] GetFieldsOfType<T>(this Type type)
        {
            return type
                .GetFields(AccessTools.all)
                .Where(info =>
                {
                    /*Il2CppReferenceArray<Il2CppSystem.Type> args = info.FieldType.GetGenericArguments();
                    if (args != null && args.Count > 0)
                    {
                        Il2CppSystem.Type genericType = args.Single();
                        return genericType.Equals(Il2CppType.Of<T>());
                    }*/

                    return info.FieldType.Equals(typeof(T));
                }).ToArray();
        }
    
        /// <summary>
        /// Does nothing, it's not il2cpp!
        /// </summary>
        /// <param name="obj">object to cast</param>
        /// <returns>cast object</returns>
        [Obsolete]
        public static object CastToActualType(this Object obj)
        {
            return obj;
        }

        public static bool IsAction<T>(MethodInfo method)
        {
            var parameters = method.GetParameters();
            return method.ReturnParameter.ParameterType == typeof(void) &&
                   parameters.Length == 1 &&
                   parameters[0].ParameterType == typeof(T);
        }

        public static bool HasAttribute<T>(MemberInfo type) where T : Attribute
        {
            return type.GetCustomAttribute<T>() != null;
        }
    }
}