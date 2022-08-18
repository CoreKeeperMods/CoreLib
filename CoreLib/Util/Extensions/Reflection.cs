﻿#nullable enable
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;


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
    }
}