using Unity.Entities;
using Unity.NetCode;

namespace CoreLib.Submodules.ChatCommands.Communication
{
    public enum CommandMessageType : byte
    {
        Command,
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
        public CommandMessageType messageType;
        public CommandStatus status;
        public int totalSize;
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
    }
}