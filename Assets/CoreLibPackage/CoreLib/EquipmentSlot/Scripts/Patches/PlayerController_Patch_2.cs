using System;
using HarmonyLib;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.EquipmentSlot.Patches
{
    /// <summary>
    /// Provides patch implementations for the PlayerController class, focusing on
    /// specific behaviors related to equipment slot handling and visual updates for slots.
    /// </summary>
    public static class PlayerController_Patch_2
    {
        /// Determines the slot type based on the provided object type.
        /// This method checks if the object type matches the criteria of any defined slot information,
        /// and if so, it sets the resulting slot type accordingly. The result is assigned only if a match is found.
        /// <param name="objectType">The type of the object to determine the corresponding slot type for.</param>
        /// <param name="__result">The reference to the result where the determined slot type is assigned.</param>
        [HarmonyPatch(typeof(PlayerController), "GetSlotTypeForObjectType")]
        [HarmonyPostfix]
        public static void DetermineSlotType(ObjectType objectType, ref Type __result)
        {
            int objectId = (int)objectType;
            if (objectId < short.MaxValue) return;

            foreach (var slotInfo in EquipmentModule.Slots.Values)
            {
                if (slotInfo.objectType == objectType)
                {
                    __result = slotInfo.slotType;
                }
            }
        }

        /// <summary>
        /// Updates the visuals of the currently equipped equipment slot within the player controller.
        /// </summary>
        /// <param name="__instance">The instance of the PlayerController for which the slot visuals are being updated.</param>
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.UpdateEquippedSlotVisuals))]
        [HarmonyPostfix]
        public static void UpdateSlotVisuals(PlayerController __instance)
        {
            global::EquipmentSlot slot = __instance.GetEquippedSlot();
            if (slot == null) return;

            if ((int)slot.GetSlotType() >= EquipmentModule.ModSlotTypeIdStart &&
                slot is IModEquipmentSlot modEquipmentSlot)
            {
                modEquipmentSlot.UpdateSlotVisuals(__instance);
            }
        }
    }
}