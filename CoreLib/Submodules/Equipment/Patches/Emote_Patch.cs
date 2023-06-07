using HarmonyLib;
using UnityEngine;

namespace CoreLib.Submodules.Equipment.Patches
{
    public static class Emote_Patch
    {
        [HarmonyPatch(typeof(Emote), nameof(Emote.OnOccupied))]
        [HarmonyPostfix]
        public static void OnOccupied(Emote __instance)
        {
            if (EquipmentSlotModule.textEmotes.ContainsKey(__instance.emoteTypeInput))
            {
                __instance.textToPrint = EquipmentSlotModule.textEmotes[__instance.emoteTypeInput];
                ApplyEmote(__instance);
            }
        }

        private static void ApplyEmote(Emote __instance)
        {
            __instance.text.localize = true;
            __instance.textOutline.localize = true;

            __instance.text.Render(__instance.textToPrint, true);
            __instance.textOutline.Render(__instance.textToPrint, true);
            __instance.textOutline.SetTempColor(Color.black);
        }
    }
}