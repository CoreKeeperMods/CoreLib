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
    /// Provides utilities for invoking methods and accessing fields or properties on objects using reflection.
    public static class ReflectionUtil
    {
        /// Invokes a method with the specified name on the given object without expecting a return value.
        /// <param name="obj">The object instance on which the method is to be invoked.</param>
        /// <param name="methodName">The name of the method to invoke.</param>
        /// <param name="args">An array of arguments to pass to the method while invoking it.</param>
        /// <exception cref="MissingFieldException">Thrown when a method with the specified name is not found in the given object.</exception>
        public static void InvokeVoid(this object obj, string methodName, object[] args)
        {
            var method = obj.GetType()
                .GetMembersChecked()
                .FirstOrDefault(info => info.GetNameChecked().Equals(methodName));
            if (method == null)
                throw new MissingFieldException(obj.GetType().GetNameChecked(), methodName);

            API.Reflection.Invoke(method, obj, args);
        }

        /// Invokes a method with the specified name on the given object and returns the result of the invocation.
        /// <param name="obj">The object instance on which the method is to be invoked.</param>
        /// <param name="methodName">The name of the method to invoke.</param>
        /// <param name="args">An array of arguments to pass to the method while invoking it.</param>
        /// <typeparam name="T">The type of the return value expected from the invoked method.</typeparam>
        /// <returns>The result of the invoked method cast to the specified type.</returns>
        /// <exception cref="MissingFieldException">Thrown when a method with the specified name is not found in the given object.</exception>
        public static T Invoke<T>(this object obj, string methodName, object[] args)
        {
            var method = obj.GetType()
                .GetMembersChecked()
                .FirstOrDefault(info => info.GetNameChecked().Equals(methodName));
            if (method == null)
                throw new MissingFieldException(obj.GetType().GetNameChecked(), methodName);

            return (T)API.Reflection.Invoke(method, obj, args);
        }

        /// Retrieves the value of the specified field from the given instance of an object.
        /// <param name="obj">The object instance from which the field value is to be retrieved.</param>
        /// <param name="fieldName">The name of the field whose value is to be retrieved.</param>
        /// <typeparam name="T">The type of the value to retrieve from the field.</typeparam>
        /// <returns>The value of the specified field cast to the specified type.</returns>
        /// <exception cref="MissingFieldException">Thrown when the specified field is not found in the given object.</exception>
        public static T GetValue<T>(this object obj, string fieldName)
        {
            var field = obj.GetType()
                .GetMembersChecked()
                .FirstOrDefault(info => info.GetNameChecked().Equals(fieldName));
            if (field == null)
                throw new MissingFieldException(obj.GetType().GetNameChecked(), fieldName);

            return (T)API.Reflection.GetValue(field, obj);
        }

        /// Retrieves the value of the specified field from the given type.
        /// <param name="type">The type from which the field value is to be retrieved.</param>
        /// <param name="fieldName">The name of the field whose value is to be retrieved.</param>
        /// <typeparam name="T">The type of the value to retrieve from the field.</typeparam>
        /// <returns>The value of the specified field cast to the specified type.</returns>
        /// <exception cref="MissingFieldException">Thrown when the specified field is not found in the given type.</exception>
        public static T GetValue<T>(this Type type, string fieldName)
        {
            var field = 
                type.GetMembersChecked()
                .FirstOrDefault(info => info.GetNameChecked().Equals(fieldName));
            if (field == null)
                throw new MissingFieldException(type.GetNameChecked(), fieldName);

            return (T)API.Reflection.GetValue(field, null);
        }

        /// Sets the value of the specified field for the given object instance.
        /// <param name="obj">The object instance whose field is to be set.</param>
        /// <param name="fieldName">The name of the field to set the value for.</param>
        /// <param name="value">The value to be assigned to the specified field.</param>
        public static void SetValue<T>(this object obj, string fieldName, T value)
        {
            var field = obj.GetType()
                .GetMembersChecked()
                .FirstOrDefault(info => info.GetNameChecked().Equals(fieldName));
            if (field == null)
                throw new MissingFieldException(obj.GetType().GetNameChecked(), fieldName);

            API.Reflection.SetValue(field, obj, value);
        }

        /// Sets the value of the specified field for the given object instance.
        /// <param name="type">The type from which the field value is to be set.</param>
        /// <param name="fieldName">The name of the field to set the value for.</param>
        /// <param name="value">The value to assign to the specified field.</param>
        /// <exception cref="MissingFieldException">Thrown when a field with the specified name is not found in the given object.</exception>
        public static void SetValue<T>(this Type type, string fieldName, T value)
        {
            var field =
                type.GetMembersChecked()
                    .FirstOrDefault(info => info.GetNameChecked().Equals(fieldName));
            if (field == null)
                throw new MissingFieldException(type.GetNameChecked(), fieldName);

            API.Reflection.SetValue(field, null, value);
        }

        /// Determines whether the current type is assignable to the specified other type.
        /// <param name="type">The type to check for assignability.</param>
        /// <param name="otherType">The target type to check against.</param>
        /// <returns>True if the current type can be assigned to the specified other type; otherwise, false.</returns>
        public static bool IsAssignableTo(this Type type, Type otherType)
        {
            return otherType.IsAssignableFrom(type);
        }
    }
}