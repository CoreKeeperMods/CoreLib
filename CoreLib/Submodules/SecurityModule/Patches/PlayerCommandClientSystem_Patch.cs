using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using CoreLib.Submodules.ChatCommands;
using CoreLib.Submodules.ModComponent;
using HarmonyLib;
using PlayerCommand;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace CoreLib.Submodules.Security.Patches
{
    public static class PlayerCommandClientSystem_Patch
    {
        private static readonly string[] skiplist = {
            "LambdaJob",
            "OnCreate",
            "OnCreateForCompiler",
            "OnDestroy",
            "OnUpdate",
            "UpdatePlayerCustomization"
        };
        
        static IEnumerable<MethodBase> TargetMethods()
        {
            return typeof(ClientSystem).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(info =>
            {
                return !info.IsSpecialName && skiplist.All(skip => !info.Name.Contains(skip));
            });
        }
        
        public static unsafe ref Rpc PeekRef(NativeQueue<Rpc> queue)
        {
            NativeQueueBlockHeader* ptr = (NativeQueueBlockHeader*)(void*)queue.m_Buffer->m_FirstBlock;
            return ref ModUnsafe.ArrayElementAsRef<Rpc>(ptr + 1, queue.m_Buffer->m_CurrentRead);
        }
        
        [HarmonyPostfix]
        public static void OnCommandEnqueue(ClientSystem __instance)
        {
            ref Rpc lastCommand = ref PeekRef(__instance.rpcQueue);
            if (lastCommand.command == Command.SetName ||
                lastCommand.command == Command.MapPing ||
                lastCommand.command == Command.BanPlayer)
                return;
            
            if (!CommandsModule.Loaded) return;
            if (CommandsModule.currentCommandInfo == null) return;

            lastCommand.text = CommandsModule.currentCommandInfo.ToString();
        }
    }
}