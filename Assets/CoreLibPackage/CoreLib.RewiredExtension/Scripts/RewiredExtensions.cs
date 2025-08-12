using System.Collections.Generic;
using CoreLib.RewiredExtension;
using Rewired;
using Rewired.Data.Mapping;

namespace CoreLib.Util.Extensions
{
    /// <summary>
    /// Provides extension methods for working with the Rewired library.
    /// </summary>
    public static class RewiredExtensions
    {
        /// Adds an action to the specified category within an ActionCategoryMap.
        /// If the action is not already present in the category, it will be added.
        /// <param name="map">The ActionCategoryMap instance to which the action will be added.</param>
        /// <param name="categoryId">The ID of the category to which the action belongs.</param>
        /// <param name="actionId">The ID of the action to add to the category.</param>
        /// <returns>True if the action was successfully added or already exists in the category; otherwise, false.</returns>
        public static bool AddAction(this ActionCategoryMap map, int categoryId, int actionId)
        {
            var list = map.GetValue<List<ActionCategoryMap.Entry>>("list");
            if (list == null)
            {
                return false;
            }
            int num = map.IndexOfCategory(categoryId);
            if (num < 0)
            {
                return false;
            }
            if (!list[num].actionIds.Contains(actionId))
            {
                list[num].actionIds.Add(actionId);
            }
            return true;
        }

        /// <summary>
        /// Checks if the specified action is currently being held down by the player.
        /// </summary>
        /// <param name="player">The Rewired Player instance.</param>
        /// <param name="actionName">The name of the action to query.</param>
        /// <returns>True if the action is being held down, otherwise false.</returns>
        public static bool GetButton(this Player player, string actionName)
        {
            int actionId = RewiredExtensionModule.GetKeybindId(actionName);
            return player.GetButton(actionId);
        }

        /// <summary>
        /// Checks if the button mapped to a specified action name was pressed down during the current frame.
        /// </summary>
        /// <param name="player">The Rewired Player object to query input from.</param>
        /// <param name="actionName">The name of the action to check the button down state for.</param>
        /// <returns>
        /// True if the button associated with the given action name was pressed down during the current frame; otherwise, false.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the specified action name does not correspond to a valid key bind.
        /// </exception>
        public static bool GetButtonDown(this Player player, string actionName)
        {
            int actionId = RewiredExtensionModule.GetKeybindId(actionName);
            return player.GetButtonDown(actionId);
        }

        /// <summary>
        /// Determines whether the button associated with the specified action name was released during the frame.
        /// </summary>
        /// <param name="player">The Player object to check the button state for.</param>
        /// <param name="actionName">The name of the action mapped to the button being checked.</param>
        /// <returns>
        /// A boolean value indicating whether the button associated with the specified action name was released during the frame.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the specified action name is not registered as a valid key bind.
        /// </exception>
        public static bool GetButtonUp(this Player player, string actionName)
        {
            int actionId = RewiredExtensionModule.GetKeybindId(actionName);
            return player.GetButtonUp(actionId);
        }
    }
}