using System;
using System.Collections.Generic;
using CoreLib.Data;
using CoreLib.Equipment.Patches;
using CoreLib.ModResources;
using CoreLib.Submodules.ModEntity;
using CoreLib.Util.Extensions;
using UnityEngine;

namespace CoreLib.Equipment
{
    public class EquipmentModule : BaseSubmodule
    {
        internal override GameVersion Build => new GameVersion(0, 7, 3, "a28f");
        internal override string Version => "3.1.0";
        internal static EquipmentModule Instance => CoreLibMod.GetModuleInstance<EquipmentModule>();

        public static readonly string EMPTY_PREFAB = "Assets/CoreLibPackage/CoreLib.Equipment/Prefab/EmptySlot";
        public static readonly string PLACEMENT_PREFAB = "Assets/CoreLibPackage/CoreLib.Equipment/Prefab/DefaultPlaceSlot";

        public static ObjectType GetObjectType(string typeName)
        {
            int index = objectTypeIDs.HasIndex(typeName) ? 
                objectTypeIDs.GetIndex(typeName) : 
                objectTypeIDs.GetNextId(typeName);
            return (ObjectType)index;
        }

        public static EquipmentSlot.EquipmentSlotType GetEquipmentSlotType<T>()
            where T : EquipmentSlot, IModEquipmentSlot
        {
            string typeName = typeof(T).FullName;

            int index = equipmentSlotTypeBind.HasIndex(typeName) ? equipmentSlotTypeBind.GetIndex(typeName) : equipmentSlotTypeBind.GetNextId(typeName);
            return (EquipmentSlot.EquipmentSlotType)index;
        }

        public static void RegisterEquipmentSlot<T>(GameObject prefab)
            where T : EquipmentSlot, IModEquipmentSlot
        {
            EquipmentSlot slot = prefab.GetComponent<T>();

            if (slot == null)
            {
                throw new ArgumentException($"Failed to get Equipment slot main class! Please check your prefab.");
            }

            Type slotType = typeof(T);

            if (slotPrefabs.ContainsKey(slotType))
            {
                throw new ArgumentException($"Equipment Slot with type {slotType.FullName} was already registered!");
            }

            slotPrefabs.Add(slotType, prefab);
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

        public const int ModSlotTypeIdStart = 128;
        public const int ModSlotTypeIdEnd = byte.MaxValue;

        public const int ModEmoteTypeIdStart = short.MaxValue;
        public const int ModEmoteTypeIdEnd = int.MaxValue;
        
        public const int modObjectTypeIdRangeStart = 33000;
        public const int modObjectTypeIdRangeEnd = ushort.MaxValue;

        internal static Dictionary<Type, GameObject> slotPrefabs = new Dictionary<Type, GameObject>();
        internal static Dictionary<Emote.EmoteType, string> textEmotes = new Dictionary<Emote.EmoteType, string>();

        internal static IdBind equipmentSlotTypeBind = new IdBind(ModSlotTypeIdStart, ModSlotTypeIdEnd);
        internal static IdBind emoteTypeBind = new IdBind(ModEmoteTypeIdStart, ModEmoteTypeIdEnd);
        internal static IdBind objectTypeIDs = new IdBind(modObjectTypeIdRangeStart, modObjectTypeIdRangeEnd);
        
        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(Emote_Patch));
            CoreLibMod.Patch(typeof(PlayerController_Patch_2));
        }
        
        internal override void Load()
        {
            ResourcesModule.RefreshModuleBundles();
        }
    }
}