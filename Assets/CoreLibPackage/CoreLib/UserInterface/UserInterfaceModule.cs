using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Submodule.UserInterface.Component;
using CoreLib.Submodule.UserInterface.Interface;
using CoreLib.Submodule.UserInterface.Patch;
using UnityEngine;
using UnityEngine.Scripting;
using Logger = CoreLib.Util.Logger;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.UserInterface
{
    /// Provides functionality for managing custom user interfaces and interacting with mod-specific UI components.
    /// Offers methods to retrieve, register, and open specific UI interfaces in a modularized system.
    public class UserInterfaceModule : BaseSubmodule
    {
        #region Fields

        public const string NAME = "Core Library - UserInterface";
        
        public static Logger log = new(NAME);

        #endregion
        
        #region Public Interface

        /// Retrieves the current entity that is being interacted with by the user.
        /// <returns>
        /// The entity instance representing the current user interaction.
        /// </returns>
        [Preserve]
        public static Unity.Entities.Entity GetInteractionEntity()
        {
            Instance.ThrowIfNotLoaded();
            return _currentInteractionEntity;
        }

        /// Get the MonoBehaviour instance associated with the current interaction entity.
        /// <returns>An instance of EntityMonoBehaviour representing the current interaction, or null if no interaction is active.</returns>
        [Preserve]
        public static EntityMonoBehaviour GetInteractionMonoBehaviour()
        {
            Instance.ThrowIfNotLoaded();
            return _currentInteractionMonoBehaviour;
        }

        /// Retrieves the currently opened user interface of the specified type.
        /// <typeparam name="T">The type of user interface to retrieve, which must implement the IModUI interface.</typeparam>
        /// <returns>The currently opened user interface of type T if available; otherwise, null.</returns>
        [Preserve]
        public static T GetCurrentInterface<T>()
            where T : class, IModUI
        {
            Instance.ThrowIfNotLoaded();
            return currentInterface as T;
        }

        /// Retrieves an instance of the modded user interface with the specified identifier.
        /// <typeparam name="T">The type of the interface to retrieve.</typeparam>
        /// <param name="interfaceID">The unique identifier of the user interface.</param>
        /// <returns>An instance of the modded user interface as the specified type, or null if the interface is not registered or cannot be cast to the specified type.</returns>
        [Preserve]
        public static T GetModInterface<T>(string interfaceID)
            where T : class, IModUI
        {
            Instance.ThrowIfNotLoaded();
            if (!modInterfaces.ContainsKey(interfaceID))
            {
                log.LogError($"Trying to get UI '{interfaceID}', which is not registered!");
                return null;
            }

            IModUI modUI = modInterfaces[interfaceID];
            return modUI as T;
        }


        /// Opens the modded user interface associated with the specified identifier.
        /// <param name="interfaceID">The identifier of the user interface to open.</param>
        [Preserve]
        public static void OpenModUI(string interfaceID)
        {
            OpenModUI(null, interfaceID);
        }

        /// Opens a modded user interface with the specified identifier while associating it with the provided entity.
        /// <param name="openEntity">The entity associated with the user interface being opened.</param>
        /// <param name="interfaceID">The unique identifier of the user interface to open.</param>
        [Preserve]
        public static void OpenModUI(Unity.Entities.Entity openEntity, string interfaceID)
        {
            OpenModUI(null, openEntity, interfaceID);
        }

        /// Opens the modded user interface with the specified identifier.
        /// <param name="interfaceID">The identifier of the user interface to open.</param>
        [Preserve]
        public static void OpenModUI(EntityMonoBehaviour openBehaviour, string interfaceID)
        {
            var entity = openBehaviour != null ? openBehaviour.entity : Unity.Entities.Entity.Null;
            OpenModUI(openBehaviour, entity, interfaceID);
        }

        /// Registers a mod's user interface by attaching the provided GameObject,
        /// ensuring it is not already registered.
        /// <param name="go">The prefab GameObject representing the mod user interface to be registered.</param>
        public static void RegisterModUI(GameObject go)
        {
            Instance.ThrowIfNotLoaded();
            var modInterfaceAuthoring = go.GetComponent<ModUIAuthoring>();
            if (modInterfaceAuthoring == null) return;

            if (interfacePrefabs.Any(authoring =>
                    authoring.modInterfaceID.Equals(modInterfaceAuthoring.modInterfaceID,
                        StringComparison.InvariantCultureIgnoreCase)))
            {
                log.LogWarning($"Tried to register mod UI with id '{modInterfaceAuthoring.modInterfaceID}', which already was registered!");
                return;
            }

            interfacePrefabs.Add(modInterfaceAuthoring);
            log.LogInfo($"Registering {modInterfaceAuthoring.modInterfaceID} Modded UI!");
        }

        #endregion

        #region Private Implementation

        /// Holds a reference to the current <see cref="EntityMonoBehaviour"/> being interacted with in the user interface module.
        /// This variable is used to manage and track interactions for modded UI elements, ensuring proper context for UI operations.
        private static EntityMonoBehaviour _currentInteractionMonoBehaviour;

        /// Represents the entity currently involved in interaction logic within the <see cref="UserInterfaceModule"/>.
        /// This static field is utilized to track the active entity associated with the interactive user interface system.
        private static Unity.Entities.Entity _currentInteractionEntity;

        /// Represents the currently active user interface in the <see cref="UserInterfaceModule"/>.
        /// This variable holds the active instance of an object implementing the <see cref="IModUI"/> interface.
        /// It provides access to the currently displayed modded UI, enabling interaction or management within the module.
        internal static IModUI currentInterface;

        /// Represents a collection of prefabs for modded user interfaces.
        /// These prefabs are used for registering and initializing custom modded UIs
        /// within the <see cref="UserInterfaceModule"/>.
        internal static List<ModUIAuthoring> interfacePrefabs = new();

        /// Represents a collection of registered mod interfaces, providing a mapping between
        /// unique string identifiers and their corresponding <see cref="IModUI"/> implementations.
        /// This dictionary enables efficient retrieval and management of modded user interface components
        /// within the <see cref="UserInterfaceModule"/>.
        internal static Dictionary<string, IModUI> modInterfaces = new();

        /// Provides access to the singleton instance of the <see cref="UserInterfaceModule"/>.
        /// This property allows interaction with various functionalities related to custom modular user interfaces
        /// such as opening, managing, and registering modded UIs.
        internal static UserInterfaceModule Instance => CoreLibMod.GetModuleInstance<UserInterfaceModule>();

        /// Configures and applies necessary hooks for the user interface module by patching relevant components.
        /// <remarks>
        /// This method is responsible for integrating the module-specific hook operations
        /// into the system by applying patches to targeted components or features.
        /// </remarks>
        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(UIManagerPatch));
        }

        /// Clears all stored data related to currently active mod user interfaces,
        /// including interaction entities, MonoBehaviours, and the active interface reference.
        internal static void ClearModUIData()
        {
            _currentInteractionMonoBehaviour = null;
            _currentInteractionEntity = Unity.Entities.Entity.Null;
            currentInterface = null;
        }

        /// Opens a modular user interface associated with the provided interface ID and entity.
        /// <param name="openEntity">The entity for which the user interface should be opened.</param>
        /// <param name="interfaceID">The identifier of the interface to be opened.</param>
        private static void OpenModUI(EntityMonoBehaviour openBehaviour, Unity.Entities.Entity openEntity, string interfaceID)
        {
            Instance.ThrowIfNotLoaded();
            var modUI = GetModInterface<IModUI>(interfaceID);
            if (modUI == null) return;

            _currentInteractionMonoBehaviour = openBehaviour;
            _currentInteractionEntity = openEntity;

            modUI.ShowUI();
            currentInterface = modUI;
            if (modUI.ShowWithPlayerInventory)
            {
                if (modUI.ShouldPlayerCraftingShow)
                    Manager.ui.OnPlayerInventoryOpen();
                else
                    PlayerInventoryOpenNoCrafting(Manager.ui);
            }
        }

        /// Opens the player's inventory UI without enabling crafting-related elements.
        /// <param name="uiManager">The UIManager instance responsible for handling UI components.</param>
        private static void PlayerInventoryOpenNoCrafting(UIManager uiManager)
        {
            uiManager.inventoryButton.HideLightUpHint();
            uiManager.playerInventoryUI.ShowContainerUI();
            uiManager.trashCanUI.ShowContainerUI();
            if (uiManager.mapUI.IsShowingBigMap)
            {
                uiManager.OnMapToggle();
            }

            PlayerController player = Manager.main.player;

            uiManager.creativeModeUI.HideContainerUI();
            uiManager.creativeModeOptionsUI.Hide();

            uiManager.characterWindow.Hide();
            AudioManager.SfxUI(SfxID.chestopen, 1f, true, 0.5f, 0.15f);

            if (Manager.ui.currentSelectedUIElement != null && 
                !Manager.ui.currentSelectedUIElement.gameObject.activeInHierarchy)
            {
                Manager.ui.DeselectAnySelectedUIElement();
                uiManager.mouse.UpdateMouseUIInput(out bool _, out bool _);
            }
        }

        #endregion
    }
}