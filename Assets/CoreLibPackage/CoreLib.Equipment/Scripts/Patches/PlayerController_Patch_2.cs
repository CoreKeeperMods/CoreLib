using System;
using System.Linq;
using HarmonyLib;
// ReSharper disable InconsistentNaming

namespace CoreLib.Equipment.Patches
{
    public static class PlayerController_Patch_2
    {
        [HarmonyPatch(typeof(PlayerController), "GetSlotTypeForObjectType")]
        [HarmonyPostfix]
        public static void DetermineSlotType(ObjectType objectType, ref Type __result)
        {
            int objectId = (int)objectType;
            if (objectId < short.MaxValue) return;

            foreach (var slotInfo in EquipmentModule.slots.Values)
            {
                if (slotInfo.objectType == objectType)
                {
                    __result = slotInfo.slotType;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.UpdateEquippedSlotVisuals))]
        [HarmonyPostfix]
        public static void UpdateSlotVisuals(PlayerController __instance)
        {
            EquipmentSlot slot = __instance.GetEquippedSlot();
            if (slot == null) return;

            if ((int)slot.GetSlotType() >= EquipmentModule.ModSlotTypeIdStart &&
                slot is IModEquipmentSlot modEquipmentSlot)
            {
                modEquipmentSlot.UpdateSlotVisuals(__instance);
            }
        }
    }
}