using System;
using System.Linq;
using CoreLib.Submodules.CustomEntity;
using HarmonyLib;
using Il2CppInterop.Runtime;

namespace CoreLib.Submodules.Equipment.Patches
{
    public static class PlayerController_Patch
    {
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.GetSlotTypeForObjectType))]
        [HarmonyPostfix]
        public static void DetermineSlotType(ObjectType objectType, ref Il2CppSystem.Type __result)
        {
            int objectId = (int)objectType;
            if (objectId < CustomEntityModule.modObjectTypeIdRangeStart) return;
            
            try
            {
                var slotPair = EquipmentSlotModule.slotPrefabs.First(pair =>
                {
                    EquipmentSlot equipmentSlot = pair.Value.GetComponent<EquipmentSlot>();
                    ObjectType result = equipmentSlot.TryInvokeFunc(nameof(IModEquipmentSlot.GetSlotObjectType)).Unbox<ObjectType>();
                    return result == objectType;
                });
                __result = Il2CppType.From(slotPair.Key);
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
            if ((int)slot.slotType >= EquipmentSlotModule.ModSlotTypeIdStart)
            {
                slot.TryInvokeAction(nameof(IModEquipmentSlot.UpdateSlotVisuals), __instance);
            }
        }
    }
}