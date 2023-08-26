using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Entities;

namespace CoreLib.Submodules.ChatCommands.Communication
{
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

        public void SendCommand(string message)
        {
            if (isServer)
            {
                CoreLibMod.Log.LogWarning("Server cannot issue commands!");  
                return;
            }
            
            SendMessage(message, CommandMessageType.Command, CommandStatus.None);
        }
        
        public void SendChatMessage(string message, Entity targetConnection = default)
        {
            if (!isServer)
            {
                CoreLibMod.Log.LogWarning("Client cannot send messages!");  
                return;
            }
            
            SendMessage(message, CommandMessageType.ChatMessage, CommandStatus.None, targetConnection);
        }
        
        public void SendResponse(string message, CommandStatus status, Entity targetConnection = default)
        {
            if (!isServer)
            {
                CoreLibMod.Log.LogWarning("Client cannot send messages!");  
                return;
            }
            
            SendMessage(message, CommandMessageType.Response, status, targetConnection);
        }
        
        private unsafe void SendMessage(string message, CommandMessageType messageType, CommandStatus status, Entity targetConnection = default)
        {
            messageCount++;
            
            byte[] commandBytes = Encoding.UTF8.GetBytes(message);
            int bytesLength = commandBytes.Length;
            
            int entityCount = (bytesLength - 1) / sizeof(FixedArray64) + 1;
            
            SendRpcCommandRequestComponent rpcComponent = new SendRpcCommandRequestComponent
            {
                TargetConnection = targetConnection
            };
            
            CommandMessageRPC commandMessage = new CommandMessageRPC
            {
                messageNumber = messageCount,
                messageType = messageType,
                status = status,
                totalSize = bytesLength
            };

            CommandDataMessageRPC messagePart = new CommandDataMessageRPC
            {
                messageNumber = commandMessage.messageNumber
            };

            Entity entity = EntityManager.CreateEntity(messageRpcArchetype);
            EntityManager.SetComponentData(entity, commandMessage);
            EntityManager.SetComponentData(entity, rpcComponent);

            using NativeArray<Entity> partEntities = EntityManager.CreateEntity(messageDataRpcArchetype, entityCount, Allocator.Temp);
            int bytesRemain = bytesLength;

            fixed (byte* ptr = commandBytes)
            {
                for (int i = 0; i < partEntities.Length; i++)
                {
                    messagePart.startByte = i * messagePart.messagePart.Size;
                    UnsafeUtility.MemCpy(messagePart.messagePart.GetUnsafePtr(), ptr + messagePart.startByte,
                        math.min(messagePart.messagePart.Size, bytesRemain));
                    EntityManager.SetComponentData(partEntities[i], messagePart);
                    EntityManager.SetComponentData(partEntities[i], rpcComponent);
                    bytesLength -= messagePart.messagePart.Size;
                }
            }
        }
        
        protected override void OnCreate()
        {
            AllowToRunBeforeInit();
            messageRpcArchetype = EntityManager.CreateArchetype(typeof(CommandMessageRPC), typeof(SendRpcCommandRequestComponent));
            messageDataRpcArchetype = EntityManager.CreateArchetype(typeof(CommandDataMessageRPC), typeof(SendRpcCommandRequestComponent));
            base.OnCreate();
        }

        protected override unsafe void OnUpdate()
        {
            Entities.ForEach((in CommandMessageRPC rpc, in ReceiveRpcCommandRequestComponent req) =>
            {
                if (partialMessages.ContainsKey(rpc.messageNumber))
                {
                    CoreLibMod.Log.LogWarning("Got message with same number twice");
                    return;
                }

                partialMessages.Add(rpc.messageNumber, new CommandMessage
                {
                    sender = req.SourceConnection,
                    messageType = rpc.messageType,
                    status = rpc.status
                });
                partialMessagesData.Add(rpc.messageNumber, new byte[rpc.totalSize]);
                
            }).WithoutBurst().Run();
            
            Entities.ForEach((in CommandDataMessageRPC rpc) =>
            {
                if (!partialMessagesData.ContainsKey(rpc.messageNumber))
                {
                    CoreLibMod.Log.LogWarning("Got data message without meta message");
                    return;
                }
                
                byte[] bytes = partialMessagesData[rpc.messageNumber];
                fixed (byte* ptr = bytes)
                {
                    void* destination = ptr + rpc.startByte;
                    FixedArray64 messagePart = rpc.messagePart;

                    UnsafeUtility.MemCpy(destination, messagePart.GetUnsafePtr(), math.min(messagePart.Size, bytes.Length - rpc.startByte));
                }
                

                int startByte = rpc.startByte;
                if (startByte + sizeof(FixedArray64) < bytes.Length)
                {
                    return;
                }
                var message = partialMessages[rpc.messageNumber];
                message.message = Encoding.UTF8.GetString(partialMessagesData[rpc.messageNumber]);

                receivedMessageQueue.Enqueue(message);
                
                if (receivedMessageQueue.Count > maxReceivedMessages)
                {
                    receivedMessageQueue.Dequeue();
                }
                partialMessagesData.Remove(rpc.messageNumber);
                partialMessages.Remove(rpc.messageNumber);
            }).WithAll<ReceiveRpcCommandRequestComponent>()
                .WithoutBurst()
                .Run();

            if (isServer)
            {
                ServerHandleMessages();
            }
            
            base.OnUpdate();
        }

        private void ServerHandleMessages()
        {
            while (TryGetNextMessage(out CommandMessage message))
            {
                CommandsModule.HandleCommand(message);
            }
        }
    }
}