using UnityEngine;
using UnityEngine.Scripting;

namespace CoreLib.UserInterface
{
    /// <summary>
    /// Common interface for modded user interfaces
    /// </summary>
    public interface IModUI
    {
        /// <summary>
        /// The root of the UI. Ensure this is backed by a serialized field
        /// </summary>
        public GameObject Root { get; }
        
        /// <summary>
        /// Should player inventory be shown together with this UI?
        /// </summary>
        public bool showWithPlayerInventory { get; }
        
        /// <summary>
        /// Should player crafting UI show?
        /// This option has effect only if your UI does not have inventory.
        /// </summary>
        public bool shouldPlayerCraftingShow { get; }
        
        /// <summary>
        /// Show mod UI <br/>
        /// The default implementation should enable Root object
        /// </summary>
        public void ShowUI();
        /// <summary>
        /// Hide mod UI <br/>
        /// The default implementation should disable Root object
        /// </summary>
        public void HideUI();

        /// <summary>
        /// Is mod UI visible.
        /// </summary>
        /// <returns></returns>
        [Preserve]
        public bool IsVisible()
        {
            return Root.activeInHierarchy;
        }
    }
}