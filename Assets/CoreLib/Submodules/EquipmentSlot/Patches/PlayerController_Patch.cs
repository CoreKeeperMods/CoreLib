using System;
using System.Linq;
using HarmonyLib;

namespace CoreLib.Submodules.Equipment.Patches
{
    [HarmonyPatch]
    public static class PlayerController_Patch
    {
        [HarmonyPatch(typeof(PlayerController), "GetSlotTypeForObjectType")]
        [HarmonyPostfix]
        public static void DetermineSlotType(ObjectType objectType, ref Type __result)
        {
            int objectId = (int)objectType;
            if (objectId < short.MaxValue) return;
            
            try
            {
                var slotPair = EquipmentSlotModule.slotPrefabs.First(pair =>
                {
                    EquipmentSlot equipmentSlot = pair.Value.GetComponent<EquipmentSlot>();
                    if (equipmentSlot is IModEquipmentSlot modEquipmentSlot)
                    {
                        return modEquipmentSlot.GetSlotObjectType() == objectType;
                    }
                    return false;
                });
                __result = slotPair.Key;
            }
            catch (InvalidOperationException)
            {
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.UpdateEquippedSlotVisuals))]
        [HarmonyPostfix]
        public static void UpdateSlotVisuals(PlayerController __instance)
        {
            EquipmentSlot slot = __instance.GetEquippedSlot();
            if (slot == null) return;
            
            if ((int)slot.GetSlotType() >= EquipmentSlotModule.ModSlotTypeIdStart &&
                slot is IModEquipmentSlot modEquipmentSlot)
            {
                modEquipmentSlot.UpdateSlotVisuals(__instance);
            }
        }
    }
}