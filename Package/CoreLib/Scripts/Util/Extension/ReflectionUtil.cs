// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: ReflectionUtil.cs
// Author: Minepatcher, Limoka
// Created: 2025-11-07
// Description: Provides reflection-based extension methods for safely invoking methods,
//              accessing fields or properties, and setting values on objects or types.
// ========================================================

using System;
using PugMod;

// ReSharper disable once CheckNamespace
namespace CoreLib.Util.Extension
{
    /// Provides reflection-based utilities for invoking methods, retrieving values,
    /// and modifying fields on objects and types within the CoreLib framework.
    /// <remarks>
    /// This class offers a consistent and safe API for reflection calls within CoreLib mods.
    /// It wraps the <see cref="API.Reflection"/> utilities, performing member validation before use.
    /// </remarks>
    /// <seealso cref="API.Reflection"/>
    /// <seealso cref="System.Reflection.MemberInfo"/>
    public static class ReflectionUtil
    {
        #region Method Invocation

        /// Invokes a method by name on the given object without expecting a return value.
        /// <param name="obj">The instance on which the method should be invoked.</param>
        /// <param name="methodName">The name of the method to invoke.</param>
        /// <param name="args">An array of arguments to pass to the method during invocation.</param>
        /// <exception cref="MissingFieldException">
        /// Thrown if no method with the specified name exists on the given object.
        /// </exception>
        /// <remarks>
        /// This method locates a member via <see cref="ModExtensions.FindMember(Type, string)"/> before calling
        /// <see cref="ModAPIReflection.Invoke(MemberInfo, object, object[])"/> internally.
        /// </remarks>
        /// <seealso cref="ModAPIReflection.Invoke(MemberInfo, object, object[])"/>
        public static void InvokeVoid(this object obj, string methodName, object[] args)
        {
            var method = obj.GetType().FindMember(methodName);
            if (method == null)
                throw new MissingFieldException(obj.GetType().GetNameChecked(), methodName);

            API.Reflection.Invoke(method, obj, args);
        }

        /// Invokes a method by name on the given object and returns its result.
        /// <typeparam name="T">The expected return type of the invoked method.</typeparam>
        /// <param name="obj">The instance on which the method should be invoked.</param>
        /// <param name="methodName">The name of the method to invoke.</param>
        /// <param name="args">An array of arguments to pass to the method during invocation.</param>
        /// <returns>The return value of the invoked method, cast to type <typeparamref name="T"/>.</returns>
        /// <exception cref="MissingFieldException">
        /// Thrown if the specified method is not found on the object.
        /// </exception>
        /// <seealso cref="ModAPIReflection.Invoke(MemberInfo, object, object[])"/>
        public static T Invoke<T>(this object obj, string methodName, object[] args)
        {
            var method = obj.GetType().FindMember(methodName);
            if (method != null)
                return (T)API.Reflection.Invoke(method, obj, args);

            throw new MissingFieldException(obj.GetType().GetNameChecked(), methodName);
        }

        #endregion

        #region Field and Property Access

        /// Retrieves the value of a specified field from the given object instance.
        /// <typeparam name="T">The expected type of the retrieved value.</typeparam>
        /// <param name="obj">The object instance to retrieve the field value from.</param>
        /// <param name="fieldName">The name of the field whose value is to be retrieved.</param>
        /// <returns>The field value cast to type <typeparamref name="T"/>.</returns>
        /// <exception cref="MissingFieldException">
        /// Thrown if the specified field does not exist on the object.
        /// </exception>
        /// <seealso cref="ModAPIReflection.GetValue(MemberInfo, object)"/>
        public static T GetValue<T>(this object obj, string fieldName)
        {
            var field = obj.GetType().FindMember(fieldName);
            if (field != null)
                return (T)API.Reflection.GetValue(field, obj);

            throw new MissingFieldException(obj.GetType().GetNameChecked(), fieldName);
        }

        /// Retrieves the value of a static field from the specified type.
        /// <typeparam name="T">The expected type of the retrieved value.</typeparam>
        /// <param name="type">The type containing the field to retrieve.</param>
        /// <param name="fieldName">The name of the field whose value is to be retrieved.</param>
        /// <returns>The field value cast to type <typeparamref name="T"/>.</returns>
        /// <exception cref="MissingFieldException">
        /// Thrown if the specified field is not found on the given type.
        /// </exception>
        /// <seealso cref="ModAPIReflection.GetValue(MemberInfo, object)"/>
        public static T GetValue<T>(this Type type, string fieldName)
        {
            var field = type.FindMember(fieldName);
            if (field != null)
                return (T)API.Reflection.GetValue(field, null);

            throw new MissingFieldException(type.GetNameChecked(), fieldName);
        }

        #endregion

        #region Value Setting

        /// Sets the value of a specified field on the given object instance.
        /// <typeparam name="T">The type of the value to assign.</typeparam>
        /// <param name="obj">The object instance whose field should be modified.</param>
        /// <param name="fieldName">The name of the field to modify.</param>
        /// <param name="value">The value to assign to the specified field.</param>
        /// <exception cref="MissingFieldException">
        /// Thrown if the specified field is not found on the object.
        /// </exception>
        /// <seealso cref="ModAPIReflection.SetValue(MemberInfo, object, object)"/>
        public static void SetValue<T>(this object obj, string fieldName, T value)
        {
            var field = obj.GetType().FindMember(fieldName);
            if (field == null)
                throw new MissingFieldException(obj.GetType().GetNameChecked(), fieldName);
            API.Reflection.SetValue(field, obj, value);
        }

        /// Sets the value of a static field on the given type.
        /// <typeparam name="T">The type of the value to assign.</typeparam>
        /// <param name="type">The type containing the static field.</param>
        /// <param name="fieldName">The name of the field to set.</param>
        /// <param name="value">The value to assign to the specified field.</param>
        /// <exception cref="MissingFieldException">
        /// Thrown if the specified field does not exist on the given type.
        /// </exception>
        /// <seealso cref="ModAPIReflection.SetValue(MemberInfo, object, object)"/>
        public static void SetValue<T>(this Type type, string fieldName, T value)
        {
            var field = type.FindMember(fieldName);
            if (field == null)
                throw new MissingFieldException(type.GetNameChecked(), fieldName);
            API.Reflection.SetValue(field, null, value);
        }

        #endregion
    }
}