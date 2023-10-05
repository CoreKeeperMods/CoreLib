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

    public struct CommandMessageRPC : IRpcCommand
    {
        public int messageNumber;
        public int totalSize;
        public CommandMessageType messageType;
        public CommandStatus status;
        public bool userWantsHints;
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
        public bool userWantsHints; 

        public CommandMessage(CommandOutput output)
        {
            message = output.feedback;
            status = output.status;
            messageType = CommandMessageType.ChatMessage;
            sender = Entity.Null;
            userWantsHints = false;
        }
        
        public CommandMessage(string feedback, CommandStatus status)
        {
            message = feedback;
            this.status = status;
            messageType = CommandMessageType.ChatMessage;
            sender = Entity.Null;
            userWantsHints = false;
        }
    }
}