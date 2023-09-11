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
            if (EquipmentModule.textEmotes.ContainsKey(__instance.emoteTypeInput))
            {
                __instance.SetField("textToPrint", EquipmentModule.textEmotes[__instance.emoteTypeInput]);
                ApplyEmote(__instance);
            }
        }

        private static void ApplyEmote(Emote __instance)
        {
            __instance.text.localize = true;
            __instance.textOutline.localize = true;

            string textToPrint = __instance.GetField<string>("textToPrint");
            __instance.text.Render(textToPrint, true);
            __instance.textOutline.Render(textToPrint, true);
            __instance.textOutline.SetTempColor(Color.black);
        }
    }
}