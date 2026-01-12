using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.EquipmentSlot.Component
{
    /// Represents a Unity MonoBehaviour component that specifies the type of an object
    /// through a unique type identifier.
    /// <remarks>
    /// This component is primarily intended to be used with the object conversion system in Unity.
    /// The <c>objectTypeId</c> field can be set to define the specific type of an object.
    /// External systems or patches can query this component to retrieve or assign
    /// a corresponding <c>ObjectType</c> during runtime or conversion processes.
    /// </remarks>
    public class ModObjectTypeAuthoring : MonoBehaviour
    {
        /// Represents the unique identifier for an object type.
        /// <remarks>
        /// The value of this variable is utilized to determine and retrieve
        /// the appropriate <see cref="ObjectType"/> instance in the associated module or system.
        /// This identifier directly corresponds to the object type's name or key.
        /// </remarks>
        public string objectTypeId;
    }
}