using System.Collections.Generic;
using CoreLib.Util.Extensions;
using HarmonyLib;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.EquipmentSlot.Patches
{
    /// <summary>
    /// Provides patches and utility methods for handling emote behavior within the system.
    /// </summary>
    public static class Emote_Patch
    {
        /// <summary>
        /// A static list that keeps track of the most recently spawned emote objects in the system.
        /// </summary>
        /// <remarks>
        /// The <c>lastEmotes</c> list is used to store references to emotes being managed, allowing their state or behavior to be modified after spawning.
        /// It is also utilized to clear or handle existing emote objects when new ones are spawned, ensuring proper state management within the system.
        /// </remarks>
        internal static List<Emote> lastEmotes = new List<Emote>();

        /// <summary>
        /// Fades out the specified emote immediately with a very short fade-out time.
        /// </summary>
        /// <param name="emote">The emote instance to fade quickly.</param>
        internal static void FadeQuickly(Emote emote)
        {
            emote.fadeEffect.fadeOutTime = 0.1f;
            emote.StartFadeOut();
        }

        /// <summary>
        /// Handles the behavior when the <see cref="Emote.OnOccupied"/> event is triggered.
        /// </summary>
        /// <param name="__instance">The instance of the <see cref="Emote"/> that triggered the OnOccupied event.</param>
        [HarmonyPatch(typeof(Emote), nameof(Emote.OnOccupied))]
        [HarmonyPostfix]
        public static void OnOccupied(Emote __instance)
        {
            __instance.fadeEffect.fadeOutTime = 0.5f;

            if (EquipmentModule.TextEmotes.ContainsKey(__instance.emoteTypeInput))
            {
                __instance.SetValue("textToPrint", EquipmentModule.TextEmotes[__instance.emoteTypeInput]);
                ApplyEmote(__instance);
            }
        }

        /// <summary>
        /// Applies the provided emote, rendering its associated text with localization enabled.
        /// </summary>
        /// <param name="__instance">The emote instance to be applied, with its text rendered and localized.</param>
        private static void ApplyEmote(Emote __instance)
        {
            __instance.text.localize = true;
           // __instance.textOutline.localize = true;

            string textToPrint = __instance.GetValue<string>("textToPrint");
            __instance.text.Render(textToPrint, true);
           // __instance.textOutline.Render(textToPrint, true);
           // __instance.textOutline.SetTempColor(Color.black);
        }
    }
}