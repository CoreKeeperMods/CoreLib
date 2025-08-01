using System.Linq;
using CoreLib.Commands.Communication;
using PugMod;
using Unity.Entities;

namespace CoreLib.Commands.Handlers
{
    public class DirectMessageCommandHandler : IServerCommandHandler
    {
        public CommandOutput Execute(string[] parameters, Entity sender)
        {
            if (parameters.Length < 2)
            {
                return new CommandOutput("Not enough arguments!", CommandStatus.Error);
            }
            
            var playerEntity = sender.GetPlayerEntity();
            if (playerEntity != Entity.Null)
            {
                string fromPlayer = playerEntity.GetPlayerName();

                var targetPlayerName = parameters[0];
                var targetPlayer = Manager.main.allPlayers.FirstOrDefault(pc => pc.playerName.Equals(targetPlayerName));
                if (targetPlayer == null)
                {
                    return new CommandOutput($"Player {targetPlayerName} not found!", CommandStatus.Error);
                }

                var entityManager = API.Server.World.EntityManager;
                Entity targetConnection = entityManager.GetComponentData<PlayerGhost>(targetPlayer.entity).connection;
                string message = string.Join(" ", parameters.Skip(1));
                CommandsModule.ServerCommSystem.SendChatMessage($"[{fromPlayer} whispers]: {message}", targetConnection);
                return "Message Sent";
            }

            return new CommandOutput("Sender info is invalid!", CommandStatus.Error);
        }

        public string GetDescription()
        {
            return "/whisper {player name} {message} - Send message directly to player";
        }

        public string[] GetTriggerNames()
        {
            return new[] { "dm", "whisper" };
        }
    }
}