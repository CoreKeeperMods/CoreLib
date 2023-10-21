using CoreLib.Commands.Communication;

namespace CoreLib.Commands
{
    public struct CommandPair
    {
        public ICommandInfo handler;
        public string modName;

        public bool isServer => handler is IServerCommandHandler;
        
        public IServerCommandHandler serverHandler
        {
            get
            {
                if (handler is IServerCommandHandler serverCommandHandler)
                    return serverCommandHandler;
                return null;
            }
        }
        
        public IClientCommandHandler clientHandler
        {
            get
            {
                if (handler is IClientCommandHandler clientCommandHandler)
                    return clientCommandHandler;
                return null;
            }
        }
        
        public CommandOutput Execute(CommandMessage message, string[] parameters)
        {
            if (isServer)
            {
                return serverHandler.Execute(parameters, message.sender);
            }

            return clientHandler.Execute(parameters);
        }

        public CommandPair(ICommandInfo handler, string modName)
        {
            this.handler = handler;
            this.modName = modName;
        }
    }
}