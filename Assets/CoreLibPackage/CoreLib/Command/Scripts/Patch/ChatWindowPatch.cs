using System.Collections.Generic;
using System.Linq;
using CoreLib.Submodule.Command.Data;
using CoreLib.Submodule.Command.Interface;
using CoreLib.Submodule.Command.Util;
using CoreLib.Util.Extension;
using HarmonyLib;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Command.Patch
{
    internal class ChatWindowPatch
    {
        /// Represents a collection of command strings used to store the history of user input in the chat window.
        /// <remarks>
        /// The <c>history</c> variable is used to maintain a list of previously entered commands.
        /// This history allows for navigation and re-use of past inputs via specific key bindings (e.g., up and down arrows).
        /// The number of entries stored in the history is limited by the <c>maxHistoryLen</c> constant.
        /// </remarks>
        private static readonly List<string> History = new List<string>();

        /// Represents the index of the currently selected message within the history list in the chat window.
        /// <remarks>
        /// This variable is used to navigate through the chat history when scrolling up or down.
        /// It indicates the position in the <c>history</c> list and is updated when the user presses
        /// associated navigation keys, such as up or down. The value is initialized to -1, indicating
        /// no history selection, and is constrained within the bounds of the history list's indices.
        /// </remarks>
        public static int CurrentHistoryIndex = -1;

        /// Defines the maximum number of entries that can be stored in the input history.
        /// When the history exceeds this limit, the oldest entries are removed to maintain the size.
        public static int MaxHistoryLen = 10;

        /// Handles the update logic for the ChatWindow, processes input, and manages command history navigation.
        /// This method is called as a prefix to the ChatWindow's Update method and handles:
        /// - Processing incoming command messages from the communication system.
        /// - Navigating the command input history (up/down keys).
        /// - Handling auto-completion functionality (tab key).
        /// <param name="__instance">The instance of the ChatWindow being updated.</param>
        [HarmonyPatch(typeof(ChatWindow), "Update")]
        [HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        public static void OnUpdate(ChatWindow __instance)
        {
            if (CommandModule.ClientCommSystem == null) return;

            while (CommandModule.ClientCommSystem.TryGetNextMessage(out CommandMessage message))
            {
                if (message.MessageType == CommandMessageType.RelayCommand) continue;

                if (message.CommandFlags.HasFlag(CommandFlags.SentFromQuantumConsole))
                {
                    CommandModule.SendQcMessage(message.Message, message.Status);
                }
                else
                {
                    SendMessage(__instance, message.Message, message.Status.GetColor());
                }
            }

            if (History.Count <= 0) return;

            bool pressedUpOrDown = false;
            bool pressedTab = false;
            string newText = "";

            if (CommandModule.RewiredPlayer.GetButtonDown(CommandModule.UpKey))
            {
                CurrentHistoryIndex--;
                if (CurrentHistoryIndex < 0)
                {
                    CurrentHistoryIndex = History.Count;
                }

                pressedUpOrDown = true;
            }

            if (CommandModule.RewiredPlayer.GetButtonDown(CommandModule.DownKey))
            {
                CurrentHistoryIndex++;
                if (CurrentHistoryIndex > History.Count)
                {
                    CurrentHistoryIndex = 0;
                }

                pressedUpOrDown = true;
            }

            if (CommandModule.RewiredPlayer.GetButtonDown(CommandModule.CompleteKey))
            {
                string input = __instance.inputField.displayedTextString;
                string[] args = input.Split(' ');
                if (args[0].StartsWith(CommandModule.CommandPrefix))
                {
                    string cmdName = args[0].Substring(1);
                    ICommandInfo[] commandHandlers = CommandModule.CommandHandlers
                        .Select(pair => pair.Handler)
                        .Where(handler => { return handler.GetTriggerNames().Any(name => name.StartsWith(cmdName)); }).ToArray();
                    if (commandHandlers.Length == 1)
                    {
                        string fullName = commandHandlers[0].GetTriggerNames().First(name => name.StartsWith(cmdName));
                        newText = $"{CommandModule.CommandPrefix}{fullName}";
                        pressedTab = true;
                    }
                }
            }

            if (!pressedUpOrDown && !pressedTab) return;

            if (CurrentHistoryIndex >= 0 && CurrentHistoryIndex < History.Count && pressedUpOrDown)
            {
                newText = History[CurrentHistoryIndex];
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
        // ReSharper disable once InconsistentNaming
        public static void OnSendMessage(ChatWindow __instance, ref bool commit)
        {
            if (commit)
            {
                PugText text = __instance.inputField;
                string input = text.displayedTextString;

                if (CommandModule.SendCommand(input)) return;

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
        // ReSharper disable once InconsistentNaming
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
            History.Add(input);
            if (History.Count > MaxHistoryLen)
            {
                History.RemoveAt(0);
            }

            CurrentHistoryIndex = History.Count;
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