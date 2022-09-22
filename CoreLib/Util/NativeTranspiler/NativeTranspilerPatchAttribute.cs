using System;
using System.Collections.Generic;
using HarmonyLib;

namespace CoreLib.Util;

public class NativeTranspilerPatchAttribute : Attribute
{
    public HarmonyMethod info = new HarmonyMethod();
    public int capacity = 2000;
    
    /// <summary>An annotation that specifies a method, property or constructor to patch</summary>
    /// <param name="declaringType">The declaring class/type</param>
    /// <param name="methodName">The name of the method, property or constructor to patch</param>
    public NativeTranspilerPatchAttribute(Type declaringType, string methodName, int capacity = 2000)
    {
        info.declaringType = declaringType;
        info.methodName = methodName;
        this.capacity = capacity;
        info.methodType = MethodType.Normal;
    }

    /// <summary>An annotation that specifies a method, property or constructor to patch</summary>
    /// <param name="declaringType">The declaring class/type</param>
    /// <param name="methodName">The name of the method, property or constructor to patch</param>
    /// <param name="argumentTypes">An array of argument types to target overloads</param>
    public NativeTranspilerPatchAttribute(Type declaringType, string methodName, params Type[] argumentTypes)
    {
        info.declaringType = declaringType;
        info.methodName = methodName;
        info.argumentTypes = argumentTypes;
        info.methodType = MethodType.Normal;
    }
    
    /// <summary>An annotation that specifies a method, property or constructor to patch</summary>
    /// <param name="declaringType">The declaring class/type</param>
    /// <param name="methodName">The name of the method, property or constructor to patch</param>
    /// <param name="argumentTypes">An array of argument types to target overloads</param>
    public NativeTranspilerPatchAttribute(Type declaringType, string methodName, int capacity, params Type[] argumentTypes)
    {
        info.declaringType = declaringType;
        info.methodName = methodName;
        info.argumentTypes = argumentTypes;
        this.capacity = capacity;
        info.methodType = MethodType.Normal;
    }

    /// <summary>An annotation that specifies a method, property or constructor to patch</summary>
    /// <param name="declaringType">The declaring class/type</param>
    /// <param name="methodName">The name of the method, property or constructor to patch</param>
    /// <param name="argumentTypes">An array of argument types to target overloads</param>
    /// <param name="argumentVariations">Array of <see cref="T:HarmonyLib.ArgumentType" /></param>
    public NativeTranspilerPatchAttribute(
        Type declaringType,
        string methodName,
        Type[] argumentTypes,
        ArgumentType[] argumentVariations, int capacity = 2000)
    {
        info.declaringType = declaringType;
        info.methodName = methodName;
        ParseSpecialArguments(argumentTypes, argumentVariations);
        this.capacity = capacity;
        info.methodType = MethodType.Normal;
    }

    /// <summary>An annotation that specifies a method, property or constructor to patch</summary>
    /// <param name="declaringType">The declaring class/type</param>
    /// <param name="methodType">The <see cref="T:HarmonyLib.MethodType" /></param>
    public NativeTranspilerPatchAttribute(Type declaringType, MethodType methodType, int capacity = 2000)
    {
        info.declaringType = declaringType;
        info.methodType = methodType;
        this.capacity = capacity;
    }

    /// <summary>An annotation that specifies a method, property or constructor to patch</summary>
    /// <param name="declaringType">The declaring class/type</param>
    /// <param name="methodType">The <see cref="T:HarmonyLib.MethodType" /></param>
    /// <param name="argumentTypes">An array of argument types to target overloads</param>
    public NativeTranspilerPatchAttribute(Type declaringType, MethodType methodType, params Type[] argumentTypes)
    {
        info.declaringType = declaringType;
        info.methodType = methodType;
        info.argumentTypes = argumentTypes;
    }
    
    /// <summary>An annotation that specifies a method, property or constructor to patch</summary>
    /// <param name="declaringType">The declaring class/type</param>
    /// <param name="methodType">The <see cref="T:HarmonyLib.MethodType" /></param>
    /// <param name="argumentTypes">An array of argument types to target overloads</param>
    public NativeTranspilerPatchAttribute(Type declaringType, MethodType methodType, int capacity, params Type[] argumentTypes)
    {
        info.declaringType = declaringType;
        info.methodType = methodType;
        info.argumentTypes = argumentTypes;
        this.capacity = capacity;
    }

    /// <summary>An annotation that specifies a method, property or constructor to patch</summary>
    /// <param name="declaringType">The declaring class/type</param>
    /// <param name="methodType">The <see cref="T:HarmonyLib.MethodType" /></param>
    /// <param name="argumentTypes">An array of argument types to target overloads</param>
    /// <param name="argumentVariations">Array of <see cref="T:HarmonyLib.ArgumentType" /></param>
    public NativeTranspilerPatchAttribute(
        Type declaringType,
        MethodType methodType,
        Type[] argumentTypes,
        ArgumentType[] argumentVariations,
        int capacity = 2000)
    {
        info.declaringType = declaringType;
        info.methodType = methodType;
        ParseSpecialArguments(argumentTypes, argumentVariations);
        this.capacity = capacity;
    }

    /// <summary>An annotation that specifies a method, property or constructor to patch</summary>
    /// <param name="declaringType">The declaring class/type</param>
    /// <param name="methodName">The name of the method, property or constructor to patch</param>
    /// <param name="methodType">The <see cref="T:HarmonyLib.MethodType" /></param>
    public NativeTranspilerPatchAttribute(Type declaringType, string methodName, MethodType methodType, int capacity = 2000)
    {
        info.declaringType = declaringType;
        info.methodName = methodName;
        info.methodType = methodType;
        this.capacity = capacity;
    }

    private void ParseSpecialArguments(Type[] argumentTypes, ArgumentType[] argumentVariations)
    {
        if (argumentVariations == null || argumentVariations.Length == 0)
        {
            info.argumentTypes = argumentTypes;
        }
        else
        {
            if (argumentTypes.Length < argumentVariations.Length)
                throw new ArgumentException("argumentVariations contains more elements than argumentTypes", nameof(argumentVariations));
            List<Type> typeList = new List<Type>();
            for (int index = 0; index < argumentTypes.Length; ++index)
            {
                Type type = argumentTypes[index];
                switch (argumentVariations[index])
                {
                    case ArgumentType.Ref:
                    case ArgumentType.Out:
                        type = type.MakeByRefType();
                        break;
                    case ArgumentType.Pointer:
                        type = type.MakePointerType();
                        break;
                }

                typeList.Add(type);
            }

            info.argumentTypes = typeList.ToArray();
        }
    }
}