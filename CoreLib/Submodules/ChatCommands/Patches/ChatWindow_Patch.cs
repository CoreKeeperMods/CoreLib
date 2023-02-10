using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CoreLib.Util;
using HarmonyLib;
using Il2CppInterop.Common;
using Il2CppInterop.Runtime;
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
            
            try
            {
                string cmdName = args[0].Substring(1);
                IChatCommandHandler commandHandler = CommandsModule.commandHandlers
                    .Select(pair => pair.handler)
                    .First(handler => handler.GetTriggerNames().Any(s => s.Equals(cmdName, StringComparison.InvariantCultureIgnoreCase)));
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
            catch
            {
                SendMessage(__instance, $"{input}\n\nThat command does not exist.", Color.red);
                commit = false;
            }
            
        }
    }

    public static void SendMessage(ChatWindow window, string message, Color color)
    {
        PugText pugText = AllocPugText(window, ChatWindow.MessageTextType.Sent, out PugTextEffectMaxFade fadeEffect);
        pugText.Render(message);
        pugText.defaultStyle.color = color;
        if (fadeEffect != null)
        {
            fadeEffect.FadeOut();
            window.AddPugText(ChatWindow.MessageTextType.Sent, pugText);
        }
    }

    private static readonly IntPtr AllocPugTextMethodPtr;

    static ChatWindow_Patch()
    {
        AllocPugTextMethodPtr = (IntPtr)Il2CppInteropUtils
            .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(ChatWindow).GetMethod("AllocPugText")).GetValue(null);
    }
    
    public static unsafe PugText AllocPugText(
        ChatWindow window,
        ChatWindow.MessageTextType type,
        out PugTextEffectMaxFade fadeEffect)
    {
        IL2CPP.Il2CppObjectBaseToPtrNotNull(window);
        IntPtr* numPtr1 = stackalloc IntPtr[2];
        numPtr1[0] = (IntPtr)Unsafe.AsPointer(ref type);

        IntPtr refPtr = IntPtr.Zero;
        IntPtr* numPtr2 = &refPtr;
        numPtr1[1] = (IntPtr) numPtr2;
        
        IntPtr exc = IntPtr.Zero;
        IntPtr outPtr = IL2CPP.il2cpp_runtime_invoke(AllocPugTextMethodPtr, IL2CPP.Il2CppObjectBaseToPtrNotNull(window), (void**) numPtr1, ref exc);
        Il2CppException.RaiseExceptionIfNecessary(exc);
        
        fadeEffect = refPtr == IntPtr.Zero ? null : new PugTextEffectMaxFade(refPtr);
        
        return outPtr == IntPtr.Zero ? null : new PugText(outPtr);
    }
}