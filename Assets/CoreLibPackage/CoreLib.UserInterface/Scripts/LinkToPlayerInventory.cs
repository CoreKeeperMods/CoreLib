using UnityEngine;

namespace CoreLib.UserInterface
{
    /// <summary>
    /// A MonoBehaviour component that facilitates a link between a user's interface element and the player's inventory UI.
    /// </summary>
    /// <remarks>
    /// This class can be used to associate a mod's user interface element with the player's inventory.
    /// It also optionally allows creating a reverse link to facilitate two-way interaction.
    /// </remarks>
    public class LinkToPlayerInventory : MonoBehaviour
    {
        /// <summary>
        /// Determines whether a reverse link should be established between the user's interface element and the player's inventory UI.
        /// </summary>
        /// <remarks>
        /// When set to true, this variable enables two-way interaction between the specified interface element and the player's inventory UI.
        /// The player's inventory UI will treat the linked interface as an additional top-level element for interaction purposes.
        /// </remarks>
        public bool createReverseLink = false;
    }
}