using CoreLib.Submodules.ModComponent;
using HarmonyLib;
using InventoryHandlerSystem;
using PlayerCommand;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Command = InventoryHandlerSystem.Command;

namespace CoreLib.Submodules.Security.Patches
{
    public class InventoryHandlerServerSystem_Patch
    {

        [HarmonyPatch(typeof(Server), nameof(Server.OnUpdate))]
        [HarmonyPrefix]
        public static void OnUpdate(Server __instance)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            NativeArray<Entity> nativeArray = __instance.Server_LambdaJob_1_Query.ToEntityArray(Allocator.Temp);
            ComponentDataFromEntity<ConnectionAdminLevelCD> componentDataFromEntity = __instance.GetComponentDataFromEntity<ConnectionAdminLevelCD>(true);

            for (int i = 0; i < nativeArray.Length; i++)
            {
                ServerCommandRpc rpc = __instance.EntityManager.GetModComponentData<ServerCommandRpc>(nativeArray[i]);
                ServerCommand command = rpc.Value;
                
                if (command.command == Command.SetName ||
                    command.command == Command.Move ||
                    command.command == Command.Swap ||
                    command.command == Command.Sort ||
                    command.command == Command.QuickStack ||
                    command.command == Command.MoveInventory ||
                    command.command == Command.MoveOrDrop ||
                    command.command == Command.PickUpObject ||
                    command.command == Command.MoveAllAtOrDrop ||
                    command.command == Command.MoveAllOrDrop) continue;

                string additionalData = command.string1.Value;
                Entity connection = __instance.GetComponent<ReceiveRpcCommandRequestComponent>(nativeArray[i]).SourceConnection;
                SecurityModule.CheckCommandPermission(nativeArray[i], connection, additionalData, ecb, componentDataFromEntity);
            }
            
            ecb.Playback(__instance.EntityManager);
            ecb.Dispose();
        }
    }
}