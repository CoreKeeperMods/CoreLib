using System;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity.Atributes
{
    /// <summary>
    /// Use this attribute to register your entity modification functions. Make sure to place one on the container class
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    [MeansImplicitUse]
    public class EntityModificationAttribute : Attribute
    {
        /// <summary>
        /// Represents the target entity identifier for modifications.
        /// </summary>
        /// <remarks>
        /// Utilized within the <see cref="EntityModificationAttribute"/> to determine which specific entity
        /// should be affected by the modification logic. Acts as an identifier to link modifications to their
        /// respective entities in a structured manner.
        /// </remarks>
        public ObjectID Target = ObjectID.None;

        /// <summary>
        /// Specifies the target modification key for the entity being modified.
        /// </summary>
        /// <remarks>
        /// Used within the <see cref="EntityModificationAttribute"/> to identify specific entities in a modded context.
        /// When this value is assigned, it is used as a unique identifier for modifications to be applied.
        /// </remarks>
        public string ModTarget = "";

        /// <summary>
        /// Use this version to target ALL entities. You will need to perform checks yourself if this entity is a target.
        /// </summary>
        public EntityModificationAttribute() { }
    
        /// <summary>
        /// Use this version to target vanilla entity
        /// </summary>
        /// <param name="target">Vanilla entity ID</param>
        public EntityModificationAttribute(ObjectID target)
        {
            Target = target;
            ModTarget = "";
        }

        /// <summary>
        /// Use this version to target modded entity
        /// </summary>
        /// <param name="modTarget">Modded entity ID</param>
        public EntityModificationAttribute(string modTarget)
        {
            ModTarget = modTarget;
        }
    }
}