using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using CoreLib.Submodule.Command.Data;
using Unity.Collections;
using Unity.NetCode;
using Unity.Entities;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Command.System
{
    /// <summary>
    /// Represents a communication system designed for managing command messaging in both server and client simulations.
    /// This system handles the transmission, reception, and processing of command-related messages, including chat and relay commands.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class CommandCommSystem : PugSimulationSystemBase
    {
        /// <summary>
        /// The maximum number of messages that can be stored in the received message queue at a given time.
        /// </summary>
        /// <remarks>
        /// Once the number of messages in the queue exceeds this value, the oldest messages will be discarded
        /// to maintain the limit. Used to prevent the queue from growing indefinitely, ensuring efficient system
        /// operation and avoiding potential memory or performance issues.
        /// </remarks>
        private const int MaxReceivedMessages = 10;

        /// <summary>
        /// A private variable used to keep track of the total number of messages handled by the system.
        /// This counter is incremented each time a new message is sent using the <c>SendMessage</c> method.
        /// </summary>
        private int _messageCount;

        /// <summary>
        /// Represents the entity archetype used for creating and managing message RPC entities
        /// within the command communication system.
        /// </summary>
        /// <remarks>
        /// This archetype is utilized internally in the system for sending messages with
        /// specific metadata such as the message type, status, size, and flags. It is
        /// essential for the construction of entities required for the RPC communication process.
        /// </remarks>
        private EntityArchetype _messageRpcArchetype;

        /// <summary>
        /// Represents the entity archetype used for creating message data entities in the RPC communication system.
        /// </summary>
        /// <remarks>
        /// This archetype defines the structural layout for entities that store segments of command messages to be sent via RPC.
        /// Each entity created using this archetype contains the necessary components for representing a chunk of message data.
        /// It is utilized within the <c>SendMessage</c> method of the communication system.
        /// </remarks>
        private EntityArchetype _messageDataRpcArchetype;

        /// <summary>
        /// A dictionary that stores partially received command messages, keyed by their unique message number.
        /// It is used to aggregate fragmented message data for reassembly and processing in the command communication system.
        /// </summary>
        private readonly Dictionary<int, CommandMessage> _partialMessages = new Dictionary<int, CommandMessage>();

        /// <summary>
        /// A dictionary that temporarily stores partial message data during a multipart message transfer operation.
        /// </summary>
        /// <remarks>
        /// The keys represent unique identifiers for messages (e.g., message numbers),
        /// and the values contain byte arrays that hold the partial data of the corresponding messages.
        /// This structure is essential for reconstructing and assembling multipart network messages received by the system.
        /// After a message is fully assembled and processed, it is removed from this dictionary to free up resources.
        /// </remarks>
        private readonly Dictionary<int, byte[]> _partialMessagesData = new Dictionary<int, byte[]>();

        /// <summary>
        /// A queue used to store received command messages within the communication system.
        /// This queue serves as a temporary holding system for incoming messages that need to be
        /// processed by the <c>CommandCommSystem</c>. Each message represents a command or communication between
        /// connected entities in the simulation environment.
        /// </summary>
        private readonly Queue<CommandMessage> _receivedMessageQueue = new Queue<CommandMessage>();

        /// <summary>
        /// Attempts to retrieve the next command message from the internal message queue.
        /// </summary>
        /// <param name="message">When this method returns, contains the next <see cref="CommandMessage"/> if one is available; otherwise, the default value of <see cref="CommandMessage"/>.</param>
        /// <returns>
        /// <c>true</c> if a message was successfully retrieved from the queue; otherwise, <c>false</c> if the queue is empty.
        /// </returns>
        /// <remarks>
        /// This method is used to process messages stored in the internal message queue. If the queue contains any messages,
        /// it dequeues and provides the next message for further processing; otherwise, it indicates that the queue is empty.
        /// </remarks>
        internal bool TryGetNextMessage(out CommandMessage message)
        {
            return _receivedMessageQueue.TryDequeue(out message);
        }

        /// <summary>
        /// Appends a received command message to the internal message queue for processing.
        /// If the queue exceeds the maximum allowed received messages, the oldest message is removed.
        /// </summary>
        /// <param name="message">The command message to be appended to the queue for further processing.</param>
        /// <remarks>
        /// This method helps maintain a manageable message queue by ensuring the number of stored messages does not exceed
        /// the defined maximum limit. It is internally used within the communication system for message management.
        /// </remarks>
        internal void AppendMessage(CommandMessage message)
        {
            _receivedMessageQueue.Enqueue(message);

            if (_receivedMessageQueue.Count > MaxReceivedMessages)
            {
                _receivedMessageQueue.Dequeue();
            }
        }

        /// <summary>
        /// Sends a general command for processing within the system. This is typically invoked on the client to issue commands.
        /// </summary>
        /// <param name="message">The command message to be sent and processed by the system.</param>
        /// <param name="commandFlags">Flags providing additional context or options for processing the command.</param>
        /// <remarks>
        /// This method is designed to be invoked on the client side. Attempting to execute this method on the server will log a warning, and the command will not be processed.
        /// </remarks>
        public void SendCommand(string message, CommandFlags commandFlags)
        {
            if (isServer)
            {
                CommandModule.Log.LogWarning("Server cannot issue commands!");
                return;
            }

            SendMessage(message, CommandMessageType.Command, CommandStatus.None, commandFlags);
        }

        /// <summary>
        /// Sends a relay command to other systems, typically for relaying or forwarding a command in a server-only context.
        /// </summary>
        /// <param name="message">The command message to be relayed to other systems or processes.</param>
        /// <remarks>
        /// This method is designed to only be invoked on the server. Using it on a client will result in a warning, and the relay command will not be executed.
        /// </remarks>
        public void SendRelayCommand(string message)
        {
            if (!isServer)
            {
                CommandModule.Log.LogWarning("Client cannot send relay commands!");
                return;
            }

            SendMessage(message, CommandMessageType.RelayCommand, CommandStatus.None);
        }

        /// <summary>
        /// Sends a chat message to a specified target connection or broadcasts it depending on the context of the server.
        /// </summary>
        /// <param name="message">The content of the chat message to be sent.</param>
        /// <param name="targetConnection">The target entity to which the chat message will be sent. If left as default, the message might be broadcasted based on server behavior.</param>
        /// <remarks>
        /// This method is designed for use on the server. Attempting to invoke this method on a client will result in a warning and no message being sent.
        /// </remarks>
        public void SendChatMessage(string message, Unity.Entities.Entity targetConnection = default)
        {
            if (!isServer)
            {
                CommandModule.Log.LogWarning("Client cannot send messages!");
                return;
            }

            SendMessage(message, CommandMessageType.ChatMessage, CommandStatus.None, CommandFlags.None, targetConnection);
        }

        /// <summary>
        /// Sends a response message back to the specified target entity with details on the message content, its status, and optional flags.
        /// </summary>
        /// <param name="message">The content of the response message to be sent.</param>
        /// <param name="status">Specifies the status of the response, such as informational, warning, or error.</param>
        /// <param name="commandFlags">Optional command flags that determine additional context or behavior for the response message, such as request for hints or originating from a specific console.</param>
        /// <param name="targetConnection">The target entity to which the response message will be sent. If not provided, the response behavior might vary depending on server context.</param>
        /// <remarks>
        /// This method is invoked to communicate responses from the server back to a specific target entity.
        /// It ensures that only the server can send messages and utilizes the internal message-sending mechanism for reliable delivery.
        /// </remarks>
        public void SendResponse(string message, CommandStatus status, CommandFlags commandFlags = CommandFlags.None, Unity.Entities.Entity targetConnection = default)
        {
            if (!isServer)
            {
                CommandModule.Log.LogWarning("Client cannot send messages!");
                return;
            }

            SendMessage(message, CommandMessageType.Response, status, commandFlags, targetConnection);
        }

        /// <summary>
        /// Sends a command message to specified targets using RPC, with detailed configuration of the message type, status, and optional flags.
        /// </summary>
        /// <param name="message">The textual message content to be sent through the command system.</param>
        /// <param name="messageType">Specifies the type of message being sent, such as command, relay, response, or chat.</param>
        /// <param name="status">Indicates the status of the message, such as informational, warning, or error.</param>
        /// <param name="commandFlags">Optional flags to modify the behavior or context of the command message, such as user-requested hints or specific console origin.</param>
        /// <param name="targetConnection">The target destination for the message. If not specified, a default value is used.</param>
        /// <remarks>
        /// This method processes the message by converting it to a byte representation and segmenting it into manageable sizes for transmission.
        /// It ensures each message segment is correctly associated with the message number, total size, and additional metadata before creating and dispatching the relevant entities.
        /// The method facilitates reliable communication within the entity-based system while respecting the provided message properties and flags.
        /// </remarks>
        private void SendMessage(
            string message, 
            CommandMessageType messageType, 
            CommandStatus status, 
            CommandFlags commandFlags = CommandFlags.None,
            Unity.Entities.Entity targetConnection = default)
        {
            _messageCount++;

            byte[] commandBytes = Encoding.UTF8.GetBytes(message);
            int bytesLength = commandBytes.Length;

            int entityCount = (bytesLength - 1) / 64 + 1;

            SendRpcCommandRequest rpcComponent = new SendRpcCommandRequest
            {
                TargetConnection = targetConnection
            };

            CommandMessageRPC commandMessage = new CommandMessageRPC
            {
                MessageNumber = _messageCount,
                MessageType = messageType,
                Status = status,
                TotalSize = bytesLength,
                CommandFlags = commandFlags
            };

            CommandDataMessageRPC messagePart = new CommandDataMessageRPC
            {
                MessageNumber = commandMessage.MessageNumber
            };

            Unity.Entities.Entity entity = EntityManager.CreateEntity(_messageRpcArchetype);
            EntityManager.SetComponentData(entity, commandMessage);
            EntityManager.SetComponentData(entity, rpcComponent);

            using NativeArray<Unity.Entities.Entity> partEntities = EntityManager.CreateEntity(_messageDataRpcArchetype, entityCount, Allocator.Temp);

            for (int i = 0; i < partEntities.Length; i++)
            {
                messagePart.StartByte = i * messagePart.MessagePart.Size;
                messagePart.MessagePart.CopyFrom(commandBytes, messagePart.StartByte);

                EntityManager.SetComponentData(partEntities[i], messagePart);
                EntityManager.SetComponentData(partEntities[i], rpcComponent);
            }
        }

        /// <summary>
        /// Initializes the system during its creation by configuring message archetypes and enabling pre-initialization execution.
        /// </summary>
        /// <remarks>
        /// This method sets up the required <see cref="EntityArchetype"/> instances for handling RPC communication. Specifically:
        /// - <see cref="_messageRpcArchetype"/> is created to facilitate command message RPC processing, incorporating components such as <see cref="CommandMessageRPC"/> and <see cref="SendRpcCommandRequest"/>.
        /// - <see cref="_messageDataRpcArchetype"/> is created for command data message RPCs, using components like <see cref="CommandDataMessageRPC"/> and <see cref="SendRpcCommandRequest"/>.
        /// Additionally, it calls <see cref="PugSimulationSystemBase.AllowToRunBeforeInit"/> to enable the system to execute before all initialization is complete.
        /// The base <see cref="PugSimulationSystemBase.OnCreate"/> method is then invoked to ensure proper initialization of inherited functionality.
        /// </remarks>
        protected override void OnCreate()
        {
            AllowToRunBeforeInit();
            _messageRpcArchetype = EntityManager.CreateArchetype(typeof(CommandMessageRPC), typeof(SendRpcCommandRequest));
            _messageDataRpcArchetype = EntityManager.CreateArchetype(typeof(CommandDataMessageRPC), typeof(SendRpcCommandRequest));
            base.OnCreate();
        }

        /// <summary>
        /// Executes the system's core update logic by processing message entities, handling partial message assembly,
        /// and delegating message handling to the server or client based on the current simulation mode.
        /// </summary>
        /// <remarks>
        /// This method processes entities representing RPC messages received during a network simulation cycle. For each message:
        /// - Partial messages are tracked, assembled, and stored using dictionaries.
        /// - Messages are destroyed from the EntityManager after processing to prevent reuse.
        /// During server simulation, message handling is delegated to <see cref="ServerHandleMessages"/>. For client simulation,
        /// handling is delegated to <see cref="ClientHandleMessages"/>. Entity destruction and playback are managed with <see cref="EntityCommandBuffer"/>
        /// to ensure safe and efficient operations. The method continues to run each frame, combining all necessary tasks required
        /// based on the current simulation state, and invokes its base implementation at the end.
        /// </remarks>
        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
        protected override void OnUpdate()
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            Entities.ForEach((Unity.Entities.Entity entity, in CommandMessageRPC rpc, in ReceiveRpcCommandRequest req) =>
                {
                    if (_partialMessages.ContainsKey(rpc.MessageNumber))
                    {
                        CommandModule.Log.LogWarning("Got message with same number twice");
                        ecb.DestroyEntity(entity);
                        return;
                    }

                    _partialMessages.Add(rpc.MessageNumber, new CommandMessage
                    {
                        Sender = req.SourceConnection,
                        MessageType = rpc.MessageType,
                        Status = rpc.Status,
                        CommandFlags = rpc.CommandFlags
                    });
                    _partialMessagesData.Add(rpc.MessageNumber, new byte[rpc.TotalSize]);
                    ecb.DestroyEntity(entity);
                }).WithoutBurst()
                .Run();

            Entities.ForEach((Unity.Entities.Entity entity, in CommandDataMessageRPC rpc) =>
                {
                    if (!_partialMessagesData.TryGetValue(rpc.MessageNumber, out byte[] bytes))
                    {
                        CommandModule.Log.LogWarning("Got data message without meta message");
                        ecb.DestroyEntity(entity);
                        return;
                    }

                    FixedArray64 part = rpc.MessagePart;

                    part.CopyTo(bytes, rpc.StartByte);

                    int startByte = rpc.StartByte;
                    if (startByte + part.Size < bytes.Length)
                    {
                        ecb.DestroyEntity(entity);
                        return;
                    }

                    var message = _partialMessages[rpc.MessageNumber];
                    message.Message = Encoding.UTF8.GetString(_partialMessagesData[rpc.MessageNumber]);

                    AppendMessage(message);

                    _partialMessagesData.Remove(rpc.MessageNumber);
                    _partialMessages.Remove(rpc.MessageNumber);
                    ecb.DestroyEntity(entity);
                }).WithAll<ReceiveRpcCommandRequest>()
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

        /// <summary>
        /// Processes incoming command messages from the server's message queue and delegates their handling to the server-side command logic.
        /// </summary>
        /// <remarks>
        /// This method continues to dequeue command messages from the server's internal message queue by repeatedly invoking <see cref="TryGetNextMessage(out CommandMessage)"/>.
        /// Each retrieved message is then passed to the server command handling module to execute the appropriate server-side logic.
        /// The loop runs for as long as there are messages in the queue, ensuring that all pending commands are processed sequentially.
        /// </remarks>
        private void ServerHandleMessages()
        {
            while (TryGetNextMessage(out CommandMessage message))
            {
                CommandModule.ServerHandleCommand(message);
            }
        }

        /// <summary>
        /// Processes relay command messages from the received message queue and invokes appropriate client command handling logic.
        /// </summary>
        /// <remarks>
        /// This method filters messages of type <see cref="CommandMessageType.RelayCommand"/> from the internal queue of received messages
        /// and iterates through them, executing their respective client-side command handlers.
        /// It ensures only relay commands are selected for processing and forwards them to the designated handler method.
        /// </remarks>
        private void ClientHandleMessages()
        {
            var relayCommands = _receivedMessageQueue.Where(message => message.MessageType == CommandMessageType.RelayCommand).ToArray();
            foreach (CommandMessage message in relayCommands)
            {
                CommandModule.ClientHandleCommand(message);
            }
        }
    }
}