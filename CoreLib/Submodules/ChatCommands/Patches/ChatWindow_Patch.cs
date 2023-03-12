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

    private static CharacterCustomizationMenu customizationMenu;
    public static ChatWindow chat;

    [HarmonyPatch(typeof(RadicalMenu), nameof(RadicalMenu.Awake))]
    [HarmonyPostfix]
    public static void OnCreateCustomizationWindow(RadicalMenu __instance)
    {
        if (__instance.GetIl2CppType() != Il2CppType.Of<CharacterCustomizationMenu>()) return;
        
        customizationMenu = __instance.Cast<CharacterCustomizationMenu>();

        if (chat != null && customizationMenu != null)
            AddTextInputLogic();
    }
    
    [HarmonyPatch(typeof(ChatWindow), nameof(ChatWindow.Awake))]
    [HarmonyPostfix]
    public static void OnCreate(ChatWindow __instance)
    {
        chat = __instance;
        
        if (chat != null && customizationMenu != null)
            AddTextInputLogic();
    }

    private static void AddTextInputLogic()
    {
     /*   CoreLibPlugin.Logger.LogInfo("Modifying chat input field!");
        CharacterCustomizationOption_NameInput nameInput = customizationMenu.nameInput;

        var inputField = chat.inputField;
        RadicalMenuOptionTextInput textInput = inputField.gameObject.AddComponent<RadicalMenuOptionTextInput>();
        textInput.maxWidth = 5.4f;
        textInput.pugText = chat.inputField;
        textInput.dontAllowNewLines = true;
        
        //hint text must not be null

        var blinkerGO = Object.Instantiate(nameInput.characterMarkBlinker.gameObject, textInput.transform);
        textInput.characterMarkBlinker = blinkerGO.GetComponent<CharacterMarkBlinker>();
        textInput.selectedMarker = Object.Instantiate(nameInput.selectedMarker, textInput.transform);
  */  }

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
            
            if (!CommandsModule.GetCommandHandler(cmdName, out IChatCommandHandler commandHandler))
            {
                SendMessage(__instance, $"{input}\n\nThat command does not exist.", Color.red);
                commit = false;
            }

            string[] parameters = args.Skip(1).ToArray();

            try
            {
                CommandOutput output = commandHandler.Execute(parameters);

                UpdateHistory(input);
                SendMessage(__instance, $"{input}\n{output.feedback}", output.color);
                commit = false;
            }
            catch (Exception e)
            {
                CoreLibPlugin.Logger.LogWarning($"Error executing command {cmdName}:\n{e}");

                SendMessage(__instance, $"{input}\nError executing command", Color.red);
                commit = false;
            }
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