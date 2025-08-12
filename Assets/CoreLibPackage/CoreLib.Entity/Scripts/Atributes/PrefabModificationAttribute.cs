using System;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity.Atributes
{
    /// <summary>
    /// Use this attribute to register your prefab modification functions. Make sure to place one on the container class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    [MeansImplicitUse]
    public class PrefabModificationAttribute : Attribute
    {
        /// <summary>
        /// Specifies the type of entity that is targeted for a prefab modification.
        /// </summary>
        /// <remarks>
        /// This variable is utilized in conjunction with the <see cref="PrefabModificationAttribute"/> to associate a
        /// prefab modification function or class with a specific target type that derives from <see cref="EntityMonoBehaviour"/>.
        /// </remarks>
        internal readonly Type TargetType;

        /// <summary>
        /// Use this version to target ALL prefabs. You will need to perform checks yourself if this entity is a target.
        /// </summary>
        public PrefabModificationAttribute() { }

        /// <summary>
        /// Use this version to target specific prefabs
        /// </summary>
        /// <param name="target">target Prefab</param>
        public PrefabModificationAttribute(Type targetType)
        {
            if (!typeof(EntityMonoBehaviour).IsAssignableFrom(targetType))
                throw new ArgumentException(
                    $"Type '{targetType.FullName}' does not derive from {nameof(EntityMonoBehaviour)}!");

            TargetType = targetType;
        }
    }
}