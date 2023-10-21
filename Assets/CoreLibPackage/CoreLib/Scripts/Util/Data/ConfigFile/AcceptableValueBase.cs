﻿using System;
//All code in this folder is from BepInEx library and is licensed under LGPL-2.1 license.

namespace CoreLib.Data.Configuration
{
    /// <summary>
    ///     Base type of all classes representing and enforcing acceptable values of config settings.
    /// </summary>
    public abstract class AcceptableValueBase
    {
        /// <param name="valueType">Type of values that this class can Clamp.</param>
        protected AcceptableValueBase(Type valueType)
        {
            ValueType = valueType;
        }

        /// <summary>
        ///     Type of the supported values.
        /// </summary>
        public Type ValueType { get; }

        /// <summary>
        ///     Change the value to be acceptable, if it's not already.
        /// </summary>
        public abstract object Clamp(object value);

        /// <summary>
        ///     Check if the value is an acceptable value.
        /// </summary>
        public abstract bool IsValid(object value);

        /// <summary>
        ///     Get the string for use in config files.
        /// </summary>
        public abstract string ToDescriptionString();
    }
}
