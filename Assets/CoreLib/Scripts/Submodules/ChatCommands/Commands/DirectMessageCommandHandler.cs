using System.Linq;
using CoreLib.Submodules.ChatCommands.Communication;
using PugMod;
using Unity.Entities;
using Unity.NetCode;

namespace CoreLib.Submodules.ChatCommands
{
    public class DirectMessageCommandHandler : IChatCommandHandler
    {
        public CommandOutput Execute(string[] parameters, Entity sender)
        {
            if (parameters.Length < 2)
            {
                return new CommandOutput("Not enough arguments!", CommandStatus.Error);
            }
            
            var entityManager = API.Server.World.EntityManager;
            var playerEntity = entityManager.GetComponentData<CommandTargetComponent>(sender).targetEntity;
            if (playerEntity != Entity.Null)
            {
                PlayerCustomization customization = entityManager.GetComponentData<PlayerCustomizationCD>(playerEntity).customization;
                string fromPlayer = customization.name.Value;
                CoreLibMod.Log.LogDebug($"Message from {fromPlayer}");

                var targetPlayerName = parameters[0];
                var targetPlayer = Manager.main.allPlayers.FirstOrDefault(pc => pc.playerName.Equals(targetPlayerName));
                if (targetPlayer == null)
                {
                    return new CommandOutput($"Player {targetPlayerName} not found!", CommandStatus.Error);
                }

                Entity targetConnection = entityManager.GetComponentData<PlayerGhost>(targetPlayer.entity).connection;
                string message = string.Join(" ", parameters.Skip(1));
                CommandsModule.commSystem.SendChatMessage($"[{fromPlayer} whispers]: {message}", targetConnection);
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