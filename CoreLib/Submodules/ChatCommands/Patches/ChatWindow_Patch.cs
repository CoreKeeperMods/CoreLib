using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace CoreLib.Submodules.ChatCommands.Patches;

[HarmonyPatch]
internal class ChatWindow_Patch
{
    private const string CommandPrefix = "/";

    private static List<string> history = new List<string>();

    public static int currentHistoryIndex = -1;
    public static int maxHistoryLen = 10;

    public static ChatWindow chat;

    [HarmonyPatch(typeof(ChatWindow), nameof(ChatWindow.Update))]
    [HarmonyPrefix]
    public static void OnUpdate(ChatWindow __instance)
    {
        if (chat == null)
        {
            chat = __instance;
        }

        if (history.Count <= 0) return;

        bool pressedUpOrDown = false;
        bool pressedTab = false;
        string newText = "";

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentHistoryIndex--;
            if (currentHistoryIndex < 0)
            {
                currentHistoryIndex = history.Count;
            }

            pressedUpOrDown = true;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentHistoryIndex++;
            if (currentHistoryIndex > history.Count)
            {
                currentHistoryIndex = 0;
            }

            pressedUpOrDown = true;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
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

    [HarmonyPatch(typeof(ChatWindow), nameof(ChatWindow.AddPugText))]
    [HarmonyPrefix]
    static bool ReadPugText(ref ChatWindow.MessageTextType type, ref PugText text)
    {
        string input = text.textString;
        string[] args = input.Split(' ');
        if (args.Length < 1 || !args[0].StartsWith(CommandPrefix)) return true;
        try
        {
            string cmdName = args[0].Substring(1);
            IChatCommandHandler commandHandler = CommandsModule.commandHandlers
                .Select(pair => pair.handler)
                .First(handler => handler.GetTriggerNames().Contains(cmdName));
            string[] parameters = args.Skip(1).ToArray();

            try
            {
                CommandOutput output = commandHandler.Execute(parameters);

                history.Add(input);
                if (history.Count > maxHistoryLen)
                {
                    history.RemoveAt(0);
                }

                currentHistoryIndex = history.Count;

                text.textString = $"{input}\n{output.feedback}";
                text.defaultStyle.color = output.color;
                text.Render();
                return true;
            }
            catch (Exception e)
            {
                CoreLibPlugin.Logger.LogWarning($"Error executing command {cmdName}:\n{e}");

                text.textString = $"{input}\nError executing command";
                text.defaultStyle.color = Color.red;
                text.Render();
                return true;
            }
        }
        catch
        {
            text.textString = $"{input}\n\nThat command does not exist.";
            text.defaultStyle.color = Color.red;
            text.Render();
            return true;
        }
    }
}