using System.Collections.Generic;
using System.Linq;
using CoreLib.Commands.Communication;
using CoreLib.Util.Extensions;
using HarmonyLib;
using UnityEngine;

namespace CoreLib.Commands.Patches
{
    internal class ChatWindow_Patch
    {
        /// <summary>
        /// Represents a collection of command strings used to store the history of user input in the chat window.
        /// </summary>
        /// <remarks>
        /// The <c>history</c> variable is used to maintain a list of previously entered commands.
        /// This history allows for navigation and re-use of past inputs via specific key bindings (e.g., up and down arrows).
        /// The number of entries stored in the history is limited by the <c>maxHistoryLen</c> constant.
        /// </remarks>
        private static List<string> history = new List<string>();

        /// <summary>
        /// Represents the index of the currently selected message within the history list in the chat window.
        /// </summary>
        /// <remarks>
        /// This variable is used to navigate through the chat history when scrolling up or down.
        /// It indicates the position in the <c>history</c> list and is updated when the user presses
        /// associated navigation keys, such as up or down. The value is initialized to -1, indicating
        /// no history selection, and is constrained within the bounds of the history list's indices.
        /// </remarks>
        public static int currentHistoryIndex = -1;

        /// <summary>
        /// Defines the maximum number of entries that can be stored in the input history.
        /// When the history exceeds this limit, the oldest entries are removed to maintain the size.
        /// </summary>
        public static int maxHistoryLen = 10;

        /// Handles the update logic for the ChatWindow, processes input, and manages command history navigation.
        /// This method is called as a prefix to the ChatWindow's Update method and handles:
        /// - Processing incoming command messages from the communication system.
        /// - Navigating the command input history (up/down keys).
        /// - Handling auto-completion functionality (tab key).
        /// <param name="__instance">The instance of the ChatWindow being updated.</param>
        [HarmonyPatch(typeof(ChatWindow), "Update")]
        [HarmonyPrefix]
        public static void OnUpdate(ChatWindow __instance)
        {
            if (CommandsModule.ClientCommSystem == null) return;

            while (CommandsModule.ClientCommSystem.TryGetNextMessage(out CommandMessage message))
            {
                if (message.messageType == CommandMessageType.RelayCommand) continue;

                if (message.commandFlags.HasFlag(CommandFlags.SentFromQuantumConsole))
                {
                    CommandsModule.SendQCMessage(message.message, message.status);
                }
                else
                {
                    SendMessage(__instance, message.message, message.status.GetColor());
                }
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
                string input = __instance.inputField.displayedTextString;
                string[] args = input.Split(' ');
                if (args[0].StartsWith(CommandsModule.CommandPrefix))
                {
                    string cmdName = args[0].Substring(1);
                    ICommandInfo[] commandHandlers = CommandsModule.commandHandlers
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

        /// Handles the logic for sending messages in the chat window, including command processing and updating the input history.
        /// This method intercepts the message-sending action in the ChatWindow to:
        /// - Process the entered text as a potential command through the CommandsModule.
        /// - Send the chat message if it is not a command.
        /// - Update the message history for navigation purposes.
        /// - Prevent message duplication when the commit flag is set to true.
        /// <param name="__instance">The instance of the ChatWindow from which the message is being sent.</param>
        /// <param name="commit">A flag indicating whether the input should be committed as a message.</param>
        [HarmonyPatch(typeof(ChatWindow), nameof(ChatWindow.Deactivate))]
        [HarmonyPrefix]
        public static void OnSendMessage(ChatWindow __instance, ref bool commit)
        {
            if (commit)
            {
                PugText text = __instance.inputField;
                string input = text.displayedTextString;

                if (CommandsModule.SendCommand(input)) return;

                SendMessage(__instance, input, Color.white);
                UpdateHistory(input);
                commit = false;
            }
        }

        /// Modifies the allocated PugText instance by setting its color to white.
        /// This method is executed as a postfix to the ChatWindow's AllocPugText method and ensures
        /// that the color properties of the created PugText object are properly initialized.
        /// <param name="__result">The PugText instance created by the AllocPugText method.</param>
        [HarmonyPatch(typeof(ChatWindow), "AllocPugText")]
        [HarmonyPostfix]
        public static void OnAllocPugText(PugText __result)
        {
            SetColor(__result, Color.white);
        }

        /// Updates the chat input history by adding a new entry and managing the history capacity.
        /// This method maintains the chat history by:
        /// - Adding the new input command to the history.
        /// - Ensuring the history size does not exceed the maximum defined length by removing the oldest entry.
        /// - Resetting the current history index to the end of the list.
        /// <param name="input">The input command to be added to the chat history.</param>
        private static void UpdateHistory(string input)
        {
            history.Add(input);
            if (history.Count > maxHistoryLen)
            {
                history.RemoveAt(0);
            }

            currentHistoryIndex = history.Count;
        }

        /// Sends a formatted message to the ChatWindow, applies color, and manages fading effects for the displayed message.
        /// This method is used to render a message within the ChatWindow UI, setting up effects and passing the output to the ChatWindow instance.
        /// <param name="window">The instance of ChatWindow where the message will be displayed.</param>
        /// <param name="message">The message content to be displayed in the ChatWindow.</param>
        /// <param name="color">The color to be applied to the rendered message.</param>
        internal static void SendMessage(ChatWindow window, string message, Color color)
        {
            object[] args = { ChatWindow.MessageTextType.Sent, null, null };
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

        /// Sets the color of the provided PugText instance to the specified color.
        /// Updates the text's current style color, the default style color, and the overall color attribute.
        /// <param name="pugText">The PugText instance whose color needs to be updated.</param>
        /// <param name="color">The new color to apply to the PugText instance.</param>
        private static void SetColor(PugText pugText, Color color)
        {
            pugText.style.color = color;
            pugText.GetValue<PugTextStyle>("defaultStyle").color = color;
            pugText.color = color;
        }
    }
}