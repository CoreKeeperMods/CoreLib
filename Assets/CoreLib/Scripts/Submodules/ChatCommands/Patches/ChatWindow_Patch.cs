using System.Collections.Generic;
using System.Linq;
using CoreLib.Submodules.ChatCommands.Communication;
using HarmonyLib;
using UnityEngine;

namespace CoreLib.Submodules.ChatCommands.Patches
{
    internal class ChatWindow_Patch
    {
        private static List<string> history = new List<string>();

        public static int currentHistoryIndex = -1;
        public static int maxHistoryLen = 10;

        [HarmonyPatch(typeof(ChatWindow), "Update")]
        [HarmonyPrefix]
        public static void OnUpdate(ChatWindow __instance)
        {
            if (CommandsModule.ClientCommSystem == null) return;

            while (CommandsModule.ClientCommSystem.TryGetNextMessage(out CommandMessage message))
            {
                SendMessage(__instance, message.message, message.status.GetColor());
            }

            if (history.Count <= 0) return;

            bool pressedUpOrDown = false;
            bool pressedTab = false;
            string newText = "";

            if (CommandsModule.rewiredPlayer.GetButtonDown(CommandsModule.UP_KEY))
            {
                currentHistoryIndex--;
                if (currentHistoryIndex < 0)
                {
                    currentHistoryIndex = history.Count;
                }

                pressedUpOrDown = true;
            }

            if (CommandsModule.rewiredPlayer.GetButtonDown(CommandsModule.DOWN_KEY))
            {
                currentHistoryIndex++;
                if (currentHistoryIndex > history.Count)
                {
                    currentHistoryIndex = 0;
                }

                pressedUpOrDown = true;
            }

            if (CommandsModule.rewiredPlayer.GetButtonDown(CommandsModule.COMPLETE_KEY))
            {
                string input = __instance.inputField.textString;
                string[] args = input.Split(' ');
                if (args[0].StartsWith(CommandsModule.CommandPrefix))
                {
                    string cmdName = args[0].Substring(1);
                    IChatCommandHandler[] commandHandlers = CommandsModule.commandHandlers
                        .Select(pair => pair.handler)
                        .Where(handler => { return handler.GetTriggerNames().Any(name => name.StartsWith(cmdName)); }).ToArray();
                    if (commandHandlers.Length == 1)
                    {
                        string fullName = commandHandlers[0].GetTriggerNames().First(name => name.StartsWith(cmdName));
                        newText = $"{CommandsModule.CommandPrefix}{fullName}";
                        pressedTab = true;
                    }
                }
            }

            if (!pressedUpOrDown && !pressedTab) return;

            if (currentHistoryIndex >= 0 && currentHistoryIndex < history.Count && pressedUpOrDown)
            {
                newText = history[currentHistoryIndex];
            }

            __instance.inputField.Render(newText);
        }

        [HarmonyPatch(typeof(ChatWindow), nameof(ChatWindow.Deactivate))]
        [HarmonyPrefix]
        public static void OnSendMessage(ChatWindow __instance, ref bool commit)
        {
            if (commit)
            {
                PugText text = __instance.inputField;
                string input = text.textString;

                string[] args = input.Split(' ');
                if (args.Length < 1 || !args[0].StartsWith(CommandsModule.CommandPrefix)) return;
                if (CommandsModule.ClientCommSystem == null) return;

                SendMessage(__instance, input, Color.white);
                CommandsModule.ClientCommSystem.SendCommand(input);
                UpdateHistory(input);
                commit = false;
            }
        }

        [HarmonyPatch(typeof(ChatWindow), "AllocPugText")]
        [HarmonyPostfix]
        public static void OnAllocPugText(PugText __result)
        {
            SetColor(__result, Color.white);
        }

        private static void UpdateHistory(string input)
        {
            history.Add(input);
            if (history.Count > maxHistoryLen)
            {
                history.RemoveAt(0);
            }

            currentHistoryIndex = history.Count;
        }

        internal static void SendMessage(ChatWindow window, string message, Color color)
        {
            object[] args = { ChatWindow.MessageTextType.Sent, null };
            PugText pugText = window.Invoke<PugText>("AllocPugText", args);
            PugTextEffectMaxFade fadeEffect = (PugTextEffectMaxFade)args[1];

            pugText.Render(message);
            SetColor(pugText, color);
            if (fadeEffect != null)
            {
                fadeEffect.FadeOut();
                window.InvokeVoid("AddPugText", new object[] { ChatWindow.MessageTextType.Sent, pugText });
            }
        }

        private static void SetColor(PugText pugText, Color color)
        {
            pugText.style.color = color;
            pugText.GetField<PugTextStyle>("defaultStyle").color = color;
            pugText.color = color;
        }
    }
}