using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Data;
using CoreLib.Submodule.EquipmentSlot.Component;
using CoreLib.Submodule.EquipmentSlot.Interface;
using CoreLib.Submodule.EquipmentSlot.Patch;
using CoreLib.Submodule.EquipmentSlot.System;
using CoreLib.Util;
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
    public class EquipmentSlotModule : BaseSubmodule
    {
        
        public const string NAME = "Core Library - Equipment Slot";
        
        internal static Logger log = new(NAME);

        /// Gets the singleton instance of the <see cref="EquipmentSlotModule"/> submodule.
        internal static EquipmentSlotModule Instance => CoreLibMod.GetModuleInstance<EquipmentSlotModule>();

        /// Default prefab for empty prefab
        [UsedImplicitly] 
        public static readonly string EMPTY_PREFAB = "EmptySlot";

        /// Default prefab for placement related slots
        [UsedImplicitly] 
        public static readonly string PLACEMENT_PREFAB = "DefaultPlaceSlot";

        /// Retrieves or generates an <see cref="ObjectType"/> corresponding to the specified type name.
        /// <param name="typeName">Object type identifier</param>
        public static ObjectType GetObjectType(string typeName)
        {
            int index = objectTypeIDs.HasIndex(typeName) ? 
                objectTypeIDs.GetIndex(typeName) : 
                objectTypeIDs.GetNextId(typeName);
            return (ObjectType)index;
        }

        /// Retrieves EquipmentSlotType associated with a specified type of equipment slot.
        /// <typeparam name="T">Equipment slot type, which must implement <see cref="IModEquipmentSlot"/>.</typeparam>
        public static EquipmentSlotType GetEquipmentSlotType<T>()
            where T : global::EquipmentSlot, IModEquipmentSlot
        {
            return GetEquipmentSlotType(typeof(T));
        }

        /// Retrieves EquipmentSlotType associated with the specified type.
        /// <param name="type">Equipment slot type</param>
        public static EquipmentSlotType GetEquipmentSlotType(Type type)
        {
            string typeName = type.FullName;

            int index = equipmentSlotTypeBind.HasIndex(typeName) ? equipmentSlotTypeBind.GetIndex(typeName) : equipmentSlotTypeBind.GetNextId(typeName);
            return (EquipmentSlotType)index;
        }

        /// Registers an equipment slot of the specified type, using default prefab
        /// <typeparam name="T">Type of the equipment slot, which must implement <see cref="EquipmentSlot"/> and <see cref="IModEquipmentSlot"/>.</typeparam>
        /// <param name="objectType">Object type identifier equipment slot will use</param>
        /// <param name="assetName">Built in prefab name. Must be either <see cref="EMPTY_PREFAB"/> or <see cref="PLACEMENT_PREFAB"/> constants</param>
        /// <param name="logic">The logic handler class implementing <see cref="IEquipmentLogic"/> to manage the equipment slot's behavior.</param>
        public static void RegisterEquipmentSlot<T>(string objectType, string assetName, IEquipmentLogic logic)
            where T : global::EquipmentSlot, IModEquipmentSlot
        {
            var prefab = LoadPrefab(assetName, typeof(T));
            var component = prefab.AddComponent<T>();

            if (component is PlaceObjectSlot placeObjectSlot)
            {
                placeObjectSlot.placementHandler = prefab.GetComponentInChildren<PlacementHandler>(true);
            }
            
            RegisterEquipmentSlotImpl<T>(objectType, prefab, logic, true);
        }

        /// Registers a new equipment slot type, adding it to the internal management system.
        /// <typeparam name="T">Type of equipment slot, which must implement both EquipmentSlot and IModEquipmentSlot.</typeparam>
        /// <param name="objectType">Object type identifier equipment slot will use</param>
        /// <param name="prefab">The prefab GameObject associated with this equipment slot type.</param>
        /// <param name="logic">The logic handler class implementing <see cref="IEquipmentLogic"/> to manage the equipment slot's behavior.</param>
        /// <exception cref="ArgumentException">Thrown if the prefab does not contain the specified equipment slot type or if the slot type is already registered.</exception>
        [UsedImplicitly]
        public static void RegisterEquipmentSlot<T>(string objectType, GameObject prefab, IEquipmentLogic logic)
            where T : global::EquipmentSlot, IModEquipmentSlot
        {
            RegisterEquipmentSlotImpl<T>(objectType, prefab, logic, false);
        }

        private static void RegisterEquipmentSlotImpl<T>(
            string objectType, 
            GameObject prefab, 
            IEquipmentLogic logic,
            bool createPool = false
        ) where T : global::EquipmentSlot, IModEquipmentSlot
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

            DataBlockAddress poolAddress;

            if (createPool)
            {
                log.LogInfo($"Registering {typeof(T)} equipment slot prefab for pooling");
                var poolBlock = API.DataBlocks.CreateRuntimeInstance<PooledObjectDataBlock>(CoreLibMod.modInfo.ModId);

                poolBlock.prefab = prefab;
                poolBlock.name = $"{objectType}_pool";
            
                var addr = new DataBlockAddress("969c1f24-c01e-5b44-dae6-06726b6d896c"); // 4_16_1024 pool params
                poolBlock.poolParams = addr;
                poolAddress = poolBlock.address;
            }
            else
            {
                var poolReference = prefab.GetComponent<PoolReference>();
                if (poolReference == null)
                    throw new ArgumentException($"Equipment slot prefab for {objectType} doesn't have PoolReference component on the root game object!");
                
                if (!poolReference.poolRef.hasAddress)
                    throw new ArgumentException($"PoolReference for equipment slot {objectType} is not assigned a PooledObjectDataBlock!");

                poolAddress = poolReference.poolRef.address;
            }
            
            var slotInfo = new SlotInfo()
            {
                objectType = objectTypeID,
                slotPool = poolAddress,
                slotPrefab = prefab,
                logic = logic,
                needsResizing = logic.CanResize
            };
            slots.Add(slotType, slotInfo);
            
            log.LogInfo($"Equipment slot {typeof(T)} added");
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
        /// <param name="position">The position where the emote text should be spawned.</param>
        /// <param name="emoteType">The type of emote text to be spawned</param>
        /// <param name="randomizePosition">Indicates whether the position of the emote text should be randomized. Defaults to true.</param>
        /// <param name="replace">Indicates whether existing emotes should be replaced. Defaults to true.</param>
        [UsedImplicitly]
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

        /// Represents the starting ID value for defining custom equipment slot types
        public const int MOD_SLOT_TYPE_ID_START = 128;

        /// Represents the ending ID value for defining custom equipment slot types
        public const int MOD_SLOT_TYPE_ID_END = byte.MaxValue;
        
        /// Represents the starting ID value for defining custom emote types
        public const int MOD_EMOTE_TYPE_ID_START = short.MaxValue;

        /// Represents the ending ID value for defining custom emote types
        public const int MOD_EMOTE_TYPE_ID_END = int.MaxValue;

        /// Represents the starting ID value for mod-defined object type IDs.
        public const int MOD_OBJECT_TYPE_ID_RANGE_START = 33000;

        /// Represents the ending ID value for mod-defined object type IDs.
        public const int MOD_OBJECT_TYPE_ID_RANGE_END = ushort.MaxValue;

        /// A dictionary that maps <see cref="EquipmentSlotType"/> to corresponding <see cref="SlotInfo"/> instances.
        internal static Dictionary<EquipmentSlotType, SlotInfo> slots = new();

        /// A dictionary that maps emote types to their corresponding text representations.
        internal static Dictionary<Emote.EmoteType, string> textEmotes = new();

        /// Represents the binding mechanism for associating types with unique slot type identifiers within a predefined range.
        internal static IdBind equipmentSlotTypeBind = new(MOD_SLOT_TYPE_ID_START, MOD_SLOT_TYPE_ID_END);

        /// Represents an instance of the <see cref="IdBind"/> class responsible for managing and binding unique identifiers
        /// for custom emote types within the application.
        internal static IdBind emoteTypeBind = new(MOD_EMOTE_TYPE_ID_START, MOD_EMOTE_TYPE_ID_END);

        /// Represents a binding mechanism managing unique identifiers for object types within a specific range.
        internal static IdBind objectTypeIDs = new(MOD_OBJECT_TYPE_ID_RANGE_START, MOD_OBJECT_TYPE_ID_RANGE_END);

        internal static bool GetSlotInfoFor(EquipmentSlotType slotType, out SlotInfo slotInfo)
        {
            slotInfo = null;
            var slotTypeNum = (int)slotType;
            if (slotTypeNum < MOD_SLOT_TYPE_ID_START) return false;
            
            return slots.TryGetValue(slotType, out slotInfo);
        }
        
        /// Configures and applies necessary patches or hooks for the functionality provided by the module.
        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(EmotePatch));
            CoreLibMod.Patch(typeof(PlayerControllerPatch));
            CoreLibMod.Patch(typeof(ObjectAuthoringConverterPatch));
            CoreLibMod.Patch(typeof(PlacementHandlerPatch));
            CoreLibMod.Patch(typeof(MemoryManager_Patch));
            CoreLibMod.Patch(typeof(ObjectAuthoring_Patch));
            CoreLibMod.Patch(typeof(EquipmentSlot_Patch));
            
            API.Authoring.OnObjectTypeAdded += ModifyPlayer;
        }

        /// Modifies player entity to support equipment tool resizing
        private static void ModifyPlayer(
            Unity.Entities.Entity entity,
            GameObject authoringdata,
            EntityManager entitymanager
        )
        {
            var objectId = authoringdata.GetEntityObjectID();
            if (objectId != ObjectID.Player) return;

            if (slots.Values.All(slot => !slot.needsResizing)) return;

            const int vanillaSize = PlacementSizeByEquipmentTypeBuffer.EquipmentWithPlacementSize;
            var lastIndex = vanillaSize;

            var buffer = entitymanager.GetBuffer<PlacementSizeByEquipmentTypeBuffer>(entity);

            foreach (var slot in slots.Values)
            {
                if (!slot.needsResizing) continue;

                buffer.Add(new PlacementSizeByEquipmentTypeBuffer()
                {
                    sizeVariationToPlace = 254
                });

                slot.resizeIndex = lastIndex;
                lastIndex++;
            }
        }

        /// Loads and initializes the Equipment Module by setting up necessary dependencies and event handlers.
        internal override void Load()
        {
            base.Load();
            API.Client.OnWorldCreated += ClientWorldReady;
            API.Server.OnWorldCreated += ServerWorldReady;
        }

        /// Loads a prefab and initializes it as a new instance configured for the given slot type.
        /// <param name="assetName">The name of the prefab to load</param>
        /// <param name="slotType">The type of the equipment slot for which the prefab is being loaded.</param>
        internal static GameObject LoadPrefab(string assetName, Type slotType)
        {
            GameObject prefab = Mod.LoadAsset<GameObject>(assetName);

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

        /// Initializes server-side equipment update systems when the game world is created on the server.
        private static void ServerWorldReady()
        {
            CreateEquipmentUpdateSystems(API.Server.World);
        }

        /// Creates and initializes the necessary systems for updating equipment within a specified World instance.
        /// <param name="world">The <see cref="World"/> instance where the equipment update systems will be added and managed.</param>
        private static void CreateEquipmentUpdateSystems(World world)
        {

            var updateSystem = world.GetOrCreateSystemManaged<ModEquipmentSystem>();
            var equipmentGroup = world.GetExistingSystemManaged<EquipmentUpdateSystemGroup>();
            equipmentGroup.AddSystemToUpdateList(updateSystem);
            
            var changeSystem = world.GetOrCreateSystemManaged<ModEquipmentChangeSystem>();
            var equipmentBeforeGroup = world.GetExistingSystemManaged<EquipmentBeforeUpdateSystemGroup>();
            equipmentBeforeGroup.AddSystemToUpdateList(changeSystem);
        }
    }
}