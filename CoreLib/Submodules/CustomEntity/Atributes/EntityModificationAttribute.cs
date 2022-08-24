using System;

namespace CoreLib.Submodules.CustomEntity.Atributes;

/// <summary>
/// Use this attribute to register your entity modification functions. Make sure to place one on the container class
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class EntityModificationAttribute : Attribute
{
    public ObjectID target = ObjectID.None;
    public string modTarget;

    public EntityModificationAttribute() { }
    
    /// <summary>
    /// Use this version to target vanilla entity
    /// </summary>
    /// <param name="target">Vanilla entity ID</param>
    public EntityModificationAttribute(ObjectID target)
    {
        this.target = target;
        modTarget = null;
    }

    /// <summary>
    /// Use this version to target modded entity
    /// </summary>
    /// <param name="modTarget">Modded entity ID</param>
    public EntityModificationAttribute(string modTarget)
    {
        this.modTarget = modTarget;
    }

    internal void ResolveTarget()
    {
        if (modTarget != null)
        {
            target = CustomEntityModule.GetItemIndex(modTarget);
        }
    }
}