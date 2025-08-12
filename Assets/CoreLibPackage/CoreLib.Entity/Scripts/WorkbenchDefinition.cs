using System.Collections.Generic;
using Pug.Sprite;
using Pug.UnityExtensions;
using UnityEngine;
using UnityEngine.Serialization;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity
{
    /// <summary>
    /// Represents a definition for a workbench in the system. This class is used to define
    /// essential properties and icons for a workbench that can be utilized in CoreLib modules.
    /// </summary>
    [CreateAssetMenu(fileName = "WorkbenchDefinition", menuName = "CoreLib/New WorkbenchDefinition", order = 2)]
    public class WorkbenchDefinition : ScriptableObject
    {
        /// <summary>
        /// Contains the unique identifier for the workbench item within the system.
        /// This identifier is used to distinguish specific workbench definitions
        /// among various modules or modifications.
        /// <remarks>
        /// The identifier format typically follows the structure `<ModID>:<ItemID>`.
        /// It plays a critical role in associating related workbenches, crafting processes,
        /// and ensuring correct integration across module boundaries.
        /// </remarks>
        /// </summary>
        [Tooltip("Format: <ModID>:<ItemID>\nExample: CoreLib:RootModWorkbench")]
        public string itemId;

        /// <summary>
        /// Represents the primary large graphical icon used for defining the visual identity
        /// of a workbench within the system, typically applicable in UI elements or inventories.
        /// </summary>
        /// <remarks>
        /// This sprite is expected to be a 16x16 PNG image, ensuring proper resolution and
        /// aesthetic consistency. It is especially significant in differentiating workbench types
        /// and maintaining a cohesive visual language throughout the application.
        /// </remarks>
        [FormerlySerializedAs("icon")][Tooltip("Large Icon: 16x16 PNG")]
        public Sprite bigIcon;

        /// <summary>
        /// Represents the small icon used for visual representation of the Workbench.
        /// This smaller-sized sprite is particularly suitable for compact UI elements or lists where space is limited.
        /// <remarks>
        /// The recommended size for this sprite is 10x10, though it should be compatible with icons up to 16x16 PNG.
        /// This ensures visual clarity and consistency, especially in dense layouts or minimalistic UI components.
        /// </remarks>
        /// </summary>
        [Tooltip("Small Icon: 16x16 PNG\nRecommended Sprite Size: 10x10")]
        public Sprite smallIcon;
        // Removed Variations as they are no longer used.
        /// <summary>
        /// Defines the skin or visual style for the graphical assets associated with a Workbench.
        /// This variable specifies a sprite-based skin used to customize the appearance of a Workbench.
        /// <remarks>
        /// The asset skin is primarily used for defining the visual theme of the Workbench, ensuring
        /// that it aligns with the intended aesthetic of the associated items and functionality.
        /// This can include a reference to specific sprite assets related to the Workbench design.
        /// </remarks>
        /// </summary>
        [Tooltip("Sprite Asset Skin: Target Asset should be Workbench.asset in CoreLib")]
        public SpriteAssetSkin assetSkin;

        /// <summary>
        /// Determines whether the Workbench should be bound to the Root Workbench in the CoreLib system.
        /// When set to true, the current Workbench becomes associated with the Root Workbench, enabling
        /// shared functionality, such as crafting capabilities or inventory synchronization.
        /// <remarks>
        /// Binding a Workbench to the Root Workbench ensures compatibility and integration within
        /// the CoreLib framework, allowing for centralized management and modular extensibility.
        /// </remarks>
        /// </summary>
        [Tooltip("Bind this Workbench to the Root Workbench in CoreLib")]
        public bool bindToRootWorkbench;

        /// <summary>
        /// Represents the recipe or list of required items to craft this Workbench.
        /// This variable defines the crafting prerequisites for creating the Workbench,
        /// detailing each component needed for its construction.
        /// <remarks>
        /// The recipe is used during the crafting process to ensure all required components
        /// are provided before the Workbench can be created successfully.
        /// </remarks>
        /// </summary>
        [FormerlySerializedAs("requiredObjectsToCraft")][Tooltip("The required items to create this Workbench")]
        public List<InventoryItemAuthoring.CraftingObject> recipe;

        /// <summary>
        /// Specifies the list of items that this Workbench is capable of crafting.
        /// This variable determines which crafting options are available to the user.
        /// </summary>
        /// <remarks>
        /// The items listed in this variable are crafted using this specific Workbench.
        /// It is a key part of defining the functionality and purpose of the Workbench.
        /// </remarks>
        [Tooltip("The items this Workbench can craft")]
        public List<InventoryItemAuthoring.CraftingObject> canCraft;
        [PickStringFromEnum(typeof(ObjectID))] [Tooltip("The Workbenches that this Workbench can switch to.\nThe items of that Workbench will be added to this one.")]
        public List<string> relatedWorkbenches;

        /// <summary>
        /// Represents the main window title of the Workbench interface.
        /// This variable sets the central title displayed prominently on the Workbench UI.
        /// <remarks>
        /// The title serves as a descriptive label for the Workbench and helps the user identify its purpose or type.
        /// </remarks>
        /// </summary>
        [Tooltip("The center Window Title of the Workbench")]
        public string title;
        [Tooltip("The left Window Title of the Workbench")]
        public string leftTitle;

        /// <summary>
        /// Represents the right window title of the Workbench.
        /// This string defines the label or heading displayed in the right section of the Workbench's UI window.
        /// <remarks>
        /// The right title helps to inform users about specific functionality or content available in the right pane of the UI.
        /// It provides contextual information to enhance user interaction with the Workbench.
        /// </remarks>
        /// </summary>
        [Tooltip("The right Window Title of the Workbench")]
        public string rightTitle;

        /// <summary>
        /// Represents the skin or visual theme of the windows of the Workbench.
        /// This variable determines the appearance of the crafting UI, including its colors and design.
        /// <remarks>
        /// The skin is applied to enhance the user interface of the Workbench, ensuring consistency in the visual style.
        /// </remarks>
        /// </summary>
        [Tooltip("The skin of the Windows of the Workbench")]
        public UIManager.CraftingUIThemeType skin;
    }
}