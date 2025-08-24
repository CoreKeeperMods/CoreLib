using System.Linq;
using CoreLib.Submodule.Command.Communication;
using PugMod;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Command.Handlers
{
    /// <summary>
    /// Handles the execution of the direct message command, allowing players
    /// to send private messages to specified recipients within the game.
    /// </summary>
    public class DirectMessageCommandHandler : IServerCommandHandler
    {
        /// <summary>
        /// Executes the direct message command. This command sends a private message
        /// from the sender to a specified target player in the game.
        /// </summary>
        /// <param name="parameters">
        /// An array of strings where the first element represents the target player's name,
        /// and the remaining elements form the message to send.
        /// </param>
        /// <param name="sender">
        /// The entity representing the sender of the message.
        /// </param>
        /// <returns>
        /// A CommandOutput object that indicates the status and result of the command execution.
        /// The status may reflect success or specific error messages if the command fails.
        /// </returns>
        public CommandOutput Execute(string[] parameters, Unity.Entities.Entity sender)
        {
            if (parameters.Length < 2)
            {
                return new CommandOutput("Not enough arguments!", CommandStatus.Error);
            }
            
            var playerEntity = sender.GetPlayerEntity();
            if (playerEntity != Unity.Entities.Entity.Null)
            {
                string fromPlayer = playerEntity.GetPlayerName();

                var targetPlayerName = parameters[0];
                var targetPlayer = Manager.main.allPlayers.FirstOrDefault(pc => pc.playerName.Equals(targetPlayerName));
                if (targetPlayer == null)
                {
                    return new CommandOutput($"Player {targetPlayerName} not found!", CommandStatus.Error);
                }

                var entityManager = API.Server.World.EntityManager;
                Unity.Entities.Entity targetConnection = entityManager.GetComponentData<PlayerGhost>(targetPlayer.entity).connection;
                string message = string.Join(" ", parameters.Skip(1));
                CommandsModule.ServerCommSystem.SendChatMessage($"[{fromPlayer} whispers]: {message}", targetConnection);
                return "Message Sent";
            }

            return new CommandOutput("Sender info is invalid!", CommandStatus.Error);
        }

        /// <summary>
        /// Retrieves the description of the command.
        /// This description provides information about the purpose and usage of the command.
        /// </summary>
        /// <returns>
        /// A string that contains the description of the command.
        /// </returns>
        public string GetDescription()
        {
            return "/whisper {player name} {message} - Send message directly to player";
        }

        /// <summary>
        /// Retrieves the trigger names associated with the command.
        /// These trigger names are used to invoke the command.
        /// </summary>
        /// <returns>
        /// An array of strings representing the trigger names for the command.
        /// </returns>
        public string[] GetTriggerNames()
        {
            return new[] { "dm", "whisper" };
        }
    }
}