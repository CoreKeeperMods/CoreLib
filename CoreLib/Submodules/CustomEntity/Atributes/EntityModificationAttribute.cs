using System;

namespace CoreLib.Submodules.CustomEntity.Atributes;

/// <summary>
/// Use this attribute to register your entity modification functions. Make sure to place one on the container class
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class EntityModificationAttribute : Attribute
{
    public ObjectID target = ObjectID.None;
    public string modTarget = "";

    /// <summary>
    /// Use this version to target ALL entities. You will need to performs checks yourself if this entity is a target.
    /// </summary>
    public EntityModificationAttribute() { }
    
    /// <summary>
    /// Use this version to target vanilla entity
    /// </summary>
    /// <param name="target">Vanilla entity ID</param>
    public EntityModificationAttribute(ObjectID target)
    {
        this.target = target;
        modTarget = "";
    }

    /// <summary>
    /// Use this version to target modded entity
    /// </summary>
    /// <param name="modTarget">Modded entity ID</param>
    public EntityModificationAttribute(string modTarget)
    {
        this.modTarget = modTarget;
    }
}