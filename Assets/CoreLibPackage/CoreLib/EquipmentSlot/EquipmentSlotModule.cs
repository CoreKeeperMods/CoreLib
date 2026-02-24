using System;
using System.Collections.Generic;
using CoreLib.Data;
using CoreLib.Submodule.EquipmentSlot.Interface;
using CoreLib.Submodule.EquipmentSlot.Patch;
using CoreLib.Submodule.EquipmentSlot.System;
using CoreLib.Util.Extension;
using JetBrains.Annotations;
using PlayerEquipment;
using PugMod;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;
using Logger = CoreLib.Util.Logger;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.EquipmentSlot
{
    /// Represents the equipment module within the CoreLib framework that manages equipment slots,
    /// object types, emotes, and associated logic for extending mod functionality.
    /// <remarks>
    /// This module provides functionality for registering, managing, and applying custom equipment slots
    /// and emotes. It includes metadata for versioning, prefab management, and internal dictionaries
    /// to handle registered slots and emotes.
    /// </remarks>
    public class EquipmentSlotModule : BaseSubmodule
    {
        
        public const string NAME = "Core Library - Equipment Slot";
        
        internal static Logger log = new(NAME);

        /// Gets the singleton instance of the <see cref="EquipmentSlotModule"/> submodule.
        /// This property retrieves an instance of <see cref="EquipmentSlotModule"/> through the CoreLibMod system.
        /// <remarks>
        /// The <c>Instance</c> property is utilized to access all functionality exposed by the <see cref="EquipmentSlotModule"/> class.
        /// It includes features such as registering equipment slots, managing slot types, and handling emotes.
        /// This property ensures that only one instance of <see cref="EquipmentSlotModule"/> exists and is shared across the application.
        /// </remarks>
        internal static EquipmentSlotModule Instance => CoreLibMod.GetModuleInstance<EquipmentSlotModule>();

        /// Represents the file path to an empty equipment slot prefab within the CoreLib framework.
        /// <remarks>
        /// The <c>EMPTY_PREFAB</c> constant provides the default path to a prefab asset used for representing an
        /// empty equipment slot in the system. This path is critical for handling UI or gameplay elements that
        /// require the visual representation of an unoccupied slot.
        /// </remarks>
        [UsedImplicitly] public static readonly string EMPTY_PREFAB = "Assets/CoreLibPackage/CoreLib/EquipmentSlot/Prefab/EmptySlot";

        /// Represents the default prefab path used for placement slots within the equipment module system.
        /// <remarks>
        /// The <c>PLACEMENT_PREFAB</c> constant defines the file location for the prefab associated with
        /// default placement slots. This path is leveraged by the module to load the appropriate prefab
        /// when managing or rendering placement slots.
        /// It is primarily used internally for asset loading and initialization processes.
        /// </remarks>
        [UsedImplicitly] public static readonly string PLACEMENT_PREFAB = "Assets/CoreLibPackage/CoreLib/EquipmentSlot/Prefab/DefaultPlaceSlot";

        /// Retrieves or generates an <see cref="ObjectType"/> corresponding to the specified type name.
        /// <param name="typeName">The name of the object type for which the <see cref="ObjectType"/> is to be retrieved or created.</param>
        /// <returns>The <see cref="ObjectType"/> corresponding to the provided type name.</returns>
        public static ObjectType GetObjectType(string typeName)
        {
            int index = objectTypeIDs.HasIndex(typeName) ? 
                objectTypeIDs.GetIndex(typeName) : 
                objectTypeIDs.GetNextId(typeName);
            return (ObjectType)index;
        }

        /// Retrieves the equipment slot type associated with a specified type of equipment slot.
        /// <typeparam name="T">The specific equipment slot type, which must implement <see cref="IModEquipmentSlot"/>.</typeparam>
        /// <returns>The equipment slot type corresponding to the provided generic type.</returns>
        public static EquipmentSlotType GetEquipmentSlotType<T>()
            where T : global::EquipmentSlot, IModEquipmentSlot
        {
            return GetEquipmentSlotType(typeof(T));
        }

        /// Retrieves the equipment slot type associated with the specified type.
        /// <param name="type">The type of the equipment slot for which the slot type is being retrieved.</param>
        /// <returns>The <see cref="EquipmentSlotType"/> corresponding to the given type.</returns>
        public static EquipmentSlotType GetEquipmentSlotType(Type type)
        {
            string typeName = type.FullName;

            int index = equipmentSlotTypeBind.HasIndex(typeName) ? equipmentSlotTypeBind.GetIndex(typeName) : equipmentSlotTypeBind.GetNextId(typeName);
            return (EquipmentSlotType)index;
        }

        /// Registers an equipment slot of the specified type, associating it with a prefab and logic handler.
        /// <typeparam name="T">The type of the equipment slot, which must implement <see cref="EquipmentSlot"/> and <see cref="IModEquipmentSlot"/>.</typeparam>
        /// <param name="objectType">The identifier for the object type associated with the equipment slot.</param>
        /// <param name="prefabPath">The file path to the prefab that represents the visual representation of the equipment slot.</param>
        /// <param name="logic">The logic handler implementing <see cref="IEquipmentLogic"/> to manage the equipment slot's behavior.</param>
        public static void RegisterEquipmentSlot<T>(string objectType, string prefabPath, IEquipmentLogic logic)
            where T : global::EquipmentSlot, IModEquipmentSlot
        {
            var prefab = LoadPrefab(prefabPath, typeof(T));
            var component = prefab.AddComponent<T>();

            if (component is PlaceObjectSlot placeObjectSlot)
            {
                placeObjectSlot.placementHandler = prefab.GetComponentInChildren<PlacementHandler>(true);
            }
            
            RegisterEquipmentSlot<T>(objectType, prefab, logic);
        }

        /// Registers a new equipment slot type with specified parameters, adding it to the internal management system.
        /// This method allows associating a prefab with logic and object type for equipment slot implementation.
        /// <typeparam name="T">The specific type of equipment slot to register, which must implement both EquipmentSlot and IModEquipmentSlot.</typeparam>
        /// <param name="objectType">The object type identifier used to categorize and reference the equipment slot.</param>
        /// <param name="prefab">The prefab GameObject associated with this equipment slot type.</param>
        /// <param name="logic">The logic associated with this equipment slot, implementing the IEquipmentLogic interface.</param>
        /// <exception cref="ArgumentException">Thrown if the prefab does not contain the specified equipment slot type or if the slot type is already registered.</exception>
        public static void RegisterEquipmentSlot<T>(string objectType, GameObject prefab, IEquipmentLogic logic)
            where T : global::EquipmentSlot, IModEquipmentSlot
        {
            global::EquipmentSlot slot = prefab.GetComponent<T>();

            if (slot == null)
            {
                throw new ArgumentException($"Failed to get Equipment slot main class! Please check your prefab.");
            }

            ObjectType objectTypeID = GetObjectType(objectType);
            var slotType = GetEquipmentSlotType(typeof(T));
            
            if (slots.ContainsKey(slotType))
            {
                throw new ArgumentException($"Equipment Slot with type {objectType} was already registered!");
            }
            
            
            slots.Add(slotType, new SlotInfo()
            {
                objectType = objectTypeID,
                slotType = typeof(T),
                slot = prefab,
                logic = logic
            });
        }

        /// Registers a new text-based emote with the specified emote identifier and returns the associated <see cref="Emote.EmoteType"/>.
        /// <param name="emoteId">The identifier for the emote to be registered.</param>
        /// <returns>The <see cref="Emote.EmoteType"/> representing the registered emote.</returns>
        public static Emote.EmoteType RegisterTextEmote(string emoteId)
        {
            Emote.EmoteType emoteType = (Emote.EmoteType)emoteTypeBind.GetNextId(emoteId);
            string emoteTerm = $"Emotes/MOD_{emoteId}";
            
            //LocalizationModule.AddTerm(emoteTerm, emoteTexts);
            textEmotes.Add(emoteType, emoteTerm);
            
            return emoteType;
        }

        /// Spawns a text-based emote at the specified position with optional properties to randomize placement and replace existing emotes.
        /// <param name="position">
        /// The position where the emote text should be spawned.
        /// </param>
        /// <param name="emoteType">
        /// The type of emote text to be spawned, specified by its EmoteType.
        /// </param>
        /// <param name="randomizePosition">
        /// Indicates whether the position of the emote text should be randomized. Defaults to true.
        /// </param>
        /// <param name="replace">
        /// Indicates whether existing emotes should be replaced. Defaults to true.
        /// </param>
        public static void SpawnModEmoteText(
            Vector3 position, 
            Emote.EmoteType emoteType,
            bool randomizePosition = true,
            bool replace = true)
        {
            if (replace && EmotePatch.lastEmotes.Count > 0)
            {
                foreach (Emote lastEmote in EmotePatch.lastEmotes)
                {
                    EmotePatch.FadeQuickly(lastEmote);
                }
                EmotePatch.lastEmotes.Clear();
            }
            
            var emote = Emote.SpawnEmoteText(position, emoteType, randomizePosition, false, false);
            EmotePatch.lastEmotes.Add(emote);
        }

        /// Represents the starting ID value for defining custom equipment slot types within the <see cref="EquipmentSlotModule"/>.
        /// <remarks>
        /// This constant defines the lower boundary for IDs that can be assigned to dynamically added equipment slot types by mods or extensions.
        /// Slot types with IDs less than this value are reserved for core system use and cannot be defined or overridden by external modules.
        /// </remarks>
        public const int MOD_SLOT_TYPE_ID_START = 128;

        /// Represents the ending value for the range of IDs used for modifiable equipment slot types in the system.
        /// <remarks>
        /// <c>ModSlotTypeIdEnd</c> defines the upper limit (inclusive) of the range allocated for IDs
        /// associated with custom-defined equipment slot types added via mods. This ensures that
        /// all ID assignments for mod equipment slots fall within a predefined, consistent range for proper validation and usage.
        /// </remarks>
        public const int MOD_SLOT_TYPE_ID_END = byte.MaxValue;

        /// Represents the initial value for custom emote type IDs defined in the modding framework.
        /// This constant marks the start of the ID range reserved for custom emote types
        /// within the <see cref="EquipmentSlotModule"/> system.
        /// <remarks>
        /// The <c>ModEmoteTypeIdStart</c> value is utilized as the lower bound for assigning unique IDs
        /// to custom emote types added through extensions or modifications to the core framework.
        /// It ensures consistency and separates mod-defined emotes from those defined internally.
        /// </remarks>
        public const int MOD_EMOTE_TYPE_ID_START = short.MaxValue;

        /// Represents the maximum identifier value for modded emote types within the system.
        /// <remarks>
        /// The <c>ModEmoteTypeIdEnd</c> constant is utilized to define the upper limit of the range for custom emote type identifiers.
        /// It ensures that all emote types registered by mods fall within a valid and defined range of IDs. This value is critical
        /// for managing modded emote registration and maintaining compatibility across different modules or plugins.
        /// </remarks>
        public const int MOD_EMOTE_TYPE_ID_END = int.MaxValue;

        /// Represents the starting value of the range reserved for mod-defined object type IDs.
        /// This constant marks the lower boundary of the ID range assigned to custom object types
        /// , ensuring they do not conflict with core object type IDs.
        /// <remarks>
        /// The <c>modObjectTypeIdRangeStart</c> value is used internally to allocate IDs for
        /// new object types introduced via mods. This helps maintain a clear boundary
        /// between core system object type IDs and those introduced by mod content.
        /// Mod developers should ensure their IDs fall within this designated range defined by
        /// <c>modObjectTypeIdRangeStart</c> and its corresponding end value.
        /// </remarks>
        public const int MOD_OBJECT_TYPE_ID_RANGE_START = 33000;

        /// Defines the exclusive upper-bound value for the range of mod object type IDs.
        /// This constant represents the maximum value that can be assigned to mod object type IDs
        /// to ensure compatibility and prevent overlap with other ranges.
        /// <remarks>
        /// The <c>modObjectTypeIdRangeEnd</c> constant is used in conjunction with
        /// <c>modObjectTypeIdRangeStart</c> to specify a valid range of object type IDs
        /// that can be used for customization or extension purposes within the module.
        /// This value is primarily used internally for ID management systems to bind and
        /// validate IDs defined for mod objects.
        /// </remarks>
        public const int MOD_OBJECT_TYPE_ID_RANGE_END = ushort.MaxValue;

        /// A dictionary that maps <see cref="EquipmentSlotType"/> to corresponding <see cref="SlotInfo"/> instances.
        /// <remarks>
        /// The <c>slots</c> dictionary is used throughout the <see cref="EquipmentSlotModule"/> and related systems to manage
        /// information about equipment slots and their associated logic. It provides a central repository to retrieve and modify
        /// slot-specific data, ensuring consistent handling of equipment placement, slot type determination, and related functionality.
        /// This internal field is accessed by various patches and systems to enable advanced equipment customization and behavior.
        /// </remarks>
        internal static Dictionary<EquipmentSlotType, SlotInfo> slots = new();

        /// A dictionary that maps emote types to their corresponding text representations.
        /// <remarks>
        /// The <c>textEmotes</c> dictionary is used to associate <see cref="Emote.EmoteType"/> values with their localized text definitions.
        /// It serves as the central storage for text-based emotes that can be dynamically registered with the system via the <see cref="EquipmentSlotModule.RegisterTextEmote"/> method.
        /// This is utilized in functionalities such as emote customization and display, allowing text to be linked to specific emote types.
        /// </remarks>
        internal static Dictionary<Emote.EmoteType, string> textEmotes = new();

        /// Represents the binding mechanism for associating types with unique slot type identifiers within a predefined range.
        /// <remarks>
        /// The <c>equipmentSlotTypeBind</c> variable provides an instance of the <see cref="IdBind"/> class for managing
        /// the mapping of equipment slot types to unique identifiers. It ensures that identifiers fall within the range
        /// specified by <see cref="MOD_SLOT_TYPE_ID_START"/> and <see cref="MOD_SLOT_TYPE_ID_END"/>.
        /// This binding is utilized to dynamically assign and retrieve slot type identifiers based on type information
        /// in the <see cref="EquipmentSlotModule"/> module.
        /// </remarks>
        internal static IdBind equipmentSlotTypeBind = new(MOD_SLOT_TYPE_ID_START, MOD_SLOT_TYPE_ID_END);

        /// Represents an instance of the <see cref="IdBind"/> class responsible for managing and binding unique identifiers
        /// for custom emote types within the application.
        /// <remarks>
        /// The <c>emoteTypeBind</c> field is used to allocate and track IDs for dynamically added emote types defined
        /// by modification frameworks. It is preconfigured with a specific range of allowable ID values
        /// to ensure no conflicts with existing identifiers in the system.
        /// </remarks>
        internal static IdBind emoteTypeBind = new(MOD_EMOTE_TYPE_ID_START, MOD_EMOTE_TYPE_ID_END);

        /// Represents a binding mechanism managing unique identifiers for object types within a specific range.
        /// <remarks>
        /// The <c>objectTypeIDs</c> field is used to assign and retrieve unique IDs for various object types within the application's defined range.
        /// It ensures that each object type is mapped to a unique identifier, facilitating consistent type management and lookup operations.
        /// This field is a core component for handling object type bindings in the <see cref="EquipmentSlotModule"/> system.
        /// </remarks>
        internal static IdBind objectTypeIDs = new(MOD_OBJECT_TYPE_ID_RANGE_START, MOD_OBJECT_TYPE_ID_RANGE_END);

        /// Configures and applies necessary patches or hooks for the functionality provided by the module.
        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(EmotePatch));
            CoreLibMod.Patch(typeof(PlayerControllerPatch));
            CoreLibMod.Patch(typeof(ObjectAuthoringConverterPatch));
            CoreLibMod.Patch(typeof(PlacementHandlerPatch));
        }

        /// Loads and initializes the Equipment Module by setting up necessary dependencies and event handlers.
        /// <remarks>
        /// This method is responsible for refreshing the module bundles through the ResourcesModule and
        /// subscribing to relevant events for both client and server worlds during the initialization process.
        /// It ensures the Equipment Module is prepared and functional for its intended operation within the system.
        /// </remarks>
        internal override void Load()
        {
            base.Load();
            API.Client.OnWorldCreated += ClientWorldReady;
            API.Server.OnWorldCreated += ServerWorldReady;
        }

        /// Loads a prefab from the specified path and initializes it as a new instance configured for the given slot type.
        /// <param name="prefabPath">The path of the prefab to load, typically from the resource module.</param>
        /// <param name="slotType">The type of the equipment slot for which the prefab is being loaded.</param>
        /// <returns>A new <see cref="GameObject"/> instance representing the loaded prefab, configured for the specified slot type.</returns>
        internal static GameObject LoadPrefab(string prefabPath, Type slotType)
        {
            GameObject prefab = Mod.LoadAsset<GameObject>(prefabPath);

            GameObject newPrefab = Object.Instantiate(prefab);
            newPrefab.hideFlags = HideFlags.HideAndDontSave;
            newPrefab.name = $"{slotType.GetNameChecked()}_Prefab";

            return newPrefab;
        }

        /// Initializes client-side equipment update systems when the game world is created on the client.
        private static void ClientWorldReady()
        {
            CreateEquipmentUpdateSystems(API.Client.World);
        }

        /// This method is invoked when the server's world has been created and is ready for use.
        /// It is responsible for initializing and creating equipment update systems within the server world.
        private static void ServerWorldReady()
        {
            CreateEquipmentUpdateSystems(API.Server.World);
        }

        /// Creates and initializes the necessary systems for updating equipment within a specified World instance.
        /// <param name="world">The <see cref="World"/> instance where the equipment update systems will be added and managed.</param>
        private static void CreateEquipmentUpdateSystems(World world)
        {
            //TODO rework
            /*var updateSystem = world.GetOrCreateSystemManaged<ModEquipmentSystem>();
            var equipmentGroup = world.GetExistingSystemManaged<EquipmentUpdateSystemGroup>();
            equipmentGroup.AddSystemToUpdateList(updateSystem);
            
            var changeSystem = world.GetOrCreateSystemManaged<ModEquipmentChangeSystem>();
            var equipmentBeforeGroup = world.GetExistingSystemManaged<EquipmentBeforeUpdateSystemGroup>();
            equipmentBeforeGroup.AddSystemToUpdateList(changeSystem);*/
        }
    }
}