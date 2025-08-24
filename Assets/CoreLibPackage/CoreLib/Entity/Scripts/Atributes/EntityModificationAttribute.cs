using System;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Atributes
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
        public ObjectID Target = ObjectID.None;

        /// <summary>
        /// Specifies the target modification key for the entity being modified.
        /// </summary>
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