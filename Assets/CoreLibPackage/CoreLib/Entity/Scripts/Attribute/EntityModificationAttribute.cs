// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: EntityModificationAttribute.cs
// Author: Minepatcher, Limoka
// Created: 2025-12-15
// Description: 
// ========================================================

using System;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Attribute
{
    /// Use this attribute to register your entity modification functions. Make sure to place one on the container class.
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    [MeansImplicitUse]
    public class EntityModificationAttribute : System.Attribute
    {
        /// Represents the target entity identifier for modifications.
        public ObjectID target;

        /// Specifies the target modification key for the entity being modified.
        public string modTarget;
        
        /// Use this version to target vanilla entity
        /// <param name="target">Vanilla entity ID</param>
        public EntityModificationAttribute(ObjectID target)
        {
            this.target = target;
            modTarget = "";
        }

        /// Use this version to target modded entity
        /// <param name="modTarget">Modded entity ID</param>
        public EntityModificationAttribute(string modTarget)
        {
            this.modTarget = modTarget;
            target = ObjectID.None;
        }
    }
}