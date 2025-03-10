using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.UserInterface.Patches;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Scripting;

namespace CoreLib.UserInterface
{
    /// <summary>
    /// This module provides tools for creating and working with custom UI's
    /// </summary>
    public class UserInterfaceModule : BaseSubmodule
    {
        #region Public Interface

        /// <summary>
        /// Get current Entity the user has interacted with
        /// </summary>
        [Preserve]
        public static Entity GetInteractionEntity()
        {
            Instance.ThrowIfNotLoaded();
            return currentInteractionEntity;
        }

        /// <summary>
        /// Get current EntityMonoBehaviour the user has interacted with
        /// </summary>
        /// <returns>EntityMonoBehaviour or null</returns>
        [Preserve]
        public static EntityMonoBehaviour GetInteractionMonoBehaviour()
        {
            Instance.ThrowIfNotLoaded();
            return currentInteractionMonoBehaviour;
        }

        /// <summary>
        /// Get currently opened modded user interface
        /// </summary>
        [Preserve]
        public static T GetCurrentInterface<T>()
            where T : class, IModUI
        {
            Instance.ThrowIfNotLoaded();
            return currentInterface as T;
        }

        /// <summary>
        /// Get instance of the modded user interface with specified identifier
        /// </summary>
        /// <param name="interfaceID">User interface identifier</param>
        /// <typeparam name="T">Type of the interface</typeparam>
        [Preserve]
        public static T GetModInterface<T>(string interfaceID)
            where T : class, IModUI
        {
            Instance.ThrowIfNotLoaded();
            if (!modInterfaces.ContainsKey(interfaceID))
            {
                CoreLibMod.Log.LogError($"Trying to get UI '{interfaceID}', which is not registered!");
                return null;
            }

            IModUI modUI = modInterfaces[interfaceID];
            return modUI as T;
        }

        
        /// <summary>
        /// Open modded user interface with specified identifier
        /// </summary>
        /// <param name="interfaceID">User interface identifier</param>
        [Preserve]
        public static void OpenModUI(string interfaceID)
        {
            OpenModUI(null, interfaceID);
        }

        /// <summary>
        /// Open modded user interface with specified identifier, while specifying current entity <br/>
        /// If you want to rely on <see cref="GetInteractionEntity"/> you must use this variant
        /// </summary>
        /// <param name="openEntity">Current Entity</param>
        /// <param name="interfaceID">User interface identifier</param>
        [Preserve]
        public static void OpenModUI(Entity openEntity, string interfaceID)
        {
            OpenModUI(null, openEntity, interfaceID);
        }

        /// <summary>
        /// Open modded user interface with specified identifier, while specifying current EntityMonoBehaviour <br/>
        /// If you want to rely on <see cref="GetInteractionEntity"/> or <see cref="GetInteractionMonoBehaviour"/> you must use this variant
        /// </summary>
        /// <param name="openBehaviour">Current EntityMonoBehaviour</param>
        /// <param name="interfaceID">User interface identifier</param>
        [Preserve]
        public static void OpenModUI(EntityMonoBehaviour openBehaviour, string interfaceID)
        {
            var entity = openBehaviour != null ? openBehaviour.entity : Entity.Null;
            OpenModUI(openBehaviour, entity, interfaceID);
        }

        /// <summary>
        /// Register mod user interface by calling this method on a prefab of the user interface. <br/>
        /// The intended use of this function is within <see cref="PugMod.IMod.ModObjectLoaded"/> like so:
        /// <code>
        /// public void ModObjectLoaded(Object obj)
        /// {
        ///     if (obj is not GameObject gameObject) return;
        ///
        ///     UserInterfaceModule.RegisterModUI(gameObject);
        /// }
        /// </code>
        /// </summary>
        /// <param name="go">Prefab game object</param>
        public static void RegisterModUI(GameObject go)
        {
            Instance.ThrowIfNotLoaded();
            var modInterfaceAuthoring = go.GetComponent<ModUIAuthoring>();
            if (modInterfaceAuthoring == null) return;

            if (interfacePrefabs.Any(authoring =>
                    authoring.modInterfaceID.Equals(modInterfaceAuthoring.modInterfaceID, StringComparison.InvariantCultureIgnoreCase)))
            {
                CoreLibMod.Log.LogWarning($"Tried to register mod UI with id '{modInterfaceAuthoring.modInterfaceID}', which already was registered!");
                return;
            }

            interfacePrefabs.Add(modInterfaceAuthoring);
            CoreLibMod.Log.LogInfo($"Registering {modInterfaceAuthoring.modInterfaceID} Modded UI!");
        }

        #endregion

        #region Private Implementation

        private static EntityMonoBehaviour currentInteractionMonoBehaviour;
        private static Entity currentInteractionEntity;
        internal static IModUI currentInterface;

        internal static List<ModUIAuthoring> interfacePrefabs = new List<ModUIAuthoring>();
        internal static Dictionary<string, IModUI> modInterfaces = new Dictionary<string, IModUI>();

        internal override GameVersion Build => new GameVersion(1, 1, 0, "90bc");
        internal override string Version => "0.1.2";
        internal static UserInterfaceModule Instance => CoreLibMod.GetModuleInstance<UserInterfaceModule>();

        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(UIManager_Patch));
        }

        internal static void ClearModUIData()
        {
            currentInteractionMonoBehaviour = null;
            currentInteractionEntity = Entity.Null;
            currentInterface = null;
        }

        private static void OpenModUI(EntityMonoBehaviour openBehaviour, Entity openEntity, string interfaceID)
        {
            Instance.ThrowIfNotLoaded();
            var modUI = GetModInterface<IModUI>(interfaceID);
            if (modUI == null) return;

            currentInteractionMonoBehaviour = openBehaviour;
            currentInteractionEntity = openEntity;

            modUI.ShowUI();
            currentInterface = modUI;
            if (modUI.showWithPlayerInventory)
            {
                if (modUI.shouldPlayerCraftingShow)
                    Manager.ui.OnPlayerInventoryOpen();
                else
                    PlayerInventoryOpenNoCrafting(Manager.ui);
            }
        }

        private static void PlayerInventoryOpenNoCrafting(UIManager uiManager)
        {
            uiManager.inventoryButton.HideLightUpHint();
            uiManager.playerInventoryUI.ShowContainerUI();
            uiManager.trashCanUI.ShowContainerUI();
            if (uiManager.mapUI.isShowingBigMap)
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