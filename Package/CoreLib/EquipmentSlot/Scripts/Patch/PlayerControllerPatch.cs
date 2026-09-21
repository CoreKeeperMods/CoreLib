using System;
using CoreLib.Submodule.EquipmentSlot.Interface;
using HarmonyLib;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.EquipmentSlot.Patch
{
    /// Provides patch implementations for the PlayerController class, focusing on
    /// specific behaviors related to equipment slot handling and visual updates for slots.
    public static class PlayerControllerPatch
    {
        [HarmonyPatch(typeof(PlayerController), "GetSlotPoolForObjectType")]
        [HarmonyPostfix]
        public static void DetermineSlotPool(
            ObjectType objectType, 
            ObjectDataCD objectData,
            ref DataBlockAddress __result
        ) {
            int objectId = (int)objectType;
            if (objectId < short.MaxValue) return;

            foreach (var slotInfo in EquipmentSlotModule.slots.Values)
            {
                if (slotInfo.objectType == objectType)
                {
                    __result = slotInfo.slotPool;
                }
            }
        }

        /// Updates the visuals of the currently equipped equipment slot within the player controller.
        /// <param name="__instance">The instance of the PlayerController for which the slot visuals are being updated.</param>
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.UpdateEquippedSlotVisuals))]
        [HarmonyPostfix]
        public static void UpdateSlotVisuals(PlayerController __instance)
        {
            var slot = __instance.GetEquippedSlot();
            if (slot == null) return;

            if ((int)slot.GetSlotType() >= EquipmentSlotModule.MOD_SLOT_TYPE_ID_START &&
                slot is IModEquipmentSlot modEquipmentSlot)
            {
                modEquipmentSlot.UpdateSlotVisuals(__instance);
            }
        }
    }
}