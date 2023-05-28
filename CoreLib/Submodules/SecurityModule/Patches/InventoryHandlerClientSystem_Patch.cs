using CoreLib.Submodules.ChatCommands;
using HarmonyLib;
using InventoryHandlerSystem;
using PlayerCommand;
using Command = InventoryHandlerSystem.Command;

namespace CoreLib.Submodules.Security.Patches
{
    public class InventoryHandlerClientSystem_Patch
    {

        [HarmonyPatch(typeof(Client), nameof(Client.Send), typeof(ServerCommand), typeof(Il2CppSystem.Action<bool>))]
        [HarmonyPrefix]
        public static void OnCommandSend(ref ServerCommand command)
        {
            if (command.command == Command.SetName ||
                command.command == Command.Move ||
                command.command == Command.Swap ||
                command.command == Command.Sort ||
                command.command == Command.QuickStack ||
                command.command == Command.MoveInventory ||
                command.command == Command.MoveOrDrop ||
                command.command == Command.PickUpObject ||
                command.command == Command.MoveAllAtOrDrop ||
                command.command == Command.MoveAllOrDrop) 
                return;
            
            if (!CommandsModule.Loaded) return;
            if (CommandsModule.currentCommandInfo == null) return;

            command.string1 = CommandsModule.currentCommandInfo.ToString();
        }
    }
}