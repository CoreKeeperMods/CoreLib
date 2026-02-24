using CoreLib.Submodule.UserInterface.Component;
using CoreLib.Submodule.UserInterface.Interface;
using HarmonyLib;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.UserInterface.Patch
{
    /// Provides patches through Harmony to modify and extend the behavior of the UIManager class within the user interface system.
    /// This class includes methods for initializing mod-specific user interfaces, handling their visibility,
    /// and ensuring integration with the core inventory and crafting UI systems.
    public class UIManagerPatch
    {
        /// Initializes and sets up mod-specific interfaces within the user interface system.
        /// This method ensures that the interfaces associated with user-defined modules are instantiated
        /// and linked to the player's inventory UI for seamless interaction between custom components and core systems.
        /// <param name="__instance">The instance of the UIManager being initialized, providing access to core UI elements such as player's inventory UI.</param>
        [HarmonyPatch(typeof(UIManager), nameof(UIManager.Init))]
        [HarmonyPostfix]
        public static void OnInit(UIManager __instance)
        {
            var uiTransform = __instance.chestInventoryUI.transform.parent;
            var inventoryUI = __instance.playerInventoryUI;

            foreach (ModUIAuthoring interfacePrefab in UserInterfaceModule.interfacePrefabs)
            {
                var interfaceGameObject = Object.Instantiate(interfacePrefab.gameObject, uiTransform);
                var interfaceComponent = interfaceGameObject.GetComponent<IModUI>();
                interfaceGameObject.transform.localPosition = interfacePrefab.initialInterfacePosition;
                UserInterfaceModule.modInterfaces.Add(interfacePrefab.modInterfaceID, interfaceComponent);

                var myLink = interfaceGameObject.GetComponent<LinkToPlayerInventory>();
                MakeLink(myLink, inventoryUI);
                
                var links = interfaceGameObject.GetComponentsInChildren<LinkToPlayerInventory>(true);
                foreach (var link in links)
                {
                    MakeLink(link, inventoryUI);
                }
            }
        }

        /// Establishes a link between a UI element and the player's inventory UI, enabling interaction between them.
        /// Adds the player's inventory UI to the bottom UI elements of the specified link and optionally creates a reverse link.
        /// <param name="link">The UI element that should link to the player's inventory UI.</param>
        /// <param name="inventoryUI">The player's inventory UI to be linked with the specified UI element.</param>
        private static void MakeLink(LinkToPlayerInventory link, ItemSlotsUIContainer inventoryUI)
        {
            if (link == null) return;
            
            var uiElement = link.gameObject.GetComponent<UIelement>();
            if (uiElement == null) return;

            uiElement.bottomUIElements.Add(inventoryUI);
            if (link.createReverseLink)
            {
                inventoryUI.topUIElements.Add(uiElement);
            }
        }

        /// Postfix method for hiding all modded user interfaces when the inventory and crafting UI is hidden.
        /// This method iterates through all registered mod UI components and hides them, ensuring a consistent behavior
        /// with the default UI. Additionally, it clears the user interface module's mod UI data, resetting its state.
        [HarmonyPatch(typeof(UIManager), nameof(UIManager.HideAllInventoryAndCraftingUI))]
        [HarmonyPostfix]
        public static void OnHide()
        {
            foreach (IModUI modUI in UserInterfaceModule.modInterfaces.Values)
            {
                modUI.HideUI();
            }

            UserInterfaceModule.ClearModUIData();
        }

        /// Postfix method for checking if any menu is active in the UI.
        /// This method modifies the result of the original property to include the state of the custom user interface module.
        /// <param name="__result">The original value indicating whether any default inventory or crafting UI is active. This will be modified if a custom menu is active via <see cref="UserInterfaceModule"/>.</param>
        [HarmonyPatch(typeof(UIManager), nameof(UIManager.isAnyInventoryShowing), MethodType.Getter)]
        [HarmonyPostfix]
        public static void OnIsAnyMenuActive(ref bool __result)
        {
            if (__result) return;

            __result |= UserInterfaceModule.currentInterface != null;
        }
    }
}