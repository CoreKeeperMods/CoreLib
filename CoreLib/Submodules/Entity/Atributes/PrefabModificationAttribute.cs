using System;

namespace CoreLib.Submodules.ModEntity.Atributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class PrefabModificationAttribute : Attribute
    {
        internal readonly Type targetType;

        public PrefabModificationAttribute() { }

        public PrefabModificationAttribute(Type targetType)
        {
            if (!targetType.IsAssignableTo(typeof(EntityMonoBehaviour)))
                throw new ArgumentException($"Type '{targetType.FullName}' does not derive from {nameof(EntityMonoBehaviour)}!");
            
            this.targetType = targetType;
        }
    }
}