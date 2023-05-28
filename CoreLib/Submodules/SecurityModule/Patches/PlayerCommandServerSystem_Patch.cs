using System;
using System.Collections.Generic;
using System.Text;
using CoreLib.Submodules.ChatCommands;
using HarmonyLib;
using PlayerCommand;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace CoreLib.Submodules.Security.Patches
{
    public static class PlayerCommandServerSystem_Patch
    {
        [HarmonyPatch(typeof(ServerSystem), nameof(ServerSystem.OnUpdate))]
        [HarmonyPrefix]
        public static void OnUpdate(ServerSystem __instance)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            NativeArray<Entity> nativeArray = __instance._rpcQuery.ToEntityArray(Allocator.Temp);
            ComponentDataFromEntity<ConnectionAdminLevelCD> componentDataFromEntity = __instance.GetComponentDataFromEntity<ConnectionAdminLevelCD>(true);

            for (int i = 0; i < nativeArray.Length; i++)
            {
                Rpc rpc = __instance.GetComponent<Rpc>(nativeArray[i]);
                
                if (rpc.command == Command.SetName ||
                    rpc.command == Command.MapPing ||
                    rpc.command == Command.BanPlayer) continue;

                string additionalData = rpc.text.Value;
                Entity connection = __instance.GetComponent<ReceiveRpcCommandRequestComponent>(nativeArray[i]).SourceConnection;
                SecurityModule.CheckCommandPermission(nativeArray[i], connection, additionalData, ecb, componentDataFromEntity);
            }
            
            ecb.Playback(__instance.EntityManager);
            ecb.Dispose();
        }
    }
}