using System;
using System.Collections.Generic;
using System.Reflection;
using CoreLib.Submodules.Common.Patches;
using CoreLib.Submodules.Equipment.Patches;
using CoreLib.Submodules.Localization;
using CoreLib.Submodules.ModResources;
using CoreLib.Util;
using Il2CppInterop.Runtime.Injection;
using Unity.Physics.Authoring;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreLib.Submodules.Equipment
{
    [CoreLibSubmodule(Dependencies = new[] { typeof(ResourcesModule), typeof(LocalizationModule) })]
    public static class EquipmentSlotModule
    {
        public static readonly string EMPTY_PREFAB = "Assets/CoreLib/Slots/EmptySlot";
        public static readonly string PLACEMENT_PREFAB = "Assets/CoreLib/Slots/DefaultPlaceSlot";


        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded
        {
            get => _loaded;
            internal set => _loaded = value;
        }

        public static EquipmentSlot.EquipmentSlotType GetEquipmentSlotType<T>()
            where T : EquipmentSlot, IModEquipmentSlot
        {
            ThrowIfNotLoaded();
            string typeName = typeof(T).FullName;

            int index = equipmentSlotTypeBind.HasIndex(typeName) ? equipmentSlotTypeBind.GetIndex(typeName) : equipmentSlotTypeBind.GetNextId(typeName);
            return (EquipmentSlot.EquipmentSlotType)index;
        }

        public static void RegisterEquipmentSlot<T>(string prefabPath)
            where T : EquipmentSlot, IModEquipmentSlot
        {
            ThrowIfNotLoaded();
            ClassInjector.RegisterTypeInIl2Cpp<T>();

            GameObject prefab = ResourcesModule.LoadAsset<GameObject>(prefabPath);
            GameObject newPrefab = Object.Instantiate(prefab);
            newPrefab.SetActive(false);
            newPrefab.hideFlags = HideFlags.HideAndDontSave;
            ResourcesModule.Retain(newPrefab);
            MonoBehaviourUtils.AllocObject(newPrefab);

            EquipmentSlot slot;

            if (prefabPath.Contains("CoreLib"))
            {
                slot = SetupDefaultPrefab<T>(newPrefab);
            }
            else
            {
                slot = newPrefab.GetComponent<T>();
            }

            if (slot == null)
            {
                throw new ArgumentException($"Failed to get Equipment slot main class! Please check your prefab.");
            }

            Type slotType = typeof(T);

            if (slotPrefabs.ContainsKey(slotType))
            {
                throw new ArgumentException($"Equipment Slot with type {slotType.FullName} was already registered!");
            }

            slotPrefabs.Add(slotType, newPrefab);
        }

        public static Emote.EmoteType RegisterTextEmote(string emoteId, string enText, string cnText = "")
        {
            return RegisterTextEmote(emoteId, new Dictionary<string, string> { { "en", enText }, { "zh-CN", cnText } });
        }

        public static Emote.EmoteType RegisterTextEmote(string emoteId, Dictionary<string, string> emoteTexts)
        {
            ThrowIfNotLoaded();
            
            Emote.EmoteType emoteType = (Emote.EmoteType)emoteTypeBind.GetNextId(emoteId);
            string emoteTerm = $"Emotes/MOD_{emoteId}";
            
            LocalizationModule.AddTerm(emoteTerm, emoteTexts);
            textEmotes.Add(emoteType, emoteTerm);
            
            return emoteType;
        }


        private static EquipmentSlot SetupDefaultPrefab<T>(GameObject prefab) where T : EquipmentSlot
        {
            EquipmentSlot slot = prefab.AddComponent<T>();
            if (typeof(T).IsAssignableTo(typeof(PlaceObjectSlot)))
            {
                PlaceObjectSlot placeSlot = slot.Cast<PlaceObjectSlot>();
                PlacementHandler placementHandler = prefab.GetComponentInChildren<PlacementHandler>();
                placeSlot.placementHandler = placementHandler;

                placeSlot.collidesWith = new PhysicsCategoryTags()
                {
                    Category00 = true,
                    Category01 = true,
                    Category02 = true,
                    Category04 = true,
                    Category06 = true,
                    Category08 = true,
                    Category09 = true
                };
            }

            return slot;
        }

        private static bool _loaded;

        public const int ModSlotTypeIdStart = 128;
        public const int ModSlotTypeIdEnd = byte.MaxValue;

        public const int ModEmoteTypeIdStart = short.MaxValue;
        public const int ModEmoteTypeIdEnd = int.MaxValue;

        internal static Dictionary<Type, GameObject> slotPrefabs = new Dictionary<Type, GameObject>();
        internal static Dictionary<Emote.EmoteType, string> textEmotes = new Dictionary<Emote.EmoteType, string>();

        internal static IdBind equipmentSlotTypeBind = new IdBind(ModSlotTypeIdStart, ModSlotTypeIdEnd);
        internal static IdBind emoteTypeBind = new IdBind(ModEmoteTypeIdStart, ModEmoteTypeIdEnd);

        [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks()
        {
            MemoryManager_Patch.TryPatch();
            CoreLibPlugin.harmony.PatchAll(typeof(PlayerController_Patch));
            CoreLibPlugin.harmony.PatchAll(typeof(Emote_Patch));
        }

        internal static void ThrowIfNotLoaded()
        {
            if (!Loaded)
            {
                Type submoduleType = MethodBase.GetCurrentMethod().DeclaringType;
                string message = $"{submoduleType.Name} is not loaded. Please use [{nameof(CoreLibSubmoduleDependency)}(nameof({submoduleType.Name})]";
                throw new InvalidOperationException(message);
            }
        }
    }
}