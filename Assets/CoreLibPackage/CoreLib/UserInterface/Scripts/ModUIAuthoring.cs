using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.UserInterface
{
    /// <summary>
    /// Represents an authoring component for integrating modded user interface elements within the system.
    /// </summary>
    public class ModUIAuthoring : MonoBehaviour
    {
        /// <summary>
        /// A unique identifier for a modded user interface component, used to register and retrieve
        /// the corresponding mod interface within the user interface system.
        /// </summary>
        /// <remarks>
        /// The <c>modInterfaceID</c> is a string identifier that allows for the integration, tracking,
        /// and management of custom modded UI components. It must be unique to avoid conflicts,
        /// as duplicate identifiers are not allowed and will be logged as warnings in the system.
        /// This identifier is utilized for dynamically instantiating and linking modded interfaces
        /// during initialization of the user interface, as well as for registering mod UIs through
        /// the <c>UserInterfaceModule</c>.
        /// </remarks>
        public string modInterfaceID;

        /// <summary>
        /// Specifies the initial local position of a modded user interface element within its parent transform.
        /// </summary>
        /// <remarks>
        /// The <c>initialInterfacePosition</c> defines the default starting position for the modded UI element
        /// relative to its parent container in 3D space. This vector is applied during the instantiation of the mod UI,
        /// ensuring that the interface appears at the intended position within the user interface layout.
        /// Properly setting this value is critical for seamless integration of modded interfaces within the system.
        /// </remarks>
        public Vector3 initialInterfacePosition = new Vector3(0,0,10);
    }
}