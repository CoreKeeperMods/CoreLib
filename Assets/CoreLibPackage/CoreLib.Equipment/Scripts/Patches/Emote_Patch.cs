using System.Collections.Generic;
using CoreLib.Util.Extensions;
using HarmonyLib;
using UnityEngine;

namespace CoreLib.Equipment.Patches
{
    public static class Emote_Patch
    {
        internal static List<Emote> lastEmotes = new List<Emote>();

        internal static void FadeQuickly(Emote emote)
        {
            emote.fadeEffect.fadeOutTime = 0.1f;
            emote.StartFadeOut();
        }
        
        [HarmonyPatch(typeof(Emote), nameof(Emote.OnOccupied))]
        [HarmonyPostfix]
        public static void OnOccupied(Emote __instance)
        {
            __instance.fadeEffect.fadeOutTime = 0.5f;

            if (EquipmentModule.textEmotes.ContainsKey(__instance.emoteTypeInput))
            {
                __instance.SetValue("textToPrint", EquipmentModule.textEmotes[__instance.emoteTypeInput]);
                ApplyEmote(__instance);
            }
        }

        private static void ApplyEmote(Emote __instance)
        {
            __instance.text.localize = true;
            __instance.textOutline.localize = true;

            string textToPrint = __instance.GetValue<string>("textToPrint");
            __instance.text.Render(textToPrint, true);
            __instance.textOutline.Render(textToPrint, true);
            __instance.textOutline.SetTempColor(Color.black);
        }
    }
}