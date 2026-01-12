using System;
//All code in this folder is from BepInEx library and is licensed under LGPL-2.1 license.

// ReSharper disable once CheckNamespace
namespace CoreLib.Data.Configuration
{
    ///     Base type of all classes representing and enforcing acceptable values of config settings.
    public abstract class AcceptableValueBase
    {
        /// <param name="valueType">Type of values that this class can Clamp.</param>
        protected AcceptableValueBase(Type valueType)
        {
            ValueType = valueType;
        }

        ///     Type of the supported values.
        public Type ValueType { get; }

        ///     Change the value to be acceptable, if it's not already.
        public abstract object Clamp(object value);

        ///     Check if the value is an acceptable value.
        public abstract bool IsValid(object value);

        ///     Get the string for use in config files.
        public abstract string ToDescriptionString();
    }
}
