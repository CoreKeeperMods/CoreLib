// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: PrefabModificationAttribute.cs
// Author: Minepatcher, Limoka
// Created: 2025-12-15
// Description: 
// ========================================================

using System;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Attribute
{
    /// Use this attribute to register your prefab modification functions. Make sure to place one on the container class.
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    [MeansImplicitUse]
    public class PrefabModificationAttribute : System.Attribute
    {
        /// Specifies the type of entity that is targeted for a prefab modification.
        internal readonly Type targetType;
        
        /// Use this version to target specific prefabs
        /// <param name="targetType">target Prefab</param>
        public PrefabModificationAttribute(Type targetType)
        {
            if (!typeof(EntityMonoBehaviour).IsAssignableFrom(targetType))
                throw new ArgumentException($"Type '{targetType.FullName}' does not derive from {nameof(EntityMonoBehaviour)}!");

            this.targetType = targetType;
        }
    }
}