using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CoreLib.Util;
using HarmonyLib;
using Il2CppInterop.Common;
using Il2CppInterop.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreLib.Submodules.ChatCommands.Patches;

[HarmonyPatch]
internal class ChatWindow_Patch
{
    private const string CommandPrefix = "/";

    private static List<string> history = new List<string>();

    public static int currentHistoryIndex = -1;
    public static int maxHistoryLen = 10;

    [HarmonyPatch(typeof(ChatWindow), nameof(ChatWindow.Update))]
    [HarmonyPrefix]
    public static void OnUpdate(ChatWindow __instance)
    {
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
            if (args[0].StartsWith(CommandPrefix))
            {
                string cmdName = args[0].Substring(1);
                IChatCommandHandler[] commandHandlers = CommandsModule.commandHandlers
                    .Select(pair => pair.handler)
                    .Where(handler => { return handler.GetTriggerNames().Any(name => name.StartsWith(cmdName)); }).ToArray();
                if (commandHandlers.Length == 1)
                {
                    string fullName = commandHandlers[0].GetTriggerNames().First(name => name.StartsWith(cmdName));
                    newText = $"{CommandPrefix}{fullName}";
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
            if (args.Length < 1 || !args[0].StartsWith(CommandPrefix)) return;
            
            string cmdName = args[0].Substring(1);
            
            if (!CommandsModule.GetCommandHandler(cmdName, out CommandPair commandPair))
            {
                SendMessage(__instance, $"{input}\n\nThat command does not exist.", Color.red);
                commit = false;
            }

            string[] parameters = args.Skip(1).ToArray();

            try
            {
                CommandKind kind = CommandsModule.DetermineCommandKind(commandPair);

                CommandsModule.currentCommandInfo = new CommandInfo(commandPair.modName, commandPair.handler.GetType().Name, kind);
                
                CommandOutput output = commandPair.handler.Execute(parameters);

                UpdateHistory(input);
                SendMessage(__instance, $"{input}\n{output.feedback}", output.color);
                if (output.color == Color.red && CommandsModule.remindAboutHelpCommand.Value)
                {
                    SendMessage(__instance, $"Use /help {cmdName} to learn command usage!", Color.blue);
                }
                commit = false;
            }
            catch (Exception e)
            {
                CoreLibPlugin.Logger.LogWarning($"Error executing command {cmdName}:\n{e}");

                SendMessage(__instance, $"{input}\nError executing command", Color.red);
                commit = false;
            }

            CommandsModule.currentCommandInfo = null;
        }
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

    public static void SendMessage(ChatWindow window, string message, Color color)
    {
        PugText pugText = window.AllocPugText(ChatWindow.MessageTextType.Sent, out PugTextEffectMaxFade fadeEffect);
        pugText.Render(message);
        pugText.style.color = color;
        pugText.defaultStyle.color = color;
        pugText.color = color;
        if (fadeEffect != null)
        {
            fadeEffect.FadeOut();
            window.AddPugText(ChatWindow.MessageTextType.Sent, pugText);
        }
    }
}