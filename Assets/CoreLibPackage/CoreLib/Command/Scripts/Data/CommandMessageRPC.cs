using System;
using Unity.NetCode;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Command.Data
{
    /// <summary>
    /// Defines the type or category of a command message in the communication system.
    /// </summary>
    /// <remarks>
    /// The CommandMessageType enumeration categorizes various types of messages within the command system.
    /// It helps differentiate between standard commands, relayed commands, responses, and chat messages.
    /// This categorization enables the system to process and handle messages more effectively based on their context.
    /// </remarks>
    public enum CommandMessageType : byte
    {
        Command,
        RelayCommand,
        Response,
        ChatMessage
    }

    /// <summary>
    /// Represents the status level or classification of a command or message.
    /// </summary>
    /// <remarks>
    /// The CommandStatus enumeration is used to distinguish various levels of command output or feedback.
    /// It categorizes messages into types, such as informational messages, warnings, errors, and more.
    /// This enables the system to handle, filter, or display messages appropriately based on their status level.
    /// </remarks>
    public enum CommandStatus : byte
    {
        None,
        Info,
        Hint,
        Warning,
        Error
    }

    /// <summary>
    /// Represents flags indicating specific attributes or behaviors of a command.
    /// </summary>
    /// <remarks>
    /// The CommandFlags enumeration is used to specify additional metadata or operational
    /// characteristics of a command. It supports a combination of values using bitwise operations
    /// due to its [Flags] attribute, allowing commands to represent multiple states or properties.
    /// Common uses include signaling user preferences for hints and identifying the source of a command.
    /// </remarks>
    [Flags]
    public enum CommandFlags : byte
    {
        None = 0,
        UserWantsHints = 1,
        SentFromQuantumConsole = 2
    }

    /// <summary>
    /// Represents a remote procedure call message for transmitting command-related data.
    /// </summary>
    /// <remarks>
    /// The CommandMessageRPC struct is used for conveying command-specific information
    /// across networked environments. It includes metadata such as the message number,
    /// the total size of the message, type classifications, processing status, and
    /// additional flags describing operational context. This facilitates structured
    /// communication and execution of commands in distributed systems.
    /// </remarks>
    public struct CommandMessageRPC : IRpcCommand
    {
        public int MessageNumber;
        public int TotalSize;
        public CommandMessageType MessageType;
        public CommandStatus Status;
        public CommandFlags CommandFlags;
    }

    /// <summary>
    /// Represents a command data message transmitted remotely via RPC.
    /// </summary>
    /// <remarks>
    /// The CommandDataMessageRPC struct is utilized for sending segmented data or
    /// messages over a network. It includes information such as the message sequence
    /// number, a portion of the message's content, and the starting index of the current data segment.
    /// This ensures proper management and reconstruction of large messages transferred across systems.
    /// </remarks>
    public struct CommandDataMessageRPC : IRpcCommand
    {
        public int MessageNumber;
        public FixedArray64 MessagePart;
        public int StartByte;
    }

    /// <summary>
    /// Represents a command message used for communication within the system.
    /// </summary>
    /// <remarks>
    /// The CommandMessage struct is used to encapsulate details of a command or message
    /// that is transmitted between different entities or systems. It contains the message
    /// content, the entity sending the message, the type of the message, its status, and any
    /// associated command flags.
    /// </remarks>
    public struct CommandMessage
    {
        /// <summary>
        /// Represents the content of a command message being transmitted.
        /// </summary>
        /// <remarks>
        /// This variable holds the text or main content of the command message.
        /// It is a central component of the <see cref="CommandMessage"/> struct, which is used
        /// for communication between different parts of the system. The value of this variable
        /// can be processed, displayed, or relayed depending on the message type and associated flags.
        /// </remarks>
        public string Message;

        /// <summary>
        /// Represents the sender of the command message.
        /// </summary>
        /// <remarks>
        /// The sender is identified as an <see cref="Unity.Entities.Entity"/>.
        /// It represents the entity responsible for originating the command or message.
        /// </remarks>
        public Unity.Entities.Entity Sender;

        /// <summary>
        /// Represents the type of the command message, indicating its purpose or category.
        /// </summary>
        /// <remarks>
        /// The <c>messageType</c> variable is used to identify the specific type of command message being processed.
        /// It is an instance of the <c>CommandMessageType</c> enumeration, which defines various types such as:
        /// - Command
        /// - RelayCommand
        /// - Response
        /// - ChatMessage
        /// This distinction allows the system to handle messages appropriately based on their type.
        /// </remarks>
        public CommandMessageType MessageType;

        /// <summary>
        /// Represents the status of a command or message within the system.
        /// </summary>
        /// <remarks>
        /// The status field indicates the type or severity of the command message, which
        /// can be categorized into various levels such as None, Info, Hint, Warning, or Error.
        /// This property is utilized to determine the nature of the feedback or information being conveyed
        /// and may be used for logging, displaying colored text, or applying conditional logic
        /// based on the message's severity.
        /// </remarks>
        public CommandStatus Status;

        /// <summary>
        /// A variable that specifies the flags indicating metadata or options associated with a command message within the system.
        /// </summary>
        /// <remarks>
        /// The <c>commandFlags</c> variable is used to include additional contextual information about a command.
        /// This information is represented as a combination of values defined in the <c>CommandFlags</c> enumeration.
        /// These flags can denote specific behaviors or states of the command, such as whether the command was sent
        /// from a specific source or if the user has enabled specific preferences (e.g., hints).
        /// </remarks>
        public CommandFlags CommandFlags;

        /// Represents a command message used for communication within the system.
        /// This struct encapsulates the message content, sender entity, message type, status, and associated flags.
        public CommandMessage(CommandOutput output, CommandFlags flags = CommandFlags.None)
        {
            Message = output.Feedback;
            Status = output.Status;
            MessageType = CommandMessageType.ChatMessage;
            Sender = Unity.Entities.Entity.Null;
            CommandFlags = flags;
        }

        /// Represents a command or message used for communication within the system.
        /// This struct encapsulates the message content, sender information, message type, status, and additional flags.
        public CommandMessage(string feedback, CommandStatus status, CommandFlags flags = CommandFlags.None)
        {
            Message = feedback;
            this.Status = status;
            MessageType = CommandMessageType.ChatMessage;
            Sender = Unity.Entities.Entity.Null;
            CommandFlags = flags;
        }
    }
}