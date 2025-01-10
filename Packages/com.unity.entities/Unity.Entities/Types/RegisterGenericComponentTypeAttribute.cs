using System;

namespace Unity.Entities
{
    /// <summary>
    /// When added as an assembly-level attribute, allows creating component type reflection data for instances of generic components
    /// for the <see cref="TypeManager"/> to reason about.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class RegisterGenericComponentTypeAttribute : Attribute
    {
        /// <summary>
        /// Fully closed generic component type to register with the <see cref="TypeManager"/>
        /// </summary>
        public Type ConcreteType;

        /// <summary>
        /// Registers a fully closed generic component type with the <see cref="TypeManager"/>
        /// </summary>
        /// <param name="type">The component type.</param>
        public RegisterGenericComponentTypeAttribute(Type type)
        {
            ConcreteType = type;
        }
    }
}
