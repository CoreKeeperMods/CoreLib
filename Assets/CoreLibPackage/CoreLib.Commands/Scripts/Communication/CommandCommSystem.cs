using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using CoreLib.Util.Extensions;
using Unity.Collections;
using Unity.NetCode;
using Unity.Entities;

namespace CoreLib.Commands.Communication
{
    [UpdateInWorld(TargetWorld.ClientAndServer)]
    public partial class CommandCommSystem : PugSimulationSystemBase
    {
        private const int maxReceivedMessages = 10;

        private int messageCount;
        private EntityArchetype messageRpcArchetype;
        private EntityArchetype messageDataRpcArchetype;

        private Dictionary<int, CommandMessage> partialMessages = new Dictionary<int, CommandMessage>();
        private Dictionary<int, byte[]> partialMessagesData = new Dictionary<int, byte[]>();

        private Queue<CommandMessage> receivedMessageQueue = new Queue<CommandMessage>();

        internal bool TryGetNextMessage(out CommandMessage message)
        {
            return receivedMessageQueue.TryDequeue(out message);
        }
        
        internal void AppendMessage(CommandMessage message)
        {
            receivedMessageQueue.Enqueue(message);

            if (receivedMessageQueue.Count > maxReceivedMessages)
            {
                receivedMessageQueue.Dequeue();
            }
        }

        public void SendCommand(string message, CommandFlags commandFlags)
        {
            if (isServer)
            {
                CoreLibMod.Log.LogWarning("Server cannot issue commands!");
                return;
            }

            SendMessage(message, CommandMessageType.Command, CommandStatus.None, commandFlags);
        }
        
        public void SendRelayCommand(string message)
        {
            if (!isServer)
            {
                CoreLibMod.Log.LogWarning("Client cannot send relay commands!");
                return;
            }

            SendMessage(message, CommandMessageType.RelayCommand, CommandStatus.None);
        }

        public void SendChatMessage(string message, Entity targetConnection = default)
        {
            if (!isServer)
            {
                CoreLibMod.Log.LogWarning("Client cannot send messages!");
                return;
            }

            SendMessage(message, CommandMessageType.ChatMessage, CommandStatus.None, CommandFlags.None, targetConnection);
        }

        public void SendResponse(string message, CommandStatus status, CommandFlags commandFlags = CommandFlags.None, Entity targetConnection = default)
        {
            if (!isServer)
            {
                CoreLibMod.Log.LogWarning("Client cannot send messages!");
                return;
            }

            SendMessage(message, CommandMessageType.Response, status, commandFlags, targetConnection);
        }

        private void SendMessage(
            string message, 
            CommandMessageType messageType, 
            CommandStatus status, 
            CommandFlags commandFlags = CommandFlags.None,
            Entity targetConnection = default)
        {
            messageCount++;

            byte[] commandBytes = Encoding.UTF8.GetBytes(message);
            int bytesLength = commandBytes.Length;

            int entityCount = (bytesLength - 1) / 64 + 1;

            SendRpcCommandRequestComponent rpcComponent = new SendRpcCommandRequestComponent
            {
                TargetConnection = targetConnection
            };

            CommandMessageRPC commandMessage = new CommandMessageRPC
            {
                messageNumber = messageCount,
                messageType = messageType,
                status = status,
                totalSize = bytesLength,
                commandFlags = commandFlags
            };

            CommandDataMessageRPC messagePart = new CommandDataMessageRPC
            {
                messageNumber = commandMessage.messageNumber
            };

            Entity entity = EntityManager.CreateEntity(messageRpcArchetype);
            EntityManager.SetComponentData(entity, commandMessage);
            EntityManager.SetComponentData(entity, rpcComponent);

            using NativeArray<Entity> partEntities = EntityManager.CreateEntity(messageDataRpcArchetype, entityCount, Allocator.Temp);

            for (int i = 0; i < partEntities.Length; i++)
            {
                messagePart.startByte = i * messagePart.messagePart.Size;
                messagePart.messagePart.CopyFrom(commandBytes, messagePart.startByte);

                EntityManager.SetComponentData(partEntities[i], messagePart);
                EntityManager.SetComponentData(partEntities[i], rpcComponent);
            }
        }

        protected override void OnCreate()
        {
            AllowToRunBeforeInit();
            messageRpcArchetype = EntityManager.CreateArchetype(typeof(CommandMessageRPC), typeof(SendRpcCommandRequestComponent));
            messageDataRpcArchetype = EntityManager.CreateArchetype(typeof(CommandDataMessageRPC), typeof(SendRpcCommandRequestComponent));
            base.OnCreate();
        }

        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
        protected override void OnUpdate()
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            Entities.ForEach((Entity entity, in CommandMessageRPC rpc, in ReceiveRpcCommandRequestComponent req) =>
                {
                    if (partialMessages.ContainsKey(rpc.messageNumber))
                    {
                        CoreLibMod.Log.LogWarning("Got message with same number twice");
                        ecb.DestroyEntity(entity);
                        return;
                    }

                    partialMessages.Add(rpc.messageNumber, new CommandMessage
                    {
                        sender = req.SourceConnection,
                        messageType = rpc.messageType,
                        status = rpc.status,
                        commandFlags = rpc.commandFlags
                    });
                    partialMessagesData.Add(rpc.messageNumber, new byte[rpc.totalSize]);
                    ecb.DestroyEntity(entity);
                }).WithoutBurst()
                .Run();

            Entities.ForEach((Entity entity, in CommandDataMessageRPC rpc) =>
                {
                    if (!partialMessagesData.ContainsKey(rpc.messageNumber))
                    {
                        CoreLibMod.Log.LogWarning("Got data message without meta message");
                        ecb.DestroyEntity(entity);
                        return;
                    }

                    byte[] bytes = partialMessagesData[rpc.messageNumber];
                    FixedArray64 part = rpc.messagePart;

                    part.CopyTo(bytes, rpc.startByte);

                    int startByte = rpc.startByte;
                    if (startByte + part.Size < bytes.Length)
                    {
                        ecb.DestroyEntity(entity);
                        return;
                    }

                    var message = partialMessages[rpc.messageNumber];
                    message.message = Encoding.UTF8.GetString(partialMessagesData[rpc.messageNumber]);

                    AppendMessage(message);

                    partialMessagesData.Remove(rpc.messageNumber);
                    partialMessages.Remove(rpc.messageNumber);
                    ecb.DestroyEntity(entity);
                }).WithAll<ReceiveRpcCommandRequestComponent>()
                .WithoutBurst()
                .Run();

            ecb.Playback(EntityManager);
            ecb.Dispose();

            if (isServer)
            {
                ServerHandleMessages();
            }
            else
            {
                ClientHandleMessages();
            }

            base.OnUpdate();
        }

        private void ServerHandleMessages()
        {
            while (TryGetNextMessage(out CommandMessage message))
            {
                CommandsModule.ServerHandleCommand(message);
            }
        }
        
        private void ClientHandleMessages()
        {
            var relayCommands = receivedMessageQueue.Where(message => message.messageType == CommandMessageType.RelayCommand);
            foreach (CommandMessage message in relayCommands)
            {
                CommandsModule.ClientHandleCommand(message);
            }
        }
    }
}