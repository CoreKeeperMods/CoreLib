using System;
using System.Collections.Generic;
using CoreLib.Data;
using CoreLib.Equipment.Patches;
using CoreLib.Equipment.System;
using CoreLib.ModResources;
using CoreLib.Submodules.ModEntity;
using CoreLib.Util.Extensions;
using JetBrains.Annotations;
using PlayerEquipment;
using PugMod;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreLib.Equipment
{
    public class EquipmentModule : BaseSubmodule
    {
        internal override GameVersion Build => new GameVersion(1, 1, 0, "90bc");
        internal override string Version => "4.0.0";
        internal static EquipmentModule Instance => CoreLibMod.GetModuleInstance<EquipmentModule>();

        [UsedImplicitly] public static readonly string EMPTY_PREFAB = "Assets/CoreLibPackage/CoreLib.Equipment/Prefab/EmptySlot";
        [UsedImplicitly] public static readonly string PLACEMENT_PREFAB = "Assets/CoreLibPackage/CoreLib.Equipment/Prefab/DefaultPlaceSlot";

        public static ObjectType GetObjectType(string typeName)
        {
            int index = objectTypeIDs.HasIndex(typeName) ? 
                objectTypeIDs.GetIndex(typeName) : 
                objectTypeIDs.GetNextId(typeName);
            return (ObjectType)index;
        }

        public static EquipmentSlotType GetEquipmentSlotType<T>()
            where T : EquipmentSlot, IModEquipmentSlot
        {
            return GetEquipmentSlotType(typeof(T));
        }

        public static EquipmentSlotType GetEquipmentSlotType(Type type)
        {
            string typeName = type.FullName;

            int index = equipmentSlotTypeBind.HasIndex(typeName) ? equipmentSlotTypeBind.GetIndex(typeName) : equipmentSlotTypeBind.GetNextId(typeName);
            return (EquipmentSlotType)index;
        }

        public static void RegisterEquipmentSlot<T>(string objectType, string prefabPath, IEquipmentLogic logic)
            where T : EquipmentSlot, IModEquipmentSlot
        {
            var prefab = LoadPrefab(prefabPath, typeof(T));
            var component = prefab.AddComponent<T>();

            if (component is PlaceObjectSlot placeObjectSlot)
            {
                placeObjectSlot.placementHandler = prefab.GetComponentInChildren<PlacementHandler>(true);
            }
            
            RegisterEquipmentSlot<T>(objectType, prefab, logic);
        }

        public static void RegisterEquipmentSlot<T>(string objectType, GameObject prefab, IEquipmentLogic logic)
            where T : EquipmentSlot, IModEquipmentSlot
        {
            EquipmentSlot slot = prefab.GetComponent<T>();

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
            
            EntityModule.AddToAuthoringList(prefab);
            EntityModule.EnablePooling(prefab);
        }

        public static Emote.EmoteType RegisterTextEmote(string emoteId)
        {
            Emote.EmoteType emoteType = (Emote.EmoteType)emoteTypeBind.GetNextId(emoteId);
            string emoteTerm = $"Emotes/MOD_{emoteId}";
            
            //LocalizationModule.AddTerm(emoteTerm, emoteTexts);
            textEmotes.Add(emoteType, emoteTerm);
            
            return emoteType;
        }
        
        public static void SpawnModEmoteText(
            Vector3 position, 
            Emote.EmoteType emoteType,
            bool randomizePosition = true,
            bool replace = true)
        {
            if (replace && Emote_Patch.lastEmotes.Count > 0)
            {
                foreach (Emote lastEmote in Emote_Patch.lastEmotes)
                {
                    Emote_Patch.FadeQuickly(lastEmote);
                }
                Emote_Patch.lastEmotes.Clear();
            }
            
            var emote = Emote.SpawnEmoteText(position, emoteType, randomizePosition, false, false);
            Emote_Patch.lastEmotes.Add(emote);
        }

        public const int ModSlotTypeIdStart = 128;
        public const int ModSlotTypeIdEnd = byte.MaxValue;

        public const int ModEmoteTypeIdStart = short.MaxValue;
        public const int ModEmoteTypeIdEnd = int.MaxValue;
        
        public const int modObjectTypeIdRangeStart = 33000;
        public const int modObjectTypeIdRangeEnd = ushort.MaxValue;

        internal static Dictionary<EquipmentSlotType, SlotInfo> slots = new Dictionary<EquipmentSlotType, SlotInfo>();
        internal static Dictionary<Emote.EmoteType, string> textEmotes = new Dictionary<Emote.EmoteType, string>();

        internal static IdBind equipmentSlotTypeBind = new IdBind(ModSlotTypeIdStart, ModSlotTypeIdEnd);
        internal static IdBind emoteTypeBind = new IdBind(ModEmoteTypeIdStart, ModEmoteTypeIdEnd);
        internal static IdBind objectTypeIDs = new IdBind(modObjectTypeIdRangeStart, modObjectTypeIdRangeEnd);
        
        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(Emote_Patch));
            CoreLibMod.Patch(typeof(PlayerController_Patch_2));
            CoreLibMod.Patch(typeof(ObjectAuthoringConverter_Patch));
            CoreLibMod.Patch(typeof(PlacementHandler_Patch));
        }
        
        internal override void Load()
        {
            ResourcesModule.RefreshModuleBundles();
            
            API.Client.OnWorldCreated += ClientWorldReady;
            API.Server.OnWorldCreated += ServerWorldReady;
        }
        
        internal static GameObject LoadPrefab(string prefabPath, Type slotType)
        {
            GameObject prefab = ResourcesModule.LoadAsset<GameObject>(prefabPath);

            GameObject newPrefab = Object.Instantiate(prefab);
            newPrefab.hideFlags = HideFlags.HideAndDontSave;
            newPrefab.name = $"{slotType.GetNameChecked()}_Prefab";

            return newPrefab;
        }
        
        private static void ClientWorldReady()
        {
            CreateEquipmentUpdateSystems(API.Client.World);
        }
        
        private static void ServerWorldReady()
        {
            CreateEquipmentUpdateSystems(API.Server.World);
        }

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