using System;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity.Atributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    [MeansImplicitUse]
    public class PrefabModificationAttribute : Attribute
    {
        internal readonly Type targetType;

        public PrefabModificationAttribute() { }

        public PrefabModificationAttribute(Type targetType)
        {
            if (!typeof(EntityMonoBehaviour).IsAssignableFrom(targetType))
                throw new ArgumentException($"Type '{targetType.FullName}' does not derive from {nameof(EntityMonoBehaviour)}!");
            
            this.targetType = targetType;
        }
    }
}