using System;
using Unity.Entities;
using Unity.NetCode;

namespace CoreLib.Commands.Communication
{
    public enum CommandMessageType : byte
    {
        Command,
        RelayCommand,
        Response,
        ChatMessage
    }

    public enum CommandStatus : byte
    {
        None,
        Info,
        Hint,
        Warning,
        Error
    }

    [Flags]
    public enum CommandFlags : byte
    {
        None = 0,
        UserWantsHints = 1,
        SentFromQuantumConsole = 2
    }

    public struct CommandMessageRPC : IRpcCommand
    {
        public int messageNumber;
        public int totalSize;
        public CommandMessageType messageType;
        public CommandStatus status;
        public CommandFlags commandFlags;
    }

    public struct CommandDataMessageRPC : IRpcCommand
    {
        public int messageNumber;
        public FixedArray64 messagePart;
        public int startByte;
    }

    public struct CommandMessage
    {
        public string message;
        public Entity sender;
        public CommandMessageType messageType;
        public CommandStatus status;
        public CommandFlags commandFlags; 
        
        public CommandMessage(CommandOutput output, CommandFlags flags = CommandFlags.None)
        {
            message = output.feedback;
            status = output.status;
            messageType = CommandMessageType.ChatMessage;
            sender = Entity.Null;
            commandFlags = flags;
        }
        
        public CommandMessage(string feedback, CommandStatus status, CommandFlags flags = CommandFlags.None)
        {
            message = feedback;
            this.status = status;
            messageType = CommandMessageType.ChatMessage;
            sender = Entity.Null;
            commandFlags = flags;
        }
    }
}