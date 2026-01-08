using UnityEngine;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.UserInterface.Interface
{
    /// <summary>
    /// Defines an interface for modded user interfaces within the application,
    /// providing methods and properties for displaying, hiding, and managing their visibility.
    /// </summary>
    public interface IModUI
    {
        /// <summary>
        /// Gets the root GameObject of the mod user interface.
        /// This object serves as the primary container for the UI elements.
        /// </summary>
        public GameObject Root { get; }

        /// <summary>
        /// Indicates whether the modded user interface should be displayed concurrently with
        /// the player's inventory UI. Used to determine if both interfaces should be shown together.
        /// </summary>
        public bool showWithPlayerInventory { get; }

        /// <summary>
        /// Determines whether the player's crafting UI should be displayed
        /// alongside the modded user interface when the player's inventory is open.
        /// </summary>
        public bool shouldPlayerCraftingShow { get; }

        /// <summary>
        /// Displays the mod user interface.
        /// The method is expected to enable the Root object associated with the mod UI.
        /// </summary>
        public void ShowUI();

        /// <summary>
        /// Hide mod UI <br/>
        /// The default implementation should disable Root object
        /// </summary>
        public void HideUI();

        /// <summary>
        /// Determines if the mod UI is currently visible.
        /// </summary>
        /// <returns>True if the mod UI is visible, otherwise false.</returns>
        [Preserve]
        public bool IsVisible()
        {
            return Root.activeInHierarchy;
        }
    }
}